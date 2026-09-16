using Microsoft.EntityFrameworkCore;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Search;

// Consultas Contains() sobre materiales, órdenes, fichas y solicitudes
public class BusquedaService : IBusquedaService
{
    private const int MaxPerCategory = 5;
    private readonly SipitexDbContext _db;

    public BusquedaService(SipitexDbContext db) => _db = db;

    public async Task<IReadOnlyList<BusquedaItemDto>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var q = (query ?? string.Empty).Trim();
        if (q.Length < 1)
            return [];

        var needle = q.ToLower();
        var materials = await _db.Materials
            .AsNoTracking()
            .Where(m => m.Name.ToLower().Contains(needle) || m.Code.ToLower().Contains(needle))
            .OrderBy(m => m.Name)
            .Take(MaxPerCategory)
            .Select(m => new BusquedaItemDto(
                m.Name + " (" + m.Code + ")",
                "/Inventario",
                "Materiales"))
            .ToListAsync(cancellationToken);

        var orders = await _db.ProductionOrders
            .AsNoTracking()
            .Where(o => o.OrderNumber.ToLower().Contains(needle) || o.ProductName.ToLower().Contains(needle))
            .OrderByDescending(o => o.Id)
            .Take(MaxPerCategory)
            .Select(o => new BusquedaItemDto(
                o.OrderNumber + " · " + o.ProductName,
                "/Ordenes",
                "Órdenes"))
            .ToListAsync(cancellationToken);

        var fichas = await _db.Fichas
            .AsNoTracking()
            .Where(f => f.NumeroGrupo.ToLower().Contains(needle)
                        || f.ProcessName.ToLower().Contains(needle)
                        || f.InstructorName.ToLower().Contains(needle))
            .OrderBy(f => f.NumeroGrupo)
            .Take(MaxPerCategory)
            .Select(f => new BusquedaItemDto(
                f.NumeroGrupo + " · " + f.ProcessName,
                "/Fichas",
                "Fichas"))
            .ToListAsync(cancellationToken);

        var solicitudes = await _db.SolicitudesMaterial
            .AsNoTracking()
            .Where(s => s.Codigo.ToLower().Contains(needle)
                        || (s.Observaciones != null && s.Observaciones.ToLower().Contains(needle))
                        || s.Ficha.NumeroGrupo.ToLower().Contains(needle))
            .OrderByDescending(s => s.FechaSolicitud)
            .Take(MaxPerCategory)
            .Select(s => new BusquedaItemDto(
                s.Codigo + " · " + s.Ficha.NumeroGrupo,
                "/SolicitudesMaterial/Detail/" + s.Id,
                "Solicitudes"))
            .ToListAsync(cancellationToken);

        var prendas = await _db.PrendasTrazables
            .AsNoTracking()
            .Where(p => p.Codigo.ToLower().Contains(needle) || p.ProductName.ToLower().Contains(needle))
            .OrderByDescending(p => p.Id)
            .Take(MaxPerCategory)
            .Select(p => new BusquedaItemDto(
                p.Codigo + " · " + p.ProductName,
                "/Trazabilidad/Details/" + p.Id,
                "Trazabilidad"))
            .ToListAsync(cancellationToken);

        return materials
            .Concat(orders)
            .Concat(fichas)
            .Concat(solicitudes)
            .Concat(prendas)
            .ToList();
    }
}
