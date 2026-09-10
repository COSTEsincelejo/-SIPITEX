using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class ConsumoMaterialRepository : IConsumoMaterialRepository
{
    private readonly SipitexDbContext _context;

    public ConsumoMaterialRepository(SipitexDbContext context) => _context = context;

    public async Task AddAsync(ConsumoMaterial consumo, CancellationToken cancellationToken = default) =>
        await _context.ConsumosMaterial.AddAsync(consumo, cancellationToken);

    public async Task<IReadOnlyList<ConsumoMaterial>> GetByOrderIdAsync(
        int productionOrderId,
        CancellationToken cancellationToken = default) =>
        await _context.ConsumosMaterial
            .AsNoTracking()
            .Include(c => c.ProductionOrder)
            .Include(c => c.Material)
            .Include(c => c.Responsable)
            .Where(c => c.ProductionOrderId == productionOrderId)
            .OrderByDescending(c => c.FechaUtc)
            .ThenByDescending(c => c.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ConsumoMaterial>> GetByMaterialIdAsync(
        int materialId,
        CancellationToken cancellationToken = default) =>
        await _context.ConsumosMaterial
            .AsNoTracking()
            .Include(c => c.ProductionOrder)
            .Include(c => c.Material)
            .Include(c => c.Responsable)
            .Where(c => c.MaterialId == materialId)
            .OrderByDescending(c => c.FechaUtc)
            .ToListAsync(cancellationToken);
}
