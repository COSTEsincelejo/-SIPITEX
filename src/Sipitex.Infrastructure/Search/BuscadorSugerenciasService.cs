using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Search;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Data;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Search;

public class BuscadorSugerenciasService : IBuscadorSugerenciasService
{
    private const int MaxPorGrupo = 5;
    private const int MaxTotal = 20;
    private readonly SipitexDbContext _db;
    private readonly InterpreteConsulta _interprete = new();

    public BuscadorSugerenciasService(SipitexDbContext db) => _db = db;

    public async Task<SugerenciasBusquedaDto> SugerirAsync(
        string consulta,
        AlcanceBusqueda alcance,
        CancellationToken cancellationToken = default)
    {
        if ((consulta ?? "").Trim().Length < 2)
            return SugerenciasBusquedaDto.Vacias();

        var interp = _interprete.Interpretar(consulta);
        if (!interp.ConsultaValida || !interp.Reconocida)
            return SugerenciasBusquedaDto.SinEntender();

        if (interp.QuierePersonas && !alcance.PuedeVerUsuarios && !TieneOtraPista(interp))
            return SugerenciasBusquedaDto.SinEntender();

        var ilike = SchemaIntrospection.IsNpgsql(_db);
        var plantas = alcance.PuedeVerInventario
            ? await PlantasVisiblesAsync(alcance, cancellationToken)
            : [];
        var plantasPedido = FiltrarPlantas(plantas, interp.NumeroPlanta);
        var ordenesVisibles = await IdsOrdenesVisiblesAsync(alcance, cancellationToken);
        var bolsa = new Bolsa();

        if (interp.NumeroOrden is int numeroOrden)
            bolsa.Agregar("Órdenes", await ItemsOrdenAsync(numeroOrden, null, ordenesVisibles, ilike, cancellationToken));
        else if (interp.TextoRestante.Length >= 2 && interp.TextoFichaTecnica.Length == 0 && interp.NivelStock is null)
            bolsa.Agregar("Órdenes", await ItemsOrdenAsync(null, interp.TextoRestante, ordenesVisibles, ilike, cancellationToken));

        if (interp.TextoFichaTecnica.Length >= 2)
            bolsa.Agregar("Fichas técnicas", await ItemsFichaTecnicaAsync(interp.TextoFichaTecnica, alcance, ilike, cancellationToken));
        else if (interp.TextoRestante.Length >= 2 && interp.NivelStock is null && !interp.QuierePersonas)
            bolsa.Agregar("Fichas técnicas", await ItemsFichaTecnicaAsync(interp.TextoRestante, alcance, ilike, cancellationToken));

        if (alcance.PuedeVerInventario && (interp.NivelStock is not null || interp.NumeroPlanta is not null || interp.TextoRestante.Length >= 2))
            bolsa.Agregar("Materiales", await ItemsMaterialAsync(interp, plantasPedido, plantas, ilike, cancellationToken));

        if (alcance.PuedeVerInventario && interp.NumeroPlanta is not null && interp.NivelStock is null && interp.TextoRestante.Length == 0)
            bolsa.Agregar("Plantas de inventario", ItemsPlanta(plantasPedido));

        if (interp.FiltroSolicitud is not null || interp.NumeroSolicitud is not null)
            bolsa.Agregar("Solicitudes", await ItemsSolicitudAsync(interp, alcance, ilike, cancellationToken));
        else if (interp.TextoRestante.Length >= 2 && interp.NivelStock is null)
            bolsa.Agregar("Solicitudes", await ItemsSolicitudAsync(interp, alcance, ilike, cancellationToken));

        if (alcance.PuedeVerUsuarios && interp.QuierePersonas)
            bolsa.Agregar("Personas", await ItemsPersonaAsync(interp.TextoPersona, ilike, cancellationToken));

        if (alcance.PuedeVerGrupos && (interp.Navegacion == NavegacionBusqueda.Grupos || (interp.TextoRestante.Length >= 2 && interp.NivelStock is null)))
            bolsa.Agregar("Grupos de confección", await ItemsGrupoAsync(interp, alcance, ilike, cancellationToken));

        if (alcance.PuedeVerMovimientos && interp.Navegacion == NavegacionBusqueda.Movimientos)
            bolsa.Agregar("Movimientos de stock", await ItemsMovimientoAsync(interp, plantasPedido, plantas, ilike, cancellationToken));

        if (alcance.PuedeVerReingreso && interp.Navegacion == NavegacionBusqueda.Reingreso)
            bolsa.Agregar("Reingreso", await ItemsReingresoAsync(interp, ordenesVisibles, ilike, cancellationToken));

        var frase = AjustarFrase(interp.Frase, interp.NumeroPlanta, plantasPedido);
        if (bolsa.Grupos.Count == 0)
            return new SugerenciasBusquedaDto(frase, [], ConsultaInterpretada.Ejemplos);

        return new SugerenciasBusquedaDto(frase, bolsa.Grupos, []);
    }

