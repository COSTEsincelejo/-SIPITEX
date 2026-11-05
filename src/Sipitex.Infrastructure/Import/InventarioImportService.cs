using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Import;

// Carga la plantilla CMTC: simula sin escribir y, al confirmar, persiste todo en una transacción.
public sealed class InventarioImportService : IInventarioImportService
{
    private const int NombreMax = 120;
    private const int PlantaMax = 80;

    private readonly SipitexDbContext _db;
    private readonly IUnitOfWork _uow;

    public InventarioImportService(SipitexDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<InventarioImportResult> SimularAsync(
        Stream contenido,
        CancellationToken cancellationToken = default)
    {
        var lectura = InventarioExcelReader.Leer(contenido);
        if (lectura.Error is not null)
            return InventarioImportResult.Invalido(lectura.Error);

        var plan = await ArmarPlanAsync(lectura.Filas, cancellationToken);
        return plan.ToResult(persistido: false, importados: 0);
    }

    public async Task<InventarioImportResult> ConfirmarAsync(
        Stream contenido,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var lectura = InventarioExcelReader.Leer(contenido);
        if (lectura.Error is not null)
            return InventarioImportResult.Invalido(lectura.Error);
        if (actorUserId <= 0)
            return InventarioImportResult.Invalido("No se pudo identificar al administrador que confirma.");

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == actorUserId, cancellationToken);
        if (user is null)
            return InventarioImportResult.Invalido("No se encontró el usuario que confirma la importación.");

        var preview = await ArmarPlanAsync(lectura.Filas, cancellationToken);
        if (preview.Validas.Count == 0)
            return preview.ToResult(persistido: false, importados: 0);

        var resultado = preview.ToResult(persistido: false, importados: 0);
        await _uow.ExecuteInTransactionAsync(async ct =>
        {
            resultado = await PersistirAsync(lectura.Filas, user, ct);
        }, cancellationToken);
        return resultado;
    }

