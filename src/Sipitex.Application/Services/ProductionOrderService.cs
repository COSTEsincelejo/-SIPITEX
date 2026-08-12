using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Órdenes de producción: crear (con snapshot BOM), editar, cancelar, listar y registrar avance
public class ProductionOrderService : IProductionOrderService
{
    private readonly IProductionOrderRepository _orderRepository;
    private readonly IBomRepository _bomRepository;
    private readonly IProductionOrderBomSnapshotRepository _snapshotRepository;
    private readonly IOrderMaterialRequirementRepository _requirementRepository;
    private readonly IProductionFlowRepository _flowRepository;
    private readonly IProductionFlowService _flowService;
    private readonly IOrderChangeLogRepository _changeLogs;
    private readonly IFichaRepository _fichaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ProductionConsumptionService _consumptionService;

    public ProductionOrderService(
        IProductionOrderRepository orderRepository,
        IBomRepository bomRepository,
        IProductionOrderBomSnapshotRepository snapshotRepository,
        IOrderMaterialRequirementRepository requirementRepository,
        IProductionFlowRepository flowRepository,
        IProductionFlowService flowService,
        IOrderChangeLogRepository changeLogs,
        IFichaRepository fichaRepository,
        IUnitOfWork unitOfWork,
        ProductionConsumptionService consumptionService)
    {
        _orderRepository = orderRepository;
        _bomRepository = bomRepository;
        _snapshotRepository = snapshotRepository;
        _requirementRepository = requirementRepository;
        _flowRepository = flowRepository;
        _flowService = flowService;
        _changeLogs = changeLogs;
        _fichaRepository = fichaRepository;
        _unitOfWork = unitOfWork;
        _consumptionService = consumptionService;
    }