    private static bool TieneOtraPista(ConsultaInterpretada interp) =>
        interp.NumeroOrden is not null
        || interp.NumeroPlanta is not null
        || interp.NivelStock is not null
        || interp.FiltroSolicitud is not null
        || interp.NumeroSolicitud is not null
        || interp.TextoFichaTecnica.Length >= 2
        || interp.Navegacion is not null
        || interp.TextoRestante.Length >= 2;

    private async Task<List<PlantaInventario>> PlantasVisiblesAsync(AlcanceBusqueda alcance, CancellationToken ct)
    {
        var consulta = _db.PlantasInventario.AsNoTracking().Where(p => p.Activo);
        if (alcance.RestringePlantas)
        {
            var ids = alcance.PlantaInventarioIds ?? [];
            consulta = consulta.Where(p => ids.Contains(p.Id));
        }

        return await consulta.OrderBy(p => p.Nombre).ToListAsync(ct);
    }

    private static List<PlantaInventario> FiltrarPlantas(List<PlantaInventario> plantas, int? numero)
    {
        if (numero is not int n)
            return plantas;

        return plantas.Where(p => p.Id == n || NombreTieneNumero(p.Nombre, n)).ToList();
    }

    private static bool NombreTieneNumero(string nombre, int numero) =>
        System.Text.RegularExpressions.Regex.IsMatch(
            InterpreteConsulta.Normalizar(nombre),
            $@"(^| ){numero}($| )");

    private async Task<HashSet<int>?> IdsOrdenesVisiblesAsync(AlcanceBusqueda alcance, CancellationToken ct)
    {
        if (!alcance.EsInstructor || alcance.UserId is not int userId || userId <= 0)
            return null;

        var ids = new HashSet<int>();
        ids.UnionWith(await _db.ProductionOrderStages.AsNoTracking()
            .Where(s => s.InstructorUserId == userId)
            .Select(s => s.ProductionOrderId)
            .ToListAsync(ct));
        ids.UnionWith(await _db.Fichas.AsNoTracking()
            .Where(f => f.ProductionOrderId != null
                && (f.InstructorUserId == userId || f.Instructors.Any(i => i.UserId == userId)))
            .Select(f => f.ProductionOrderId!.Value)
            .ToListAsync(ct));

        var productos = await _db.BomProductInstructors.AsNoTracking()
            .Where(b => b.UserId == userId)
            .Select(b => b.BomProduct.ProductName)
            .ToListAsync(ct);
        if (productos.Count > 0)
        {
            ids.UnionWith(await _db.ProductionOrders.AsNoTracking()
                .Where(o => productos.Contains(o.ProductName))
                .Select(o => o.Id)
                .ToListAsync(ct));
        }

        return ids;
    }

