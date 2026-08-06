using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class OrderMaterialRequirementRepository : IOrderMaterialRequirementRepository
{
    private readonly SipitexDbContext _context;

    public OrderMaterialRequirementRepository(SipitexDbContext context) => _context = context;

    public async Task<IReadOnlyList<ProductionOrderMaterialRequirement>> GetByOrderIdAsync(
        int orderId,
        CancellationToken cancellationToken = default) =>
        await _context.ProductionOrderMaterialRequirements
            .Include(l => l.Material)
            .Where(l => l.ProductionOrderId == orderId)
            .OrderBy(l => l.Material!.Name)
            .ToListAsync(cancellationToken);

    public Task<ProductionOrderMaterialRequirement?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        _context.ProductionOrderMaterialRequirements
            .Include(l => l.Material)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task AddAsync(
        ProductionOrderMaterialRequirement line,
        CancellationToken cancellationToken = default) =>
        await _context.ProductionOrderMaterialRequirements.AddAsync(line, cancellationToken);

    public void Update(ProductionOrderMaterialRequirement line) =>
        _context.ProductionOrderMaterialRequirements.Update(line);

    public void Remove(ProductionOrderMaterialRequirement line) =>
        _context.ProductionOrderMaterialRequirements.Remove(line);

    public Task<bool> ExistsAsync(
        int orderId,
        int materialId,
        CancellationToken cancellationToken = default) =>
        _context.ProductionOrderMaterialRequirements
            .AnyAsync(l => l.ProductionOrderId == orderId && l.MaterialId == materialId, cancellationToken);
}