    // Lista órdenes con % de avance y hint desde snapshot (o BOM vivo si no hay snapshot)
    public async Task<IReadOnlyList<ProductionOrderDto>> GetOrdersAsync(
        int? viewerUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetAllAsync(cancellationToken);

        if (IsInstructorViewer(viewerRole, viewerUserId))
        {
            var assignedIds = await GetAssignedOrderIdsAsync(
                viewerUserId!.Value, viewerName, cancellationToken);
            orders = orders.Where(o => assignedIds.Contains(o.Id)).ToList();
        }

        var result = new List<ProductionOrderDto>();

        foreach (var order in orders)
        {
            var hint = await BuildMrpHintAsync(order, cancellationToken);
            var pct = order.TotalQuantity > 0
                ? Math.Min(100, (int)Math.Round(order.ProducedQuantity * 100m / order.TotalQuantity))
                : 0;
            var reqs = await _requirementRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            var stages = await _flowRepository.GetStagesByOrderAsync(order.Id, cancellationToken);
            var flowPct = stages.Count == 0
                ? 0
                : Math.Min(100, (int)Math.Round(
                    stages.Count(s => s.Status == ProductionStageStatus.Finalizado) * 100m / stages.Count));
            var combined = stages.Count == 0 ? pct : (pct + flowPct) / 2;
            var currentName = stages.FirstOrDefault(s => s.Id == order.CurrentStageId)?.Name
                              ?? stages.FirstOrDefault(s => s.Status != ProductionStageStatus.Finalizado)?.Name;

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
                OrderMaterialService.CanRegisterProduction(order),
                order.ClientName,
                currentName,
                flowPct,
                combined));
        }

        return result;
    }

    public async Task<bool> CanAccessOrderAsync(
        int orderId,
        int? viewerUserId,
        string? viewerRole,
        string? viewerName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(viewerRole, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase)
            || string.Equals(viewerRole, UserRoles.Bodeguero, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!IsInstructorViewer(viewerRole, viewerUserId))
            return false;

        var assignedIds = await GetAssignedOrderIdsAsync(
            viewerUserId!.Value, viewerName, cancellationToken);
        return assignedIds.Contains(orderId);
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
            ClientName = string.IsNullOrWhiteSpace(dto.ClientName) ? null : dto.ClientName.Trim(),
            TotalQuantity = dto.TotalQuantity,
            ProducedQuantity = 0,
            Status = OrderStatus.Pendiente,
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

        // Inicializa flujo MES (plantilla del producto o default) — no altera BOM/MRP
        await _flowService.EnsureStagesForOrderAsync(order.Id, "Sistema", cancellationToken);

        // Instructor creador: responsable en todas las etapas (compatible con filtro de alcance por InstructorUserId)
        if (dto.ResponsibleInstructorUserId is int instructorId && instructorId > 0)
        {
            var stages = await _flowRepository.GetStagesByOrderAsync(order.Id, cancellationToken);
            foreach (var stage in stages)
            {
                stage.InstructorUserId = instructorId;
                _flowRepository.UpdateStage(stage);
            }

            if (stages.Count > 0)
                await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult.Ok($"Orden {orderNumber} creada (pendiente de aprobación).");
    }

    // Administrador: Pendiente → EnProceso (sin pasos adicionales sobre MES/materiales)
    public async Task<ServiceResult> ApproveOrderAsync(
        int orderId,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId <= 0)
            return ServiceResult.Fail("Usuario responsable no válido.");

        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
            return ServiceResult.Fail("Orden no encontrada.");

        if (order.Status != OrderStatus.Pendiente)
            return ServiceResult.Fail(
                order.Status == OrderStatus.EnProceso
                    ? "La orden ya está aprobada y en proceso."
                    : $"No se puede aprobar una orden en estado {order.Status}.");

        var previous = order.Status.ToString();
        order.Status = OrderStatus.EnProceso;
        _orderRepository.Update(order);

        await _changeLogs.AddRangeAsync(
        [
            new OrderChangeLog
            {
                ProductionOrderId = order.Id,
                UsuarioId = actorUserId,
                FechaUtc = DateTime.UtcNow,
                Campo = nameof(ProductionOrder.Status),
                ValorAnterior = previous,
                ValorNuevo = OrderStatus.EnProceso.ToString()
            }
        ], cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Orden {order.OrderNumber} aprobada. Ya puede iniciar producción y MES.");
    }

    // Gap #2: editar campos de Create; un OrderChangeLog por campo modificado
    public async Task<ServiceResult> UpdateOrderAsync(
        UpdateProductionOrderDto dto,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId <= 0)
            return ServiceResult.Fail("Usuario responsable no válido.");

        if (string.IsNullOrWhiteSpace(dto.ProductName) || dto.TotalQuantity <= 0)
            return ServiceResult.Fail("Producto y cantidad son obligatorios.");

        var order = await _orderRepository.GetByIdAsync(dto.OrderId, cancellationToken);
        if (order is null)
            return ServiceResult.Fail("Orden no encontrada.");

        if (order.Status is OrderStatus.Cancelada or OrderStatus.Finalizada)
            return ServiceResult.Fail("No se puede editar una orden cancelada o finalizada.");

        if (dto.TotalQuantity < order.ProducedQuantity)
            return ServiceResult.Fail(
                $"La cantidad total no puede ser menor a lo ya producido ({order.ProducedQuantity}).");

        var productName = dto.ProductName.Trim();
        var product = await _bomRepository.GetProductByNameAsync(productName, cancellationToken);
        if (product is null || product.Items.Count == 0)
            return ServiceResult.Fail("Producto no válido. Seleccione un producto con ficha técnica.");

        if (!product.HabilitadoParaOrdenes)
            return ServiceResult.Fail(
                $"El producto «{product.ProductName}» tiene ficha técnica incompleta o no habilitada para órdenes de producción.");

        var newClient = string.IsNullOrWhiteSpace(dto.ClientName) ? null : dto.ClientName.Trim();
        var changes = new List<OrderChangeLog>();
        var now = DateTime.UtcNow;

        void Track(string campo, string? anterior, string? nuevo)
        {
            if (string.Equals(anterior, nuevo, StringComparison.Ordinal))
                return;
            changes.Add(new OrderChangeLog
            {
                ProductionOrderId = order.Id,
                UsuarioId = actorUserId,
                FechaUtc = now,
                Campo = campo,
                ValorAnterior = anterior,
                ValorNuevo = nuevo
            });
        }

        Track(nameof(ProductionOrder.ProductName), order.ProductName, product.ProductName);
        Track(nameof(ProductionOrder.TotalQuantity), order.TotalQuantity.ToString(), dto.TotalQuantity.ToString());
        Track(nameof(ProductionOrder.Deadline), order.Deadline.ToString("yyyy-MM-dd"), dto.Deadline.ToString("yyyy-MM-dd"));
        Track(nameof(ProductionOrder.ClientName), order.ClientName, newClient);

        if (changes.Count == 0)
            return ServiceResult.Ok("Sin cambios.");

        var productChanged = !string.Equals(order.ProductName, product.ProductName, StringComparison.Ordinal);
        order.ProductName = product.ProductName;
        order.TotalQuantity = dto.TotalQuantity;
        order.Deadline = dto.Deadline;
        order.ClientName = newClient;
        _orderRepository.Update(order);

        if (productChanged)
        {
            var existing = await _snapshotRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            if (existing.Count > 0)
                _snapshotRepository.RemoveRange(existing);

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
        }

        await _changeLogs.AddRangeAsync(changes, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Orden {order.OrderNumber} actualizada ({changes.Count} campo(s)).");
    }

    // Gap #2: soft-cancel. No revierte stock ni genera StockMovement.
    public async Task<ServiceResult> CancelOrderAsync(
        int orderId,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId <= 0)
            return ServiceResult.Fail("Usuario responsable no válido.");

        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
            return ServiceResult.Fail("Orden no encontrada.");

        if (order.Status == OrderStatus.Cancelada)
            return ServiceResult.Fail("La orden ya está cancelada.");

        if (order.Status == OrderStatus.Finalizada)
            return ServiceResult.Fail("No se puede cancelar una orden finalizada.");

        var previous = order.Status.ToString();
        order.Status = OrderStatus.Cancelada;
        _orderRepository.Update(order);

        await _changeLogs.AddRangeAsync(
        [
            new OrderChangeLog
            {
                ProductionOrderId = order.Id,
                UsuarioId = actorUserId,
                FechaUtc = DateTime.UtcNow,
                Campo = nameof(ProductionOrder.Status),
                ValorAnterior = previous,
                ValorNuevo = OrderStatus.Cancelada.ToString()
            }
        ], cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Orden {order.OrderNumber} cancelada. El stock entregado no se revierte automáticamente.");
    }

    public async Task<IReadOnlyList<OrderChangeLogDto>> GetChangeLogAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _changeLogs.GetByOrderIdAsync(orderId, cancellationToken);
        return rows.Select(c => new OrderChangeLogDto(
            c.Id,
            c.FechaUtc,
            c.Usuario?.Nombre ?? $"#{c.UsuarioId}",
            c.UsuarioId,
            c.Campo,
            c.ValorAnterior,
            c.ValorNuevo)).ToList();
    }

    public async Task<ServiceResult> RegisterProductionAsync(int orderId, int units, CancellationToken cancellationToken = default)
    {
        if (units <= 0) return ServiceResult.Fail("Cantidad inválida.");

        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
            return ServiceResult.Fail("Orden no encontrada.");
        if (order.Status == OrderStatus.Pendiente)
            return ServiceResult.Fail("La orden está pendiente de aprobación del Administrador.");
        if (order.Status is OrderStatus.Finalizada or OrderStatus.Cancelada)
            return ServiceResult.Fail("Orden finalizada o cancelada.");

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

        await _flowService.LogProductionRegisteredAsync(orderId, toAdd, null, cancellationToken);
        return ServiceResult.Ok($"Se registraron {toAdd} unidades.");
    }

    // Órdenes donde el instructor es responsable: etapa MES asignada o ficha ligada (BelongsToInstructor)
    private async Task<HashSet<int>> GetAssignedOrderIdsAsync(
        int instructorUserId,
        string? instructorName,
        CancellationToken cancellationToken)
    {
        var ids = new HashSet<int>();

        var allOrders = await _orderRepository.GetAllAsync(cancellationToken);
        foreach (var order in allOrders)
        {
            var stages = await _flowRepository.GetStagesByOrderAsync(order.Id, cancellationToken);
            if (stages.Any(s => s.InstructorUserId == instructorUserId))
                ids.Add(order.Id);
        }

        var fichas = await _fichaRepository.GetAllAsync(cancellationToken);
        foreach (var ficha in fichas)
        {
            if (ficha.ProductionOrderId is not int orderId || orderId <= 0)
                continue;
            if (BelongsToInstructor(ficha, instructorUserId, instructorName))
                ids.Add(orderId);
        }

        return ids;
    }

    private static bool IsInstructorViewer(string? viewerRole, int? viewerUserId) =>
        viewerUserId is > 0
        && string.Equals(viewerRole, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase);

    // Misma regla que FichaService / SolicitudMaterialService
    private static bool BelongsToInstructor(Ficha ficha, int instructorUserId, string? instructorName)
    {
        if (ficha.Instructors.Any(i => i.UserId == instructorUserId))
            return true;

        if (ficha.InstructorUserId == instructorUserId)
            return true;

        return ficha.InstructorUserId is null
               && ficha.Instructors.Count == 0
               && !string.IsNullOrWhiteSpace(instructorName)
               && string.Equals(ficha.InstructorName, instructorName, StringComparison.OrdinalIgnoreCase);
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