    private async Task<List<ItemSugerenciaDto>> ItemsOrdenAsync(
        int? numero,
        string? texto,
        HashSet<int>? visibles,
        bool ilike,
        CancellationToken ct)
    {
        IQueryable<ProductionOrder> consulta = _db.ProductionOrders.AsNoTracking();
        if (visibles is not null)
            consulta = consulta.Where(o => visibles.Contains(o.Id));

        List<ProductionOrder> ordenes;
        if (numero is int n)
        {
            ordenes = [];
            foreach (var aguja in new[] { "op-" + n.ToString("D3"), "op-" + n, "op" + n.ToString("D3"), "op" + n })
            {
                var lote = await CoincidenciaTexto.DondeContiene(consulta, ilike, aguja, o => o.OrderNumber)
                    .OrderByDescending(o => o.Id)
                    .Take(MaxPorGrupo)
                    .ToListAsync(ct);
                ordenes.AddRange(lote.Where(o => MismoNumero(o.OrderNumber, n)));
                if (ordenes.Count > 0)
                    break;
            }
        }
        else
        {
            ordenes = await CoincidenciaTexto.DondeContiene(consulta, ilike, texto ?? "", o => o.OrderNumber, o => o.ProductName)
                .OrderByDescending(o => o.Id)
                .Take(MaxPorGrupo)
                .ToListAsync(ct);
        }

        return ordenes
            .DistinctBy(o => o.Id)
            .Take(MaxPorGrupo)
            .Select(o => new ItemSugerenciaDto(
                o.OrderNumber + " · " + o.ProductName,
                "/Ordenes/Detail/" + o.Id,
                "Detalle de la orden",
                "fa-clipboard-list"))
            .ToList();
    }

    private async Task<List<ItemSugerenciaDto>> ItemsFichaTecnicaAsync(
        string texto,
        AlcanceBusqueda alcance,
        bool ilike,
        CancellationToken ct)
    {
        IQueryable<BomProduct> consulta = _db.BomProducts.AsNoTracking();
        if (alcance.EsInstructor && !alcance.PuedeVerTodasLasFichasTecnicas && alcance.UserId is int userId)
            consulta = consulta.Where(p => p.Instructors.Any(i => i.UserId == userId));

        var fichas = await CoincidenciaTexto.DondeContiene(consulta, ilike, texto, p => p.ProductName, p => p.Codigo, p => p.Referencia)
            .OrderBy(p => p.ProductName)
            .Take(MaxPorGrupo)
            .ToListAsync(ct);

        return fichas.Select(p => new ItemSugerenciaDto(
            p.ProductName + (string.IsNullOrWhiteSpace(p.Codigo) ? "" : " · " + p.Codigo),
            "/Mrp/Materials/" + p.Id,
            "Ficha técnica",
            "fa-diagram-project")).ToList();
    }

    private async Task<List<ItemSugerenciaDto>> ItemsMaterialAsync(
        ConsultaInterpretada interp,
        List<PlantaInventario> plantasPedido,
        List<PlantaInventario> plantas,
        bool ilike,
        CancellationToken ct)
    {
        if (interp.NumeroPlanta is not null && plantasPedido.Count == 0)
            return [];

        var nombres = plantas.ToDictionary(p => p.Id, p => p.Nombre);
        IQueryable<Material> consulta = _db.Materials.AsNoTracking();
        var ids = plantasPedido.Select(p => p.Id).ToArray();
        consulta = consulta.Where(m => ids.Contains(m.PlantaInventarioId));

        var texto = interp.TextoRestante;
        if (texto.Length >= 2)
            consulta = CoincidenciaTexto.DondeContiene(consulta, ilike, texto, m => m.Name, m => m.Code);

        var materiales = await consulta.OrderBy(m => m.Name).Take(200).ToListAsync(ct);
        if (interp.NivelStock is StockNivel nivel)
            materiales = materiales.Where(m => StockNivelHelper.Classify(m.Stock, m.MinStock) == nivel).ToList();

        var items = new List<ItemSugerenciaDto>();
        if (interp.NivelStock is not null && texto.Length < 2)
        {
            foreach (var planta in plantasPedido.Take(MaxPorGrupo))
            {
                items.Add(new ItemSugerenciaDto(
                    EtiquetaNivel(interp) + " · " + planta.Nombre,
                    UrlInventario(planta.Id, null, interp.NivelStock),
                    "Inventario de " + planta.Nombre,
                    "fa-warehouse"));
            }
        }

        foreach (var material in materiales)
        {
            if (items.Count >= MaxPorGrupo)
                break;
            var planta = nombres.GetValueOrDefault(material.PlantaInventarioId) ?? "planta de inventario";
            items.Add(new ItemSugerenciaDto(
                material.Name + " · " + planta,
                UrlInventario(material.PlantaInventarioId, texto.Length >= 2 ? material.Name : null, interp.NivelStock),
                "Inventario de " + planta,
                "fa-boxes-stacked"));
        }

        return items;
    }