    private async Task<InventarioImportResult> PersistirAsync(
        IReadOnlyList<InventarioExcelFila> filas,
        User user,
        CancellationToken cancellationToken)
    {
        var plan = await ArmarPlanAsync(filas, cancellationToken);
        if (plan.Validas.Count == 0)
            return plan.ToResult(persistido: false, importados: 0);

        var plantas = await _db.PlantasInventario.ToListAsync(cancellationToken);
        var porNombre = new Dictionary<string, PlantaInventario>(StringComparer.Ordinal);
        foreach (var planta in plantas.OrderBy(p => p.Id))
        {
            var clave = ClaveNombre(planta.Nombre);
            if (clave.Length > 0)
                porNombre.TryAdd(clave, planta);
        }

        var usados = (await _db.Materials.IgnoreQueryFilters()
                .Select(m => m.Code)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
        var tick = DateTime.UtcNow.Ticks;

        var plantasDeFila = plan.Validas.Select(c => ResolverPlanta(porNombre, c.Planta)).ToList();
        if (plantasDeFila.Any(p => _db.Entry(p).State == EntityState.Added))
            await _uow.SaveChangesAsync(cancellationToken);

        var creados = new List<(Material Material, string Planta)>();
        for (var i = 0; i < plan.Validas.Count; i++)
        {
            var candidato = plan.Validas[i];
            var planta = plantasDeFila[i];
            var code = SiguienteCodigo(usados, ref tick);
            var material = new Material
            {
                Code = code,
                CodigoAlmacen = candidato.CodigoAlmacen,
                Name = candidato.Nombre,
                Categoria = candidato.Categoria,
                Subcategoria = candidato.Subcategoria,
                Referencia = candidato.Referencia,
                Descripcion = candidato.Descripcion,
                Proveedor = candidato.Proveedor,
                TipoElemento = candidato.TipoElemento,
                Unit = candidato.Unidad,
                Stock = candidato.Stock,
                MinStock = candidato.MinStock,
                Status = MaterialStatus.Bueno,
                LastEntryDate = candidato.FechaIngreso,
                CostoAdquisicion = 0,
                CostoPromedioPonderado = 0,
                PlantaInventarioId = planta.Id
            };
            _db.Materials.Add(material);
            creados.Add((material, planta.Nombre));
        }

        await _uow.SaveChangesAsync(cancellationToken);

        var userName = string.IsNullOrWhiteSpace(user.Nombre) ? user.Email.Trim() : user.Nombre.Trim();
        if (userName.Length > 120)
            userName = userName[..120];
        var ahora = DateTime.UtcNow;

        foreach (var (material, planta) in creados)
        {
            _db.StockMovements.Add(new StockMovement
            {
                MaterialId = material.Id,
                FechaUtc = ahora,
                UsuarioId = user.Id,
                TipoMovimiento = StockMovementType.Entrada,
                Origen = StockEntryOrigin.OtraFuenteAutorizada,
                Cantidad = material.Stock,
                StockResultante = material.Stock,
                Referencia = InventarioImportTextos.ReferenciaMovimiento,
                CostoUnitario = 0
            });

            var details = $"Importación inicial: {material.Name} ({material.Code}), planta {planta}.";
            if (details.Length > 2000)
                details = details[..2000];
            _db.ActivityLogs.Add(new ActivityLog
            {
                UserId = user.Id,
                UserName = userName,
                Action = ActivityLogActions.ImportInventario,
                Entity = ActivityLogEntities.Material,
                EntityId = material.Id.ToString(CultureInfo.InvariantCulture),
                Timestamp = ahora,
                Details = details
            });
        }

        return plan.ToResult(persistido: true, importados: creados.Count);
    }

    private PlantaInventario ResolverPlanta(
        Dictionary<string, PlantaInventario> porNombre,
        string nombre)
    {
        var clave = ClaveNombre(nombre);
        if (porNombre.TryGetValue(clave, out var existente))
            return existente;

        var nueva = new PlantaInventario { Nombre = nombre.Trim(), Activo = true };
        _db.PlantasInventario.Add(nueva);
        porNombre[clave] = nueva;
        return nueva;
    }

    private static string SiguienteCodigo(HashSet<string> usados, ref long tick)
    {
        string code;
        do
        {
            code = $"mat{tick}";
            tick++;
        }
        while (!usados.Add(code));

        return code;
    }

    private async Task<Plan> ArmarPlanAsync(
        IReadOnlyList<InventarioExcelFila> filas,
        CancellationToken cancellationToken)
    {
        var plantas = await _db.PlantasInventario.AsNoTracking().ToListAsync(cancellationToken);
        var plantaPorNombre = new Dictionary<string, PlantaInventario>(StringComparer.Ordinal);
        foreach (var planta in plantas.OrderBy(p => p.Id))
        {
            var clave = ClaveNombre(planta.Nombre);
            if (clave.Length > 0)
                plantaPorNombre.TryAdd(clave, planta);
        }

        var materiales = await _db.Materials.IgnoreQueryFilters().AsNoTracking()
            .Select(m => new { m.CodigoAlmacen, m.Name, m.PlantaInventarioId })
            .ToListAsync(cancellationToken);

        var clavesBd = new HashSet<string>(StringComparer.Ordinal);
        var codigosBd = new HashSet<string>(StringComparer.Ordinal);
        foreach (var material in materiales)
        {
            var codigo = (material.CodigoAlmacen ?? string.Empty).Trim().ToLowerInvariant();
            if (codigo.Length > 0)
                codigosBd.Add(codigo);
            clavesBd.Add(ClaveMaterial(codigo, material.Name, material.PlantaInventarioId.ToString(CultureInfo.InvariantCulture)));
        }

        var conteoCodigos = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var fila in filas)
        {
            if (string.IsNullOrWhiteSpace(fila.Nombre) || EsCategoriaDeFlujo(fila.Categoria))
                continue;
            var codigo = (fila.Codigo ?? string.Empty).Trim().ToLowerInvariant();
            if (codigo.Length == 0)
                continue;
            conteoCodigos[codigo] = conteoCodigos.GetValueOrDefault(codigo) + 1;
        }

        var plan = new Plan();
        var primeraFila = new Dictionary<string, int>(StringComparer.Ordinal);
        var plantasNuevas = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var fila in filas)
        {
            if (string.IsNullOrWhiteSpace(fila.Nombre))
                continue;

            if (EsCategoriaDeFlujo(fila.Categoria))
            {
                var avisoFlujo = InventarioImportTextos.PorFlujoOrdenes;
                plan.Advertencias.Add(Aviso(fila.Fila, avisoFlujo, fila.Nombre));
                plan.Omitidas.Add(new InventarioImportOmitida
                {
                    Fila = fila.Fila,
                    CodigoAlmacen = string.IsNullOrWhiteSpace(fila.Codigo) ? null : fila.Codigo.Trim(),
                    Nombre = fila.Nombre.Trim(),
                    Planta = fila.Ubicacion?.Trim() ?? string.Empty,
                    Mensaje = avisoFlujo
                });
                continue;
            }

            if (EsVacia(fila))
                continue;

            var errores = new List<string>();
            var nombre = Obligatorio(fila.Nombre, NombreMax, "El nombre", "El nombre es obligatorio.", errores);
            var ubicacion = Obligatorio(fila.Ubicacion, PlantaMax, "La ubicación", "La ubicación es obligatoria.", errores);
            var codigo = Opcional(fila.Codigo, Material.CodigoAlmacenMaxLength, "El código de almacén", errores);
            var categoria = Opcional(fila.Categoria, Material.CategoriaMaxLength, "La categoría", errores);
            var subcategoria = Opcional(fila.Subcategoria, Material.SubcategoriaMaxLength, "La subcategoría", errores);
            var referencia = Opcional(fila.Referencia, Material.ReferenciaMaxLength, "La referencia", errores);
            var descripcion = Opcional(fila.Descripcion, Material.DescripcionMaxLength, "La descripción", errores);
            var proveedor = Opcional(fila.Proveedor, Material.ProveedorMaxLength, "El proveedor", errores);
            var stock = LeerDecimal(fila.CantidadVacia, fila.CantidadNumero, fila.CantidadTexto, "La cantidad", errores);
            var minStock = LeerDecimal(fila.MinStockVacio, fila.MinStockNumero, fila.MinStockTexto, "El stock mínimo", errores);
            var fecha = LeerFecha(fila, errores);
            var tipo = LeerTipo(fila.TipoElemento, errores);
            var unidad = MaterialUnitImportParser.Parse(fila.Unidad);

            if (!unidad.Vacia && !unidad.Reconocida)
                errores.Add($"Unidad desconocida: {Recortar(fila.Unidad)}.");

            if (errores.Count > 0)
            {
                plan.Errores.Add(new InventarioImportFilaError
                {
                    Fila = fila.Fila,
                    Nombre = string.IsNullOrWhiteSpace(fila.Nombre) ? null : fila.Nombre.Trim(),
                    Mensaje = string.Join(" ", errores)
                });
                if (unidad.Vacia)
                    plan.Advertencias.Add(Aviso(fila.Fila, InventarioImportTextos.SinUnidad, fila.Nombre));
                continue;
            }

            if (unidad.Vacia)
            {
                plan.Advertencias.Add(Aviso(fila.Fila, InventarioImportTextos.SinUnidad, fila.Nombre));
                continue;
            }

            if (nombre is null || ubicacion is null || stock is not decimal cantidad || minStock is not decimal minimo
                || fecha is not DateOnly fechaIngreso || tipo is not TipoElemento tipoElemento)
                continue;

            var clavePlanta = ClaveNombre(ubicacion);
            int? plantaId = null;
            string nombrePlanta;
            var plantaNueva = false;
            if (plantaPorNombre.TryGetValue(clavePlanta, out var plantaBd))
            {
                nombrePlanta = plantaBd.Nombre;
                plantaId = plantaBd.Id;
            }
            else if (plantasNuevas.TryGetValue(clavePlanta, out var ya))
            {
                nombrePlanta = ya;
                plantaNueva = true;
            }
            else
            {
                nombrePlanta = ubicacion;
                plantaNueva = true;
                plantasNuevas[clavePlanta] = ubicacion;
            }

            var codigoClave = codigo?.ToLowerInvariant() ?? string.Empty;
            var avisos = new List<string>();
            if (codigo is null)
                avisos.Add(InventarioImportTextos.SinCodigo);
            else if (conteoCodigos.GetValueOrDefault(codigoClave) > 1 || codigosBd.Contains(codigoClave))
                avisos.Add(InventarioImportTextos.CodigoRepetido);

            var claveLote = ClaveMaterial(codigoClave, nombre, clavePlanta);
            var enBd = plantaId is int id
                && clavesBd.Contains(ClaveMaterial(codigoClave, nombre, id.ToString(CultureInfo.InvariantCulture)));
            var enLote = primeraFila.TryGetValue(claveLote, out var filaPrimera);
            if (enBd || enLote)
            {
                var mensaje = enLote
                    ? InventarioImportTextos.DuplicadoEnArchivo(fila.Fila, filaPrimera)
                    : InventarioImportTextos.Omitido;
                plan.Omitidas.Add(new InventarioImportOmitida
                {
                    Fila = fila.Fila,
                    CodigoAlmacen = codigo,
                    Nombre = nombre,
                    Planta = nombrePlanta,
                    Mensaje = mensaje
                });
                plan.Advertencias.Add(Aviso(fila.Fila, mensaje, nombre));
                continue;
            }

            primeraFila[claveLote] = fila.Fila;

            plan.Validas.Add(new Candidato(
                fila.Fila,
                codigo,
                nombre,
                categoria,
                subcategoria,
                referencia,
                descripcion,
                proveedor,
                cantidad,
                minimo,
                unidad.Unidad,
                fechaIngreso,
                nombrePlanta,
                plantaNueva,
                tipoElemento,
                avisos));
            foreach (var aviso in avisos)
                plan.Advertencias.Add(Aviso(fila.Fila, aviso, nombre));
        }

