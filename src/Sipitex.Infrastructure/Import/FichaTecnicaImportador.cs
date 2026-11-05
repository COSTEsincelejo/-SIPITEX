using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Import;

internal sealed class FichaVinculo
{
    public int Fila { get; init; }
    public int MaterialId { get; init; }
    public string? Nombre { get; init; }
    public FichaTecnicaLeida Datos { get; init; } = new();
}

internal sealed class FichasPlan
{
    public List<FichaVinculo> Vinculos { get; } = [];
    public List<InventarioImportAdvertencia> Advertencias { get; } = [];
    public int Pendientes { get; set; }
    public int Vinculadas { get; set; }
    public int AVincular => Vinculos.Count + Pendientes;
}

// Vincula cada ficha solo cuando hay exactamente un material con ese CodigoAlmacen.
internal static class FichaTecnicaImportador
{
    public static async Task<FichasPlan> ClasificarAsync(
        SipitexDbContext db,
        IReadOnlyList<FichaTecnicaExcelFila> filas,
        IEnumerable<string?> codigosNuevos,
        string? avisoEncabezados,
        CancellationToken cancellationToken)
    {
        var plan = new FichasPlan();
        if (!string.IsNullOrWhiteSpace(avisoEncabezados))
        {
            plan.Advertencias.Add(new InventarioImportAdvertencia
            {
                Fila = 1,
                Mensaje = avisoEncabezados
            });
        }

        if (filas.Count == 0)
            return plan;

        var materiales = await db.Materials.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.CodigoAlmacen != null && m.CodigoAlmacen != "")
            .Select(m => new { m.Id, m.CodigoAlmacen })
            .ToListAsync(cancellationToken);

        var porCodigo = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        foreach (var material in materiales)
        {
            var clave = Clave(material.CodigoAlmacen);
            if (clave.Length == 0)
                continue;
            if (!porCodigo.TryGetValue(clave, out var ids))
            {
                ids = [];
                porCodigo[clave] = ids;
            }

            ids.Add(material.Id);
        }