    private static List<ItemSugerenciaDto> ItemsPlanta(List<PlantaInventario> plantas) =>
        plantas.Take(MaxPorGrupo).Select(p => new ItemSugerenciaDto(
            p.Nombre,
            "/PlantasInventario/Detalle/" + p.Id,
            "Inventario de " + p.Nombre,
            "fa-warehouse")).ToList();

    private async Task<List<ItemSugerenciaDto>> ItemsSolicitudAsync(
        ConsultaInterpretada interp,
        AlcanceBusqueda alcance,
        bool ilike,
        CancellationToken ct)
    {
        IQueryable<SolicitudMaterial> consulta = _db.SolicitudesMaterial.AsNoTracking();
        if (alcance.EsInstructor && alcance.UserId is int userId)
        {
            consulta = consulta.Where(s =>
                s.SolicitanteId == userId
                || (s.Ficha != null && (s.Ficha.InstructorUserId == userId || s.Ficha.Instructors.Any(i => i.UserId == userId))));
        }

        if (alcance.RestringePlantas)
        {
            var ids = alcance.PlantaInventarioIds ?? [];
            consulta = consulta.Where(s => ids.Contains(s.PlantaInventarioId));
        }

        if (interp.FiltroSolicitud == FiltroSolicitudBusqueda.Pendientes)
            consulta = consulta.Where(s => s.Estado == SolicitudMaterialEstado.Pendiente);
        else if (interp.FiltroSolicitud == FiltroSolicitudBusqueda.Aprobadas)
            consulta = consulta.Where(s => s.Estado == SolicitudMaterialEstado.AprobadaTotal || s.Estado == SolicitudMaterialEstado.AprobadaParcial);
        else if (interp.FiltroSolicitud == FiltroSolicitudBusqueda.Rechazadas)
            consulta = consulta.Where(s => s.Estado == SolicitudMaterialEstado.Rechazada);

        List<SolicitudMaterial> solicitudes;
        if (interp.NumeroSolicitud is int numero)
        {
            var aguja = numero.ToString();
            solicitudes = await CoincidenciaTexto.DondeContiene(consulta, ilike, aguja, s => s.Codigo)
                .OrderByDescending(s => s.Id)
                .Take(30)
                .ToListAsync(ct);
            solicitudes = solicitudes.Where(s => MismoNumero(s.Codigo, numero)).Take(MaxPorGrupo).ToList();
        }
        else if (interp.TextoRestante.Length >= 2 && interp.FiltroSolicitud is null)
        {
            solicitudes = await CoincidenciaTexto.DondeContiene(consulta, ilike, interp.TextoRestante, s => s.Codigo)
                .OrderByDescending(s => s.Id)
                .Take(MaxPorGrupo)
                .ToListAsync(ct);
        }
        else
        {
            solicitudes = await consulta.OrderByDescending(s => s.FechaSolicitud).Take(MaxPorGrupo).ToListAsync(ct);
        }

        var items = new List<ItemSugerenciaDto>();
        if (interp.FiltroSolicitud is not null && interp.NumeroSolicitud is null)
        {
            var url = interp.FiltroSolicitud == FiltroSolicitudBusqueda.Pendientes
                ? "/PlantasInventarioSolicitudes"
                : "/PlantasInventarioSolicitudes?estado=todas";
            items.Add(new ItemSugerenciaDto(
                interp.FiltroSolicitud == FiltroSolicitudBusqueda.Pendientes
                    ? "Solicitudes pendientes"
                    : interp.FiltroSolicitud == FiltroSolicitudBusqueda.Aprobadas
                        ? "Solicitudes aprobadas"
                        : "Solicitudes rechazadas",
                url,
                "Cola de solicitudes",
                "fa-truck-ramp-box"));
        }

        foreach (var solicitud in solicitudes)
        {
            if (items.Count >= MaxPorGrupo)
                break;
            items.Add(new ItemSugerenciaDto(
                solicitud.Codigo + " · " + EstadoTexto(solicitud.Estado),
                "/PlantasInventarioSolicitudes/Detail/" + solicitud.Id,
                "Detalle de la solicitud",
                "fa-truck-ramp-box"));
        }

        return items;
    }

