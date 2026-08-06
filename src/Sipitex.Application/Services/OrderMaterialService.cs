using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Asocia materiales de inventario a una orden y gestiona entrega desde bodega
public class OrderMaterialService : IOrderMaterialService
{
    private readonly IProductionOrderRepository _orderRepository;
    private readonly IOrderMaterialRequirementRepository _requirementRepository;
    private readonly IMaterialRepository _materialRepository;
    private readonly IProductionOrderBomSnapshotRepository _snapshotRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OrderMaterialService(
        IProductionOrderRepository orderRepository,
        IOrderMaterialRequirementRepository requirementRepository,
        IMaterialRepository materialRepository,
        IProductionOrderBomSnapshotRepository snapshotRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _requirementRepository = requirementRepository;
        _materialRepository = materialRepository;
        _snapshotRepository = snapshotRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderMaterialsDetailDto?> GetDetailAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null) return null;

        var lines = await _requirementRepository.GetByOrderIdAsync(orderId, cancellationToken);
        return MapDetail(order, lines);
    }

    public async Task<IReadOnlyList<ProductionOrderDto>> GetOrdersForBodegaAsync(
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetAllAsync(cancellationToken);
        var result = new List<ProductionOrderDto>();

        foreach (var order in orders)
        {
            if (order.MaterialsStatus == OrderMaterialsStatus.NoAplica
                || order.MaterialsStatus == OrderMaterialsStatus.ListaParaProduccion)
                continue;
            if (order.Status is OrderStatus.Finalizada or OrderStatus.Cancelada)
                continue;

            var lines = await _requirementRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            if (lines.Count == 0) continue;

            result.Add(ToOrderDto(order, lines));
        }

        return result;
    }

    public async Task<ServiceResult> AddMaterialAsync(
        AddOrderMaterialDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.QuantityRequired <= 0)
            return ServiceResult.Fail("La cantidad requerida debe ser mayor que cero.");

        var order = await _orderRepository.GetByIdAsync(dto.OrderId, cancellationToken);
        if (order is null)
            return ServiceResult.Fail("Orden no encontrada.");
        if (order.Status is OrderStatus.Finalizada or OrderStatus.Cancelada)
            return ServiceResult.Fail("No se pueden asociar materiales a una orden cerrada.");

        var material = await _materialRepository.GetByIdAsync(dto.MaterialId, cancellationToken);
        if (material is null)
            return ServiceResult.Fail("Seleccione un material del inventario existente.");

        if (await _requirementRepository.ExistsAsync(dto.OrderId, dto.MaterialId, cancellationToken))
            return ServiceResult.Fail("Ese material ya está asociado a la orden. Edite la cantidad o elimínelo.");

        var line = new ProductionOrderMaterialRequirement
        {
            ProductionOrderId = order.Id,
            MaterialId = material.Id,
            QuantityRequired = dto.QuantityRequired,
            QuantityDelivered = 0,
            Unit = material.Unit,
            Observations = string.IsNullOrWhiteSpace(dto.Observations) ? null : dto.Observations.Trim()
        };

        await _requirementRepository.AddAsync(line, cancellationToken);

        if (order.MaterialsStatus == OrderMaterialsStatus.NoAplica
            || order.MaterialsStatus == OrderMaterialsStatus.ListaParaProduccion)
        {
            order.MaterialsStatus = OrderMaterialsStatus.PendienteRevisionBodega;
            _orderRepository.Update(order);
        }
        else if (order.MaterialsStatus == OrderMaterialsStatus.MaterialesValidados
                 || order.MaterialsStatus == OrderMaterialsStatus.EntregaParcial)
        {
            // Nuevo requisito pendiente → vuelve a revisión
            order.MaterialsStatus = OrderMaterialsStatus.PendienteRevisionBodega;
            _orderRepository.Update(order);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Material «{material.Name}» asociado a {order.OrderNumber}.");
    }

    public async Task<ServiceResult> RemoveMaterialAsync(
        int lineId,
        CancellationToken cancellationToken = default)
    {
        var line = await _requirementRepository.GetByIdAsync(lineId, cancellationToken);
        if (line is null)
            return ServiceResult.Fail("Línea de material no encontrada.");

        if (line.QuantityDelivered > 0)
            return ServiceResult.Fail("No se puede eliminar un material con entregas registradas.");

        var order = await _orderRepository.GetByIdAsync(line.ProductionOrderId, cancellationToken);
        if (order is null)
            return ServiceResult.Fail("Orden no encontrada.");

        _requirementRepository.Remove(line);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var remaining = await _requirementRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        RecalcMaterialsStatus(order, remaining);
        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok("Material desasociado de la orden.");
    }

    public async Task<ServiceResult> ImportFromBomAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
            return ServiceResult.Fail("Orden no encontrada.");
        if (order.Status is OrderStatus.Finalizada or OrderStatus.Cancelada)
            return ServiceResult.Fail("Orden cerrada.");

        var snapshots = await _snapshotRepository.GetByOrderIdAsync(orderId, cancellationToken);
        if (snapshots.Count == 0)
            return ServiceResult.Fail("La orden no tiene snapshot BOM para sugerir materiales.");

        var existing = await _requirementRepository.GetByOrderIdAsync(orderId, cancellationToken);
        var existingMaterialIds = existing.Select(e => e.MaterialId).ToHashSet();
        var added = 0;

        foreach (var snap in snapshots)
        {
            if (existingMaterialIds.Contains(snap.MaterialId))
                continue;

            var required = snap.QuantityPerUnit * order.TotalQuantity;
            if (required <= 0) continue;

            await _requirementRepository.AddAsync(new ProductionOrderMaterialRequirement
            {
                ProductionOrderId = order.Id,
                MaterialId = snap.MaterialId,
                QuantityRequired = required,
                QuantityDelivered = 0,
                Unit = snap.Unit,
                Observations = "Importado desde BOM de la orden"
            }, cancellationToken);
            added++;
        }

        if (added == 0)
            return ServiceResult.Fail("No hay materiales nuevos del BOM para agregar.");

        if (order.MaterialsStatus == OrderMaterialsStatus.NoAplica
            || order.MaterialsStatus == OrderMaterialsStatus.ListaParaProduccion)
        {
            order.MaterialsStatus = OrderMaterialsStatus.PendienteRevisionBodega;
            _orderRepository.Update(order);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Se agregaron {added} material(es) desde el BOM.");
    }

    public async Task<ServiceResult> ValidateStockAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
            return ServiceResult.Fail("Orden no encontrada.");

        var lines = await _requirementRepository.GetByOrderIdAsync(orderId, cancellationToken);
        if (lines.Count == 0)
            return ServiceResult.Fail("La orden no tiene materiales asociados.");

        if (order.MaterialsStatus == OrderMaterialsStatus.ListaParaProduccion)
            return ServiceResult.Ok("Los materiales ya fueron entregados por completo.");

        var shortfalls = lines
            .Where(l => !l.IsFullyDelivered)
            .Select(l =>
            {
                var pending = l.QuantityPending;
                var stock = l.Material?.Stock ?? 0;
                return (Line: l, Stock: stock, Pending: pending, Ok: stock >= pending);
            })
            .ToList();

        order.MaterialsStatus = OrderMaterialsStatus.MaterialesValidados;
        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var missing = shortfalls.Where(s => !s.Ok).ToList();
        if (missing.Count == 0)
            return ServiceResult.Ok("Stock validado: hay disponibilidad para cubrir lo pendiente.");

        var detail = string.Join("; ", missing.Select(m =>
            $"{m.Line.Material?.Name ?? "Material"} (falta {m.Pending - m.Stock:0.##})"));
        return ServiceResult.Ok($"Stock validado con faltantes: {detail}");
    }

    public async Task<ServiceResult> DeliverAsync(
        DeliverOrderMaterialsDto dto,
        int bodegueroId,
        CancellationToken cancellationToken = default)
    {
        if (bodegueroId <= 0)
            return ServiceResult.Fail("Bodeguero no válido.");

        var order = await _orderRepository.GetByIdAsync(dto.OrderId, cancellationToken);
        if (order is null)
            return ServiceResult.Fail("Orden no encontrada.");

        if (order.Status is OrderStatus.Finalizada or OrderStatus.Cancelada)
            return ServiceResult.Fail("No se pueden entregar materiales a una orden cerrada.");

        var lines = (await _requirementRepository.GetByOrderIdAsync(dto.OrderId, cancellationToken)).ToList();
        if (lines.Count == 0)
            return ServiceResult.Fail("La orden no tiene materiales asociados.");

        var decisions = (dto.Items ?? [])
            .GroupBy(i => i.LineId)
            .ToDictionary(g => g.Key, g => g.Last().QuantityToDeliver);

        if (decisions.Values.All(q => q <= 0))
            return ServiceResult.Fail("Indique al menos una cantidad a entregar mayor que cero.");

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                foreach (var line in lines)
                {
                    if (!decisions.TryGetValue(line.Id, out var qty) || qty <= 0)
                        continue;

                    if (line.IsFullyDelivered)
                        throw new InvalidOperationException(
                            $"«{line.Material?.Name}» ya fue entregado por completo.");

                    var maxDeliver = Math.Min(line.QuantityPending, line.Material.Stock);
                    if (qty > maxDeliver)
                        throw new InvalidOperationException(
                            $"No se puede entregar {qty:0.##} de «{line.Material?.Name}». Máximo: {maxDeliver:0.##}.");

                    line.Material.Stock -= qty;
                    if (line.Material.Stock < 0)
                        throw new InvalidOperationException("El inventario no puede quedar negativo.");

                    line.QuantityDelivered += qty;
                    _materialRepository.Update(line.Material);
                    _requirementRepository.Update(line);
                }

                RecalcMaterialsStatus(order, lines);
                _orderRepository.Update(order);
                await Task.CompletedTask;
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.Fail(ex.Message);
        }

        var refreshed = await _requirementRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        var pending = refreshed.Where(l => !l.IsFullyDelivered).ToList();
        if (pending.Count == 0)
            return ServiceResult.Ok($"Entrega completa. {order.OrderNumber} lista para producción.");

        var pendingNames = string.Join(", ", pending.Select(p => p.Material?.Name ?? $"#{p.MaterialId}"));
        return ServiceResult.Ok($"Entrega parcial registrada. Pendientes: {pendingNames}.");
    }

    internal static bool CanRegisterProduction(ProductionOrder order) =>
        order.Status != OrderStatus.Finalizada
        && order.Status != OrderStatus.Cancelada
        && (order.MaterialsStatus == OrderMaterialsStatus.NoAplica
            || order.MaterialsStatus == OrderMaterialsStatus.ListaParaProduccion);

    internal static bool UsesWarehouseIssuedMaterials(ProductionOrder order) =>
        order.MaterialsStatus != OrderMaterialsStatus.NoAplica;

    private static void RecalcMaterialsStatus(
        ProductionOrder order,
        IReadOnlyList<ProductionOrderMaterialRequirement> lines)
    {
        if (lines.Count == 0)
        {
            order.MaterialsStatus = OrderMaterialsStatus.NoAplica;
            return;
        }

        if (lines.All(l => l.IsFullyDelivered))
        {
            order.MaterialsStatus = OrderMaterialsStatus.ListaParaProduccion;
            return;
        }

        if (lines.Any(l => l.QuantityDelivered > 0))
        {
            order.MaterialsStatus = OrderMaterialsStatus.EntregaParcial;
            return;
        }

        // Conserva MaterialesValidados si ya se validó y aún no hay entregas
        if (order.MaterialsStatus != OrderMaterialsStatus.MaterialesValidados)
            order.MaterialsStatus = OrderMaterialsStatus.PendienteRevisionBodega;
    }

    private static OrderMaterialsDetailDto MapDetail(
        ProductionOrder order,
        IReadOnlyList<ProductionOrderMaterialRequirement> lines)
    {
        var mapped = lines.Select(MapLine).ToList();
        var canEdit = order.Status is not (OrderStatus.Finalizada or OrderStatus.Cancelada);

        return new OrderMaterialsDetailDto(
            order.Id,
            order.OrderNumber,
            order.ProductName,
            order.Status,
            order.MaterialsStatus,
            order.TotalQuantity,
            order.ProducedQuantity,
            CanRegisterProduction(order),
            canEdit,
            mapped);
    }

    private static OrderMaterialLineDto MapLine(ProductionOrderMaterialRequirement line)
    {
        var stock = line.Material?.Stock ?? 0;
        var pending = line.QuantityPending;
        var availability = ResolveAvailability(stock, pending, line.IsFullyDelivered);

        return new OrderMaterialLineDto(
            line.Id,
            line.MaterialId,
            line.Material?.Code ?? "",
            line.Material?.Name ?? "",
            line.QuantityRequired,
            line.QuantityDelivered,
            pending,
            stock,
            stock - pending,
            UnitHelper.ToDisplay(line.Unit),
            line.Unit,
            line.Observations,
            availability,
            line.IsFullyDelivered);
    }

    private static MaterialStockAvailability ResolveAvailability(
        decimal stock,
        decimal pending,
        bool fullyDelivered)
    {
        if (fullyDelivered || pending <= 0)
            return MaterialStockAvailability.Suficiente;
        if (stock <= 0)
            return MaterialStockAvailability.SinExistencias;
        if (stock < pending)
            return MaterialStockAvailability.Insuficiente;
        return MaterialStockAvailability.Suficiente;
    }

    private static ProductionOrderDto ToOrderDto(
        ProductionOrder order,
        IReadOnlyList<ProductionOrderMaterialRequirement> lines)
    {
        var pct = order.TotalQuantity > 0
            ? Math.Min(100, (int)Math.Round(order.ProducedQuantity * 100m / order.TotalQuantity))
            : 0;

        return new ProductionOrderDto(
            order.Id,
            order.OrderNumber,
            order.ProductName,
            order.TotalQuantity,
            order.ProducedQuantity,
            pct,
            order.Status,
            order.Deadline,
            $"{lines.Count} material(es)",
            order.MaterialsStatus,
            lines.Count > 0,
            CanRegisterProduction(order));
    }
}
