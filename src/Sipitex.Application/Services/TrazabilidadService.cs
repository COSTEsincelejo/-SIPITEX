using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

public class TrazabilidadService : ITrazabilidadService
{
    public const int MaxPorLote = 200;

    private readonly IPrendaTrazableRepository _prendas;
    private readonly IProductionOrderRepository _orders;
    private readonly IProductionOrderBomSnapshotRepository _snapshots;
    private readonly IConsumoMaterialRepository _consumos;
    private readonly IQualityRepository _quality;
    private readonly IProductionFlowRepository _flow;
    private readonly IUserRepository _users;
    private readonly IActivityLogService _activityLog;
    private readonly IUnitOfWork _uow;

    public TrazabilidadService(
        IPrendaTrazableRepository prendas,
        IProductionOrderRepository orders,
        IProductionOrderBomSnapshotRepository snapshots,
        IConsumoMaterialRepository consumos,
        IQualityRepository quality,
        IProductionFlowRepository flow,
        IUserRepository users,
        IActivityLogService activityLog,
        IUnitOfWork uow)
    {
        _prendas = prendas;
        _orders = orders;
        _snapshots = snapshots;
        _consumos = consumos;
        _quality = quality;
        _flow = flow;
        _users = users;
        _activityLog = activityLog;
        _uow = uow;
    }

    public async Task<IReadOnlyList<PrendaTrazableListDto>> SearchAsync(
        TrazabilidadViewerFilter filter,
        string? query,
        int? productionOrderId,
        CancellationToken cancellationToken = default)
    {
        if (productionOrderId is int oid && !CanSeeOrder(filter, oid))
            return [];

        var rows = await _prendas.ListAsync(productionOrderId, query, take: 200, cancellationToken);
        return rows.Where(p => CanSeeOrder(filter, p.ProductionOrderId)).Select(MapList).ToList();
    }

    public async Task<PrendaTrazableDetailDto?> GetByCodigoAsync(
        TrazabilidadViewerFilter filter,
        string codigo,
        CancellationToken cancellationToken = default)
    {
        var prenda = await _prendas.GetByCodigoAsync(codigo, cancellationToken);
        return await MapDetailAsync(filter, prenda, cancellationToken);
    }

    public async Task<PrendaTrazableDetailDto?> GetByIdAsync(
        TrazabilidadViewerFilter filter,
        int id,
        CancellationToken cancellationToken = default)
    {
        var prenda = await _prendas.GetByIdAsync(id, cancellationToken);
        return await MapDetailAsync(filter, prenda, cancellationToken);
    }