    private async Task<List<ItemSugerenciaDto>> ItemsPersonaAsync(string texto, bool ilike, CancellationToken ct)
    {
        if (texto.Length < 2)
            return [];

        var usuarios = await CoincidenciaTexto.DondeContiene(_db.Users.AsNoTracking(), ilike, texto, u => u.Nombre, u => u.Email)
            .OrderBy(u => u.Nombre)
            .Take(MaxPorGrupo)
            .Select(u => new { u.Id, u.Nombre, u.Rol, u.Email })
            .ToListAsync(ct);

        return usuarios.Select(u => new ItemSugerenciaDto(
            u.Nombre + " · " + u.Rol + (texto.Contains('@') ? " · " + u.Email : ""),
            "/Account/EditUser/" + u.Id,
            "Usuario",
            "fa-user")).ToList();
    }

    private async Task<List<ItemSugerenciaDto>> ItemsGrupoAsync(
        ConsultaInterpretada interp,
        AlcanceBusqueda alcance,
        bool ilike,
        CancellationToken ct)
    {
        IQueryable<GrupoConfeccion> consulta = _db.GruposConfeccion.AsNoTracking().Include(g => g.ProductionOrder);
        if (alcance.EsInstructor && alcance.UserId is int userId)
            consulta = consulta.Where(g => g.InstructorUserId == userId);

        if (interp.TextoRestante.Length >= 2 && interp.Navegacion != NavegacionBusqueda.Grupos)
        {
            consulta = CoincidenciaTexto.DondeContiene(consulta, ilike, interp.TextoRestante, g => g.ProductionOrder.OrderNumber, g => g.ProductionOrder.ProductName);
        }

        var grupos = await consulta.OrderByDescending(g => g.FechaRealizacion).Take(MaxPorGrupo).ToListAsync(ct);
        var items = new List<ItemSugerenciaDto>();
        if (interp.Navegacion == NavegacionBusqueda.Grupos)
        {
            items.Add(new ItemSugerenciaDto(
                "Grupos de confección",
                "/GruposConfeccion",
                "Grupos de confección",
                "fa-people-group"));
        }

        foreach (var grupo in grupos)
        {
            if (items.Count >= MaxPorGrupo)
                break;
            var orden = grupo.ProductionOrder?.OrderNumber ?? "orden";
            items.Add(new ItemSugerenciaDto(
                orden + " · " + grupo.CantidadPrendas + " prendas",
                "/GruposConfeccion?grupoId=" + grupo.Id,
                "Grupo de confección",
                "fa-people-group"));
        }

        return items;
    }

    private async Task<List<ItemSugerenciaDto>> ItemsMovimientoAsync(
        ConsultaInterpretada interp,
        List<PlantaInventario> plantasPedido,
        List<PlantaInventario> plantas,
        bool ilike,
        CancellationToken ct)
    {
        var items = new List<ItemSugerenciaDto>
        {
            new(
                "Movimientos de stock",
                "/PlantasInventario/Movimientos",
                "Movimientos de stock",
                "fa-clock-rotate-left")
        };

        if (interp.TextoRestante.Length < 2 || plantasPedido.Count == 0 && interp.NumeroPlanta is not null)
            return items;

        var ids = plantasPedido.Select(p => p.Id).ToArray();
        IQueryable<Material> consulta = _db.Materials.AsNoTracking()
            .Where(m => ids.Contains(m.PlantaInventarioId));

        var materiales = await CoincidenciaTexto.DondeContiene(consulta, ilike, interp.TextoRestante, m => m.Name, m => m.Code)
            .OrderBy(m => m.Name)
            .Take(MaxPorGrupo - 1)
            .ToListAsync(ct);
        var nombres = plantas.ToDictionary(p => p.Id, p => p.Nombre);
        foreach (var material in materiales)
        {
            var planta = nombres.GetValueOrDefault(material.PlantaInventarioId) ?? "planta de inventario";
            items.Add(new ItemSugerenciaDto(
                "Movimientos de " + material.Name + " · " + planta,
                "/PlantasInventario/Movimientos?plantaInventarioId=" + material.PlantaInventarioId + "&materialId=" + material.Id,
                "Movimientos de stock",
                "fa-clock-rotate-left"));
        }

        return items.Take(MaxPorGrupo).ToList();
    }

