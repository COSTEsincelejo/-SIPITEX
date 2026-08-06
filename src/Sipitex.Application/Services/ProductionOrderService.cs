using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Órdenes de producción: crear (con snapshot BOM), listar y registrar avance
public class ProductionOrderService : IProductionOrderService
{
    private readonly IProductionOrderRepository _orderRepository;
    private readonly IBomRepository _bomRepository;
    private readonly IProductionOrderBomSnapshotRepository _snapshotRepository;
    private readonly IOrderMaterialRequirementRepository _requirementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ProductionConsumptionService _consumptionService;

    public ProductionOrderService(
        IProductionOrderRepository orderRepository,
        IBomRepository bomRepository,
        IProductionOrderBomSnapshotRepository snapshotRepository,
        IOrderMaterialRequirementRepository requirementRepository,
        IUnitOfWork unitOfWork,
        ProductionConsumptionService consumptionService)
    {
        _orderRepository = orderRepository;
        _bomRepository = bomRepository;
        _snapshotRepository = snapshotRepository;
        _requirementRepository = requirementRepository;
        _unitOfWork = unitOfWork;
        _consumptionService = consumptionService;
    }

    // Lista órdenes con % de avance y hint desde snapshot (o BOM vivo si no hay snapshot)
    public async Task<IReadOnlyList<ProductionOrderDto>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetAllAsync(cancellationToken);
        var result = new List<ProductionOrderDto>();

        foreach (var order in orders)
        {
            var hint = await BuildMrpHintAsync(order, cancellationToken);
            var pct = order.TotalQuantity > 0
                ? Math.Min(100, (int)Math.Round(order.ProducedQuantity * 100m / order.TotalQuantity))
                : 0;
            var reqs = await _requirementRepository.GetByOrderIdAsync(order.Id, cancellationToken);

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
                order.MaterialsStatus,
                reqs.Count > 0,
                OrderMaterialService.CanRegisterProduction(order)));
        }

        return result;
    }

    public async Task<ServiceResult> CreateOrderAsync(CreateProductionOrderDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.ProductName) || dto.TotalQuantity <= 0)
            return ServiceResult.Fail("Producto y cantidad son obligatorios.");

        var productName = dto.ProductName.Trim();
        var product = await _bomRepository.GetProductByNameAsync(productName, cancellationToken);
        if (product is null || product.Items.Count == 0)
            return ServiceResult.Fail("Producto no válido. Seleccione un producto con ficha técnica.");

        if (!product.HabilitadoParaOrdenes)
            return ServiceResult.Fail(
                $"El producto «{product.ProductName}» tiene ficha técnica incompleta o no habilitada para órdenes de producción.");

        var count = await _orderRepository.CountAsync(cancellationToken);
        var orderNumber = $"OP-{(count + 101):D3}";

        var order = new ProductionOrder
        {
            OrderNumber = orderNumber,
            ProductName = product.ProductName,
            TotalQuantity = dto.TotalQuantity,
            ProducedQuantity = 0,
            Status = OrderStatus.EnProceso,
            MaterialsStatus = OrderMaterialsStatus.NoAplica,
            Deadline = dto.Deadline
        };

        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken); // Necesito order.Id para el snapshot

        var snapshots = product.Items.Select(item => new ProductionOrderBomSnapshot
        {
            ProductionOrderId = order.Id,
            MaterialId = item.MaterialId,
            MaterialCode = item.Material.Code,
            MaterialName = item.Material.Name,
            QuantityPerUnit = item.QuantityPerUnit,
            Unit = item.Unit
        }).ToList();

        await _snapshotRepository.AddRangeAsync(snapshots, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Orden {orderNumber} creada.");
    }

    public async Task<ServiceResult> RegisterProductionAsync(int orderId, int units, CancellationToken cancellationToken = default)
    {
        if (units <= 0) return ServiceResult.Fail("Cantidad inválida.");

        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null || order.Status == OrderStatus.Finalizada)
            return ServiceResult.Fail("Orden finalizada o inválida.");

        // Gate: si la orden exige materiales de bodega, deben estar entregados por completo
        if (!OrderMaterialService.CanRegisterProduction(order))
            return ServiceResult.Fail(
                "No se puede iniciar producción: hay materiales pendientes de entrega en bodega.");

        var toAdd = Math.Min(units, order.TotalQuantity - order.ProducedQuantity);
        if (toAdd <= 0) return ServiceResult.Fail("La orden ya alcanzó su meta.");

        // Si bodega ya entregó los materiales de la orden, no volver a descontar BOM (evita doble consumo)
        if (!OrderMaterialService.UsesWarehouseIssuedMaterials(order))
        {
            var recipe = await ResolveRecipeForOrderAsync(order, cancellationToken);
            if (!await _consumptionService.ConsumeRecipeAsync(recipe, toAdd, cancellationToken))
                return ServiceResult.Fail("Consumo fallido: materiales insuficientes.");
        }

        ProductionConsumptionService.UpdateOrderProgress(order, toAdd);
        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Se registraron {toAdd} unidades.");
    }

    private async Task<string> BuildMrpHintAsync(ProductionOrder order, CancellationToken cancellationToken)
    {
        var snapshots = await _snapshotRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        if (snapshots.Count > 0)
        {
            return string.Join(", ", snapshots.Select(s =>
                $"{s.MaterialName}: {s.QuantityPerUnit} {UnitHelper.ToDisplay(s.Unit)}"));
        }

        // Fallback legacy (órdenes anteriores al snapshot)
        var bom = await _bomRepository.GetByProductAsync(order.ProductName, cancellationToken);
        return bom.Count > 0
            ? string.Join(", ", bom.Select(b => $"{b.Material.Name}: {b.QuantityPerUnit} {UnitHelper.ToDisplay(b.Unit)}"))
            : "N/A";
    }

    private async Task<IReadOnlyList<ProductionConsumptionService.RecipeLine>> ResolveRecipeForOrderAsync(
        ProductionOrder order,
        CancellationToken cancellationToken)
    {
        var snapshots = await _snapshotRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        if (snapshots.Count > 0)
        {
            return snapshots
                .Select(s => new ProductionConsumptionService.RecipeLine(s.MaterialId, s.QuantityPerUnit))
                .ToList();
        }

        var bom = await _bomRepository.GetByProductAsync(order.ProductName, cancellationToken);
        return bom
            .Select(b => new ProductionConsumptionService.RecipeLine(b.MaterialId, b.QuantityPerUnit))
            .ToList();
    }
}