    public async Task<ServiceResult<IReadOnlyList<PrendaTrazableListDto>>> GenerarAsync(
        TrazabilidadViewerFilter filter,
        int productionOrderId,
        int cantidad,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (cantidad < 1)
            return ServiceResult<IReadOnlyList<PrendaTrazableListDto>>.Fail("Indique cuántos códigos únicos generar.");
        if (cantidad > MaxPorLote)
            return ServiceResult<IReadOnlyList<PrendaTrazableListDto>>.Fail($"El lote máximo es {MaxPorLote} códigos.");
        if (!CanSeeOrder(filter, productionOrderId))
            return ServiceResult<IReadOnlyList<PrendaTrazableListDto>>.Fail("No tiene alcance sobre esa orden.");

        var order = await _orders.GetByIdAsync(productionOrderId, cancellationToken);
        if (order is null)
            return ServiceResult<IReadOnlyList<PrendaTrazableListDto>>.Fail("Orden de producción no encontrada.");

        var creador = await _users.GetByIdAsync(actorUserId, cancellationToken);
        if (creador is null)
            return ServiceResult<IReadOnlyList<PrendaTrazableListDto>>.Fail("Usuario responsable no encontrado.");

        var existentes = await _prendas.CountByOrderAsync(order.Id, cancellationToken);
        var cupo = Math.Max(order.TotalQuantity, order.ProducedQuantity);
        if (cupo <= 0)
            cupo = order.TotalQuantity;
        var restantes = Math.Max(0, cupo - existentes);
        if (restantes <= 0)
            return ServiceResult<IReadOnlyList<PrendaTrazableListDto>>.Fail(
                $"La orden {order.OrderNumber} ya tiene {existentes} código(s) de trazabilidad.");

        var toCreate = Math.Min(cantidad, restantes);
        var prefix = PrefixFor(order.OrderNumber);
        var last = await _prendas.GetLastCodigoForPrefixAsync(prefix, cancellationToken);
        var now = DateTime.UtcNow;
        var created = new List<PrendaTrazable>(toCreate);
        var nextCodigo = last;

        for (var i = 0; i < toCreate; i++)
        {
            nextCodigo = CodigoGeneradorService.SiguienteCodigo(prefix, nextCodigo);
            created.Add(new PrendaTrazable
            {
                Codigo = nextCodigo,
                ProductionOrderId = order.Id,
                ProductName = order.ProductName,
                Estado = order.EstadoProducto,
                CreadoUtc = now,
                CreadoPorUserId = creador.Id,
                ProductionOrder = order,
                CreadoPor = creador
            });
        }

        await _prendas.AddRangeAsync(created, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        if (actorUserId > 0)
        {
            await _activityLog.LogAsync(
                actorUserId,
                ActivityLogActions.GenerateProductCodes,
                ActivityLogEntities.PrendaTrazable,
                entityId: order.Id.ToString(),
                details: $"{toCreate} código(s) para {order.OrderNumber}",
                cancellationToken);
        }

        return ServiceResult<IReadOnlyList<PrendaTrazableListDto>>.Ok(
            created.Select(MapList).ToList(),
            $"Se generaron {toCreate} código(s) único(s) para {order.OrderNumber}.");
    }

    public static string PrefixFor(string orderNumber)
    {
        var cleaned = new string((orderNumber ?? string.Empty)
            .Where(ch => char.IsLetterOrDigit(ch) || ch == '-')
            .ToArray());
        if (string.IsNullOrWhiteSpace(cleaned))
            cleaned = "OP";
        return $"SIP-{cleaned}-";
    }

    private async Task<PrendaTrazableDetailDto?> MapDetailAsync(
        TrazabilidadViewerFilter filter,
        PrendaTrazable? prenda,
        CancellationToken cancellationToken)
    {
        if (prenda is null || !CanSeeOrder(filter, prenda.ProductionOrderId))
            return null;

        var order = prenda.ProductionOrder ?? await _orders.GetByIdAsync(prenda.ProductionOrderId, cancellationToken);
        var snapshots = await _snapshots.GetByOrderIdAsync(prenda.ProductionOrderId, cancellationToken);
        var consumos = await _consumos.GetByOrderIdAsync(prenda.ProductionOrderId, cancellationToken);
        var quality = await _quality.GetAllAsync(cancellationToken);
        var history = await _flow.GetHistoryByOrderAsync(prenda.ProductionOrderId, cancellationToken);

        var eventos = new List<PrendaTrazableEventDto>
        {
            new(prenda.CreadoUtc, "Alta de código", $"Código {prenda.Codigo} asignado a {prenda.ProductName}.")
        };

        eventos.AddRange(history.Select(h =>
            new PrendaTrazableEventDto(h.AtUtc, h.EventType.ToString(), h.Message)));

        foreach (var q in quality.Where(q => q.ProductionOrderId == prenda.ProductionOrderId))
        {
            var when = q.InspectionDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            eventos.Add(new PrendaTrazableEventDto(
                when,
                "Calidad",
                $"{q.Result}: {q.UnitsInspected} uds" + (string.IsNullOrWhiteSpace(q.MotivoReproceso) ? "" : $" · {q.MotivoReproceso}")));
        }

        foreach (var c in consumos)
        {
            eventos.Add(new PrendaTrazableEventDto(
                c.FechaUtc,
                "Consumo",
                $"{c.Material?.Name ?? $"Material #{c.MaterialId}"} · {c.Cantidad:0.##}"));
        }

        var materiales = snapshots
            .Select(s => $"{s.MaterialName} ({s.QuantityPerUnit:0.##} / ud)")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (materiales.Count == 0)
        {
            materiales = consumos
                .Select(c => c.Material?.Name ?? $"Material #{c.MaterialId}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return new PrendaTrazableDetailDto(
            prenda.Id,
            prenda.Codigo,
            prenda.ProductionOrderId,
            order?.OrderNumber ?? prenda.ProductionOrder?.OrderNumber ?? "—",
            prenda.ProductName,
            prenda.Talla,
            prenda.Estado,
            prenda.CreadoUtc,
            prenda.CreadoPor?.Nombre ?? $"#{prenda.CreadoPorUserId}",
            prenda.Observaciones,
            order?.ClientName,
            order?.Status ?? OrderStatus.Pendiente,
            order?.ProducedQuantity ?? 0,
            order?.TotalQuantity ?? 0,
            materiales,
            eventos.OrderBy(e => e.AtUtc).ToList());
    }

    private static PrendaTrazableListDto MapList(PrendaTrazable p) => new(
        p.Id,
        p.Codigo,
        p.ProductionOrderId,
        p.ProductionOrder?.OrderNumber ?? "—",
        p.ProductName,
        p.Talla,
        p.Estado,
        p.CreadoUtc);

    private static bool CanSeeOrder(TrazabilidadViewerFilter filter, int orderId)
    {
        if (string.Equals(filter.Role, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(filter.Role, UserRoles.EncargadoDeBodega, StringComparison.OrdinalIgnoreCase))
            return true;
        return filter.AllowedOrderIds.Contains(orderId);
    }
}
