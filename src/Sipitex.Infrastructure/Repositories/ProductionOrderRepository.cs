using Microsoft.EntityFrameworkCore; // OrderBy, FirstOrDefaultAsync, CountAsync...
using Sipitex.Application.Interfaces.Repositories; // IProductionOrderRepository
using Sipitex.Domain.Entities; // ProductionOrder
using Sipitex.Infrastructure.Persistence; // SipitexDbContext

namespace Sipitex.Infrastructure.Repositories;

// Repositorio de órdenes de producción
public class ProductionOrderRepository : IProductionOrderRepository
{
    private readonly SipitexDbContext _context;

    public ProductionOrderRepository(SipitexDbContext context) => _context = context;

    // Todas las órdenes ordenadas por número OP-xxx
    public async Task<IReadOnlyList<ProductionOrder>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.ProductionOrders.OrderBy(o => o.OrderNumber).ToListAsync(cancellationToken);

    // Busca por Id interno
    public Task<ProductionOrder?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.ProductionOrders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    // Lo uso cuando necesito buscar por el código OP-xxx y no por el Id
    public Task<ProductionOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default) =>
        _context.ProductionOrders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

    // Crea una orden nueva
    public async Task AddAsync(ProductionOrder order, CancellationToken cancellationToken = default) =>
        await _context.ProductionOrders.AddAsync(order, cancellationToken);

    // Actualiza cantidad producida, estado, etc.
    public void Update(ProductionOrder order) => _context.ProductionOrders.Update(order);

    // Para armar el siguiente número de orden (OP-101, OP-102...)
    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _context.ProductionOrders.CountAsync(cancellationToken);
}
