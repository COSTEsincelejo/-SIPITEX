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

        // SQLite: Contains se traduce a LIKE '%q%'
        var materials = await _db.Materials
            .AsNoTracking()
            .Where(m => m.Name.Contains(q) || m.Code.Contains(q))
            .OrderBy(m => m.Name)
            .Take(MaxPerCategory)
            .Select(m => new BusquedaItemDto(
                m.Name + " (" + m.Code + ")",
                "/Inventario",
                "Materiales"))
            .ToListAsync(cancellationToken);

        var orders = await _db.ProductionOrders
            .AsNoTracking()
            .Where(o => o.OrderNumber.Contains(q) || o.ProductName.Contains(q))
            .OrderByDescending(o => o.Id)
            .Take(MaxPerCategory)
            .Select(o => new BusquedaItemDto(
                o.OrderNumber + " · " + o.ProductName,
                "/Ordenes",
                "Órdenes"))
            .ToListAsync(cancellationToken);

        var fichas = await _db.Fichas
            .AsNoTracking()
            .Where(f => f.FichaCode.Contains(q) || f.ProcessName.Contains(q) || f.InstructorName.Contains(q))
            .OrderBy(f => f.FichaCode)
            .Take(MaxPerCategory)
            .Select(f => new BusquedaItemDto(
                f.FichaCode + " · " + f.ProcessName,
                "/Fichas",
                "Fichas"))
            .ToListAsync(cancellationToken);

        var solicitudes = await _db.SolicitudesMaterial
            .AsNoTracking()
            .Where(s => s.Codigo.Contains(q)
                        || (s.Observaciones != null && s.Observaciones.Contains(q))
                        || s.Ficha.FichaCode.Contains(q))
            .OrderByDescending(s => s.FechaSolicitud)
            .Take(MaxPerCategory)
            .Select(s => new BusquedaItemDto(
                s.Codigo + " · " + s.Ficha.FichaCode,
                "/SolicitudesMaterial/Detail/" + s.Id,
                "Solicitudes"))
            .ToListAsync(cancellationToken);

        return materials
            .Concat(orders)
            .Concat(fichas)
            .Concat(solicitudes)
            .ToList();
    }
}