        return plan;
    }

    private static bool EsVacia(InventarioExcelFila fila) =>
        string.IsNullOrWhiteSpace(fila.Codigo)
        && string.IsNullOrWhiteSpace(fila.Categoria)
        && string.IsNullOrWhiteSpace(fila.Subcategoria)
        && string.IsNullOrWhiteSpace(fila.Nombre)
        && string.IsNullOrWhiteSpace(fila.Referencia)
        && fila.CantidadVacia
        && string.IsNullOrWhiteSpace(fila.Unidad)
        && string.IsNullOrWhiteSpace(fila.Descripcion)
        && fila.FechaVacia
        && string.IsNullOrWhiteSpace(fila.Proveedor)
        && string.IsNullOrWhiteSpace(fila.Ubicacion)
        && fila.MinStockVacio
        && string.IsNullOrWhiteSpace(fila.TipoElemento);

    private static string? Obligatorio(string? value, int max, string etiqueta, string obligatorio, List<string> errores)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errores.Add(obligatorio);
            return null;
        }

        var limpio = value.Trim();
        if (limpio.Length > max)
            errores.Add($"{etiqueta} no puede superar {max} caracteres.");
        return limpio;
    }

    private static string? Opcional(string? value, int max, string etiqueta, List<string> errores)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var limpio = value.Trim();
        if (limpio.Length > max)
            errores.Add($"{etiqueta} no puede superar {max} caracteres.");
        return limpio;
    }

    private static decimal? LeerDecimal(
        bool vacio,
        double? numero,
        string? texto,
        string etiqueta,
        List<string> errores)
    {
        if (vacio && numero is null && string.IsNullOrWhiteSpace(texto))
            return 0m;

        if (numero is double n)
        {
            if (!double.IsFinite(n))
            {
                errores.Add($"{etiqueta} no es un número válido.");
                return null;
            }

            try
            {
                return ValidarNoNegativo(decimal.Round((decimal)n, 2, MidpointRounding.AwayFromZero), etiqueta, errores);
            }
            catch (OverflowException)
            {
                errores.Add($"{etiqueta} no es un número válido.");
                return null;
            }
        }

        var crudo = texto!.Trim().Replace(" ", "", StringComparison.Ordinal);
        if (crudo.Contains(',') && crudo.Contains('.'))
            crudo = crudo.Replace(".", "", StringComparison.Ordinal).Replace(',', '.');
        else if (crudo.Contains(','))
            crudo = crudo.Replace(',', '.');

        if (!decimal.TryParse(crudo, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            errores.Add($"{etiqueta} no es un número válido.");
            return null;
        }

        return ValidarNoNegativo(decimal.Round(parsed, 2, MidpointRounding.AwayFromZero), etiqueta, errores);
    }

    private static decimal? ValidarNoNegativo(decimal valor, string etiqueta, List<string> errores)
    {
        if (valor < 0)
        {
            errores.Add($"{etiqueta} no puede ser negativa.");
            return null;
        }

        return valor;
    }

    private static DateOnly? LeerFecha(InventarioExcelFila fila, List<string> errores)
    {
        if (fila.FechaVacia && fila.FechaValor is null && fila.FechaSerial is null && string.IsNullOrWhiteSpace(fila.FechaTexto))
            return DateOnly.FromDateTime(DateTime.UtcNow);

        if (fila.FechaValor is DateTime fecha)
            return DateOnly.FromDateTime(fecha);

        if (fila.FechaSerial is double serial)
        {
            try
            {
                return DateOnly.FromDateTime(DateTime.FromOADate(serial));
            }
            catch (ArgumentException)
            {
                errores.Add("La fecha de ingreso no es válida. Use dd/mm/aaaa.");
                return null;
            }
        }

        if (TryFechaTexto(fila.FechaTexto, out var texto))
            return texto;

        errores.Add("La fecha de ingreso no es válida. Use dd/mm/aaaa.");
        return null;
    }

    private static bool TryFechaTexto(string? raw, out DateOnly fecha)
    {
        fecha = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var texto = raw.Trim();
        var space = texto.IndexOf(' ');
        if (space > 0)
            texto = texto[..space];

        string[] formatos = ["dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd", "yyyy/MM/dd"];
        if (!DateTime.TryParseExact(texto, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return false;

        fecha = DateOnly.FromDateTime(parsed);
        return true;
    }

    private static TipoElemento? LeerTipo(string? raw, List<string> errores)
    {
        var key = TextoNormalizado.Clave(raw);
        if (key.Length == 0)
            return TipoElemento.Consumible;
        if (key is "consumible" or "consumibles")
            return TipoElemento.Consumible;
        if (key is "devolutivo" or "devolutivos")
            return TipoElemento.Devolutivo;

        errores.Add("El tipo de elemento no es válido. Use Consumible o Devolutivo.");
        return null;
    }

    private static string ClaveNombre(string nombre) => TextoNormalizado.Clave(nombre);

    private static bool EsCategoriaDeFlujo(string? categoria)
    {
        var key = TextoNormalizado.Clave(categoria);
        return key is "material en proceso" or "producto terminado";
    }

    private static string ClaveMaterial(string codigo, string nombre, string planta) =>
        $"{codigo}|{nombre.Trim().ToLowerInvariant()}|{planta}";

    private static string Recortar(string? value)
    {
        var texto = value?.Trim() ?? string.Empty;
        return texto.Length <= 40 ? texto : texto[..40];
    }

    private static InventarioImportAdvertencia Aviso(int fila, string mensaje, string? nombre) =>
        new()
        {
            Fila = fila,
            Mensaje = mensaje,
            Nombre = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim()
        };

    private sealed record Candidato(
        int Fila,
        string? CodigoAlmacen,
        string Nombre,
        string? Categoria,
        string? Subcategoria,
        string? Referencia,
        string? Descripcion,
        string? Proveedor,
        decimal Stock,
        decimal MinStock,
        MaterialUnit Unidad,
        DateOnly FechaIngreso,
        string Planta,
        bool PlantaNueva,
        TipoElemento TipoElemento,
        IReadOnlyList<string> Advertencias);

    private sealed class Plan
    {
        public List<Candidato> Validas { get; } = [];
        public List<InventarioImportFilaError> Errores { get; } = [];
        public List<InventarioImportAdvertencia> Advertencias { get; } = [];
        public List<InventarioImportOmitida> Omitidas { get; } = [];

        public InventarioImportResult ToResult(bool persistido, int importados) => new()
        {
            Validas = Validas.Select(c => new InventarioImportFilaValida
            {
                Fila = c.Fila,
                CodigoAlmacen = c.CodigoAlmacen,
                Nombre = c.Nombre,
                Categoria = c.Categoria,
                Subcategoria = c.Subcategoria,
                Referencia = c.Referencia,
                Descripcion = c.Descripcion,
                Proveedor = c.Proveedor,
                Stock = c.Stock,
                MinStock = c.MinStock,
                Unidad = c.Unidad,
                FechaIngreso = c.FechaIngreso,
                Planta = c.Planta,
                PlantaNueva = c.PlantaNueva,
                TipoElemento = c.TipoElemento,
                Advertencias = c.Advertencias
            }).ToList(),
            Errores = Errores,
            Advertencias = Advertencias,
            Omitidas = Omitidas,
            Persistido = persistido,
            Importados = importados
        };
    }
}
