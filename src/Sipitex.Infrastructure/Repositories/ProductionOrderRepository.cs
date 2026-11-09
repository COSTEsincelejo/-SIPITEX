using System.Data;
using Microsoft.EntityFrameworkCore; // OrderBy, FirstOrDefaultAsync
using Microsoft.EntityFrameworkCore.Storage; // GetDbTransaction
using Sipitex.Application.Interfaces.Repositories; // IProductionOrderRepository
using Sipitex.Domain.Entities; // ProductionOrder, NumeracionDocumento
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

    // Reserva el siguiente OP-xxx en la fila NumeracionesDocumento.
    // El UPDATE ... RETURNING es atómico: la segunda alta espera el bloqueo de esa fila
    // y recibe el número siguiente. No usa Count + 101, que repetía el número si dos
    // usuarios creaban la orden a la vez. El índice único de OrderNumber queda como red de seguridad.
    public async Task<string> NextOrderNumberAsync(CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        var close = connection.State != ConnectionState.Open;
        if (close)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                UPDATE "NumeracionesDocumento"
                SET "Siguiente" = "Siguiente" + 1
                WHERE "Codigo" = @codigo
                RETURNING "Siguiente" - 1
                """;
            var codigo = command.CreateParameter();
            codigo.ParameterName = "codigo";
            codigo.Value = NumeracionDocumento.OrdenProduccion;
            command.Parameters.Add(codigo);
            if (_context.Database.CurrentTransaction is { } transaction)
                command.Transaction = transaction.GetDbTransaction();

            var scalar = await command.ExecuteScalarAsync(cancellationToken);
            if (scalar is null or DBNull)
                throw new InvalidOperationException(
                    "No está configurada la numeración de órdenes de producción.");

            return $"OP-{Convert.ToInt32(scalar):D3}";
        }
        finally
        {
            if (close)
                await connection.CloseAsync();
        }
    }
}
