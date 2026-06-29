using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class ProductionOrderRepository : IProductionOrderRepository
{
    private readonly SipitexDbContext _context;

    public ProductionOrderRepository(SipitexDbContext context) => _context = context;

    public async Task<IReadOnlyList<ProductionOrder>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.ProductionOrders.OrderBy(o => o.OrderNumber).ToListAsync(cancellationToken);

    public Task<ProductionOrder?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.ProductionOrders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<ProductionOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default) =>
        _context.ProductionOrders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

    public async Task AddAsync(ProductionOrder order, CancellationToken cancellationToken = default) =>
        await _context.ProductionOrders.AddAsync(order, cancellationToken);

    public void Update(ProductionOrder order) => _context.ProductionOrders.Update(order);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _context.ProductionOrders.CountAsync(cancellationToken);
}
