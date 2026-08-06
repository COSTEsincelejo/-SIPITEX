using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

public class ProductionOrderService : IProductionOrderService
{
    private readonly IProductionOrderRepository _orderRepository;
    private readonly IBomRepository _bomRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ProductionConsumptionService _consumptionService;

    public ProductionOrderService(
        IProductionOrderRepository orderRepository,
        IBomRepository bomRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ProductionConsumptionService consumptionService)
    {
        _orderRepository = orderRepository;
        _bomRepository = bomRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _consumptionService = consumptionService;
    }

    public async Task<IReadOnlyList<ProductionOrderDto>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetAllAsync(cancellationToken);
        var result = new List<ProductionOrderDto>();

        foreach (var order in orders)
        {
            var bom = await _bomRepository.GetByProductAsync(order.ProductName, cancellationToken);
            var hint = bom.Count > 0
                ? string.Join(", ", bom.Select(b => $"{b.Material.Name}: {b.QuantityPerUnit} {UnitHelper.ToDisplay(b.Unit)}"))
                : "Sin ficha BOM · puede producir sin descuento automático";

            var pct = order.TotalQuantity > 0
                ? Math.Min(100, (int)Math.Round(order.ProducedQuantity * 100m / order.TotalQuantity))
                : 0;

            result.Add(new ProductionOrderDto(
                order.Id,
                order.OrderNumber,
                order.ProductName,
                order.TotalQuantity,
                order.ProducedQuantity,
                pct,
                order.Status,
                order.Deadline,
                hint,
                order.Instructor?.Nombre,
                order.InstructorId));
        }

        return result;
    }

    public async Task<ServiceResult> CreateOrderAsync(CreateProductionOrderDto dto, CancellationToken cancellationToken = default)
    {
        var productName = (dto.ProductName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(productName) || dto.TotalQuantity <= 0)
            return ServiceResult.Fail("Producto y cantidad son obligatorios.");

        if (productName.Length > 80)
            return ServiceResult.Fail("El nombre del producto no puede superar 80 caracteres.");

        int? instructorId = null;
        string? instructorName = null;
        if (dto.InstructorId is int id && id > 0)
        {
            var instructor = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (instructor is null || !instructor.IsActive || instructor.Rol != UserRoles.Instructor)
                return ServiceResult.Fail("El instructor seleccionado no es válido.");

            instructorId = instructor.Id;
            instructorName = instructor.Nombre;
        }

        var bom = await _bomRepository.GetByProductAsync(productName, cancellationToken);
        var count = await _orderRepository.CountAsync(cancellationToken);
        var orderNumber = $"OP-{(count + 101):D3}";

        await _orderRepository.AddAsync(new ProductionOrder
        {
            OrderNumber = orderNumber,
            ProductName = productName,
            TotalQuantity = dto.TotalQuantity,
            ProducedQuantity = 0,
            Status = OrderStatus.EnProceso,
            Deadline = dto.Deadline,
            InstructorId = instructorId
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var bomExtra = bom.Count == 0
            ? " (sin BOM aún: defina materiales en MRP si desea descontar stock)."
            : " (MRP calculado con ficha técnica existente).";
        var instructorExtra = instructorName is null
            ? string.Empty
            : $" Instructor: {instructorName}.";

        return ServiceResult.Ok($"Orden {orderNumber} creada para «{productName}».{bomExtra}{instructorExtra}");
    }

    public async Task<ServiceResult> RegisterProductionAsync(int orderId, int units, CancellationToken cancellationToken = default)
    {
        if (units <= 0) return ServiceResult.Fail("Cantidad inválida.");

        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null || order.Status == OrderStatus.Finalizada)
            return ServiceResult.Fail("Orden finalizada o inválida.");

        var toAdd = Math.Min(units, order.TotalQuantity - order.ProducedQuantity);
        if (toAdd <= 0) return ServiceResult.Fail("La orden ya alcanzó su meta.");

        if (!await _consumptionService.ConsumeAsync(order.ProductName, toAdd, cancellationToken))
            return ServiceResult.Fail("Consumo fallido: materiales insuficientes según la ficha BOM.");

        ProductionConsumptionService.UpdateOrderProgress(order, toAdd);
        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Se registraron {toAdd} unidades.");
    }

    public async Task<IReadOnlyList<string>> GetKnownProductNamesAsync(CancellationToken cancellationToken = default)
    {
        var fromBom = await _bomRepository.GetDistinctProductNamesAsync(cancellationToken);
        var fromOrders = await _orderRepository.GetDistinctProductNamesAsync(cancellationToken);
        return fromBom
            .Concat(fromOrders)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