    private async Task<List<ItemSugerenciaDto>> ItemsReingresoAsync(
        ConsultaInterpretada interp,
        HashSet<int>? ordenesVisibles,
        bool ilike,
        CancellationToken ct)
    {
        var items = new List<ItemSugerenciaDto>
        {
            new(
                "Reingreso desde etapas",
                "/PlantasInventarioOrdenes/Reingreso",
                "Reingreso desde etapas",
                "fa-rotate-left")
        };

        if (interp.NumeroOrden is int numero)
        {
            foreach (var orden in await ItemsOrdenAsync(numero, null, ordenesVisibles, ilike, ct))
            {
                var id = orden.Url.Split('/').LastOrDefault();
                items.Add(new ItemSugerenciaDto(
                    "Reingreso · " + orden.Texto,
                    "/PlantasInventarioOrdenes/Reingreso?orderId=" + id,
                    "Reingreso desde etapas",
                    "fa-rotate-left"));
            }
        }

        return items.Take(MaxPorGrupo).ToList();
    }

    private static string UrlInventario(int plantaId, string? busqueda, StockNivel? nivel)
    {
        var url = "/PlantasInventario/Detalle/" + plantaId;
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(busqueda))
            partes.Add("busqueda=" + Uri.EscapeDataString(busqueda));
        if (nivel is StockNivel n)
            partes.Add("nivel=" + n);
        return partes.Count == 0 ? url : url + "?" + string.Join('&', partes);
    }

    private static string EtiquetaNivel(ConsultaInterpretada interp) => interp.NivelStock switch
    {
        StockNivel.Critico when interp.Agotados => "Agotados",
        StockNivel.Critico => "Stock crítico",
        StockNivel.Bajo => "Stock bajo",
        StockNivel.SinMinimo => "Sin mínimo definido",
        _ => "Inventario"
    };

    private static string EstadoTexto(SolicitudMaterialEstado estado) => estado switch
    {
        SolicitudMaterialEstado.Pendiente => "Pendiente",
        SolicitudMaterialEstado.AprobadaTotal => "Aprobada",
        SolicitudMaterialEstado.AprobadaParcial => "Aprobada parcial",
        SolicitudMaterialEstado.Rechazada => "Rechazada",
        _ => estado.ToString()
    };

    private static bool MismoNumero(string codigo, int numero)
    {
        var digitos = new string(codigo.Where(char.IsDigit).ToArray());
        return int.TryParse(digitos, out var valor) && valor == numero;
    }

    private static string AjustarFrase(string frase, int? numeroPlanta, List<PlantaInventario> plantas)
    {
        if (numeroPlanta is not int numero || plantas.Count != 1)
            return frase;

        var nombre = plantas[0].Nombre;
        var etiqueta = nombre.Contains("planta", StringComparison.OrdinalIgnoreCase)
            ? nombre
            : "planta de inventario " + nombre;
        return frase.Replace("la planta de inventario " + numero, etiqueta, StringComparison.Ordinal);
    }

    private sealed class Bolsa
    {
        private readonly List<GrupoSugerenciaDto> _grupos = [];
        private int _total;

        public IReadOnlyList<GrupoSugerenciaDto> Grupos => _grupos;

        public void Agregar(string tipo, List<ItemSugerenciaDto> items)
        {
            if (_total >= MaxTotal || items.Count == 0)
                return;

            var cupo = Math.Min(MaxPorGrupo, MaxTotal - _total);
            var tomados = items.Take(cupo).ToList();
            if (tomados.Count == 0)
                return;

            _grupos.Add(new GrupoSugerenciaDto(tipo, tomados));
            _total += tomados.Count;
        }
    }
}