        var pendientes = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var codigo in codigosNuevos)
        {
            var clave = Clave(codigo);
            if (clave.Length == 0)
                continue;
            pendientes[clave] = pendientes.GetValueOrDefault(clave) + 1;
        }

        var primera = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var fila in filas)
        {
            if (!TryDatos(fila, out var datos, out var errorDatos))
            {
                plan.Advertencias.Add(Aviso(fila, errorDatos!));
                continue;
            }

            var claves = Claves(fila);
            if (claves.Count == 0)
            {
                plan.Advertencias.Add(Aviso(fila, InventarioImportTextos.FichaSinMaterialUnico));
                continue;
            }

            var ids = new HashSet<int>();
            var nuevos = 0;
            string? codigoPendiente = null;
            foreach (var clave in claves)
            {
                if (porCodigo.TryGetValue(clave, out var encontrados))
                {
                    foreach (var id in encontrados)
                        ids.Add(id);
                }

                var extra = pendientes.GetValueOrDefault(clave);
                nuevos += extra;
                if (extra > 0)
                    codigoPendiente = clave;
            }

            if (ids.Count + nuevos != 1)
            {
                plan.Advertencias.Add(Aviso(fila, InventarioImportTextos.FichaSinMaterialUnico));
                continue;
            }

            var identidad = ids.Count == 1 ? "id:" + ids.Single().ToString(CultureInfo.InvariantCulture) : "cod:" + codigoPendiente;
            if (primera.TryGetValue(identidad, out var filaPrimera))
            {
                plan.Advertencias.Add(Aviso(fila, InventarioImportTextos.FichaRepetida(fila.Fila, filaPrimera)));
                continue;
            }

            primera[identidad] = fila.Fila;
            if (ids.Count != 1)
            {
                plan.Pendientes++;
                continue;
            }

            plan.Vinculos.Add(new FichaVinculo
            {
                Fila = fila.Fila,
                MaterialId = ids.Single(),
                Nombre = Nombre(fila),
                Datos = datos
            });
        }

        return plan;
    }

    public static async Task<int> GuardarAsync(
        SipitexDbContext db,
        FichasPlan plan,
        User user,
        CancellationToken cancellationToken)
    {
        if (plan.Vinculos.Count == 0)
            return 0;

        var ids = plan.Vinculos.Select(v => v.MaterialId).Distinct().ToList();
        var existentes = await db.FichasTecnicasMaterial.IgnoreQueryFilters()
            .Where(f => ids.Contains(f.MaterialId))
            .ToListAsync(cancellationToken);
        var porMaterial = existentes.ToDictionary(f => f.MaterialId);

        var userName = string.IsNullOrWhiteSpace(user.Nombre) ? user.Email.Trim() : user.Nombre.Trim();
        if (userName.Length > 120)
            userName = userName[..120];
        var ahora = DateTime.UtcNow;
        var creadas = new List<FichaTecnicaMaterial>();

        foreach (var vinculo in plan.Vinculos)
        {
            var crear = !porMaterial.TryGetValue(vinculo.MaterialId, out var ficha);
            if (crear)
            {
                ficha = new FichaTecnicaMaterial { MaterialId = vinculo.MaterialId };
                db.FichasTecnicasMaterial.Add(ficha);
                porMaterial[vinculo.MaterialId] = ficha;
                creadas.Add(ficha);
            }

            Copiar(ficha!, vinculo.Datos);
            if (!crear)
                AgregarLog(db, user, userName, ahora, ficha!, ActivityLogActions.UpdateFichaTecnicaMaterial, vinculo.Nombre);
        }

        if (creadas.Count > 0)
            await db.SaveChangesAsync(cancellationToken);

        foreach (var ficha in creadas)
        {
            var vinculo = plan.Vinculos.First(v => v.MaterialId == ficha.MaterialId);
            AgregarLog(db, user, userName, ahora, ficha, ActivityLogActions.CreateFichaTecnicaMaterial, vinculo.Nombre);
        }

        plan.Vinculadas = plan.Vinculos.Count;
        return plan.Vinculadas;
    }

    private static void Copiar(FichaTecnicaMaterial ficha, FichaTecnicaLeida datos)
    {
        ficha.DescripcionTecnica = datos.DescripcionTecnica;
        ficha.TipoMaterial = datos.TipoMaterial;
        ficha.Composicion = datos.Composicion;
        ficha.GramajeGm2 = datos.GramajeGm2;
        ficha.AnchoCm = datos.AnchoCm;
        ficha.Colores = datos.Colores;
        ficha.Acabados = datos.Acabados;
        ficha.EspecificacionesUso = datos.EspecificacionesUso;
        ficha.ProcesoFabricacion = datos.ProcesoFabricacion;
        ficha.NormasCertificaciones = datos.NormasCertificaciones;
        ficha.EnlaceFichaOriginal = datos.EnlaceFichaOriginal;
        ficha.Observaciones = datos.Observaciones;
    }

    private static void AgregarLog(
        SipitexDbContext db,
        User user,
        string userName,
        DateTime ahora,
        FichaTecnicaMaterial ficha,
        string action,
        string? nombre)
    {
        var details = string.IsNullOrWhiteSpace(nombre)
            ? "Ficha técnica importada."
            : $"Ficha técnica importada: {nombre.Trim()}.";
        if (details.Length > 2000)
            details = details[..2000];

        db.ActivityLogs.Add(new ActivityLog
        {
            UserId = user.Id,
            UserName = userName,
            Action = action,
            Entity = ActivityLogEntities.FichaTecnicaMaterial,
            EntityId = ficha.Id.ToString(CultureInfo.InvariantCulture),
            Timestamp = ahora,
            Details = details
        });
    }

    private static bool TryDatos(FichaTecnicaExcelFila fila, out FichaTecnicaLeida datos, out string? error)
    {
        var dto = new GuardarFichaTecnicaMaterialDto
        {
            DescripcionTecnica = fila.DescripcionTecnica,
            TipoMaterial = fila.TipoMaterial,
            Composicion = fila.Composicion,
            GramajeGm2 = NumeroOTexto(fila.GramajeVacio, fila.GramajeNumero, fila.GramajeTexto),
            AnchoCm = NumeroOTexto(fila.AnchoVacio, fila.AnchoNumero, fila.AnchoTexto),
            Colores = fila.Colores,
            Acabados = fila.Acabados,
            EspecificacionesUso = fila.EspecificacionesUso,
            ProcesoFabricacion = fila.ProcesoFabricacion,
            NormasCertificaciones = fila.NormasCertificaciones,
            EnlaceFichaOriginal = fila.EnlaceFichaOriginal,
            Observaciones = fila.Observaciones
        };

        if (!FichaTecnicaTexto.TryLeer(dto, out datos, out var errores))
        {
            error = string.Join(" ", errores);
            return false;
        }

        error = null;
        return true;
    }

    private static string? NumeroOTexto(bool vacio, double? numero, string? texto)
    {
        if (vacio && numero is null && string.IsNullOrWhiteSpace(texto))
            return null;
        if (numero is double n && double.IsFinite(n))
            return n.ToString(CultureInfo.InvariantCulture);
        return texto;
    }

    private static List<string> Claves(FichaTecnicaExcelFila fila)
    {
        var claves = new List<string>();
        foreach (var raw in new[] { fila.Codigo, fila.Referencia })
        {
            var clave = Clave(raw);
            if (clave.Length == 0 || claves.Contains(clave, StringComparer.Ordinal))
                continue;
            claves.Add(clave);
        }

        return claves;
    }

    private static string Clave(string? codigo) =>
        string.IsNullOrWhiteSpace(codigo) ? string.Empty : codigo.Trim().ToLowerInvariant();

    private static string? Nombre(FichaTecnicaExcelFila fila) =>
        string.IsNullOrWhiteSpace(fila.Nombre) ? null : fila.Nombre.Trim();

    private static InventarioImportAdvertencia Aviso(FichaTecnicaExcelFila fila, string mensaje) =>
        new()
        {
            Fila = fila.Fila,
            Nombre = Nombre(fila),
            Mensaje = mensaje
        };
}
