using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Órdenes de producción: crear, listar y registrar avance
public class ProductionOrderService : IProductionOrderService
{
    private readonly IProductionOrderRepository _orderRepository;
    private readonly IBomRepository _bomRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ProductionConsumptionService _consumptionService;

    public ProductionOrderService(
        IProductionOrderRepository orderRepository,
        IBomRepository bomRepository,
        IUnitOfWork unitOfWork,
        ProductionConsumptionService consumptionService)
    {
        _orderRepository = orderRepository;
        _bomRepository = bomRepository;
        _unitOfWork = unitOfWork;
        _consumptionService = consumptionService;
    }

    // Lista órdenes con % de avance y un hint del BOM para la vista
    public async Task<IReadOnlyList<ProductionOrderDto>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        // Todas las órdenes de la BD
        var orders = await _orderRepository.GetAllAsync(cancellationToken);
        var result = new List<ProductionOrderDto>();

        foreach (var order in orders)
        {
            // Traigo la receta del producto para mostrar hint en la tabla
            var bom = await _bomRepository.GetByProductAsync(order.ProductName, cancellationToken);
            // Texto tipo "Tela: 2 m, Hilo: 1 rollo" o N/A
            var hint = bom.Count > 0
                ? string.Join(", ", bom.Select(b => $"{b.Material.Name}: {b.QuantityPerUnit} {UnitHelper.ToDisplay(b.Unit)}"))
                : "N/A";

            // Calculo porcentaje de avance (máximo 100)
            var pct = order.TotalQuantity > 0
                ? Math.Min(100, (int)Math.Round(order.ProducedQuantity * 100m / order.TotalQuantity))
                : 0;

            // Agrego un DTO por cada orden
            result.Add(new ProductionOrderDto(
                order.Id,
                order.OrderNumber,
                order.ProductName,
                order.TotalQuantity,
                order.ProducedQuantity,
                pct,
                order.Status,
                order.Deadline,
                hint));
        }

        return result;
    }

    // Nueva orden: el producto tiene que existir en el BOM (Camisa o Pantalón en el seed)
    public async Task<ServiceResult> CreateOrderAsync(CreateProductionOrderDto dto, CancellationToken cancellationToken = default)
    {
        // Validación de producto y cantidad positiva
        if (string.IsNullOrWhiteSpace(dto.ProductName) || dto.TotalQuantity <= 0)
            return ServiceResult.Fail("Producto y cantidad son obligatorios.");

        // El producto debe tener receta en el BOM
        var bom = await _bomRepository.GetByProductAsync(dto.ProductName, cancellationToken);
        if (bom.Count == 0)
            return ServiceResult.Fail("Producto no válido. Usa Camisa o Pantalón.");

        // Número correlativo tipo OP-101, OP-102...
        var count = await _orderRepository.CountAsync(cancellationToken);
        var orderNumber = $"OP-{(count + 101):D3}";

        // INSERT de la orden nueva
        await _orderRepository.AddAsync(new ProductionOrder
        {
            OrderNumber = orderNumber,
            ProductName = dto.ProductName.Trim(),
            TotalQuantity = dto.TotalQuantity,
            ProducedQuantity = 0,
            Status = OrderStatus.EnProceso,
            Deadline = dto.Deadline
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Orden {orderNumber} creada.");
    }

    // Registra unidades producidas y descuenta materiales
    public async Task<ServiceResult> RegisterProductionAsync(int orderId, int units, CancellationToken cancellationToken = default)
    {
        // Cantidad tiene que ser mayor a cero
        if (units <= 0) return ServiceResult.Fail("Cantidad inválida.");

        // Busco la orden y verifico que no esté finalizada
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null || order.Status == OrderStatus.Finalizada)
            return ServiceResult.Fail("Orden finalizada o inválida.");

        // No dejo pasar de la meta total
        var toAdd = Math.Min(units, order.TotalQuantity - order.ProducedQuantity);
        if (toAdd <= 0) return ServiceResult.Fail("La orden ya alcanzó su meta.");

        // Intenta consumir materiales del BOM
        if (!await _consumptionService.ConsumeAsync(order.ProductName, toAdd, cancellationToken))
            return ServiceResult.Fail("Consumo fallido: materiales insuficientes.");

        // Actualizo avance y estado de la orden
        ProductionConsumptionService.UpdateOrderProgress(order, toAdd);
        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Se registraron {toAdd} unidades.");
    }
}
