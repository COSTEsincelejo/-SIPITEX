using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

public class ActaMovimientoService : IActaMovimientoService
{
    private readonly IActaMovimientoRepository _actas;
    private readonly IStockMovementRepository _stock;
    private readonly IConsumoMaterialRepository _consumos;
    private readonly IProductionOrderRepository _orders;
    private readonly IUserRepository _users;
    private readonly IActaPdfService _pdf;
    private readonly IUnitOfWork _uow;

    public ActaMovimientoService(
        IActaMovimientoRepository actas,
        IStockMovementRepository stock,
        IConsumoMaterialRepository consumos,
        IProductionOrderRepository orders,
        IUserRepository users,
        IActaPdfService pdf,
        IUnitOfWork uow)
    {
        _actas = actas;
        _stock = stock;
        _consumos = consumos;
        _orders = orders;
        _users = users;
        _pdf = pdf;
        _uow = uow;
    }

    public async Task<IReadOnlyList<ActaMovimientoDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _actas.GetAllAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<ActaMovimientoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var acta = await _actas.GetByIdAsync(id, cancellationToken);
        return acta is null ? null : Map(acta);
    }

    public async Task<ServiceResult<ActaMovimientoDto>> CreateAsync(
        CreateActaDto dto,
        CancellationToken cancellationToken = default)
    {
        var entrega = (dto.EntregaNombre ?? string.Empty).Trim();
        var recibe = (dto.RecibeNombre ?? string.Empty).Trim();
        if (entrega.Length == 0 || recibe.Length == 0)
            return ServiceResult<ActaMovimientoDto>.Fail("Nombre de quien entrega y de quien recibe son obligatorios.");

        var creador = await _users.GetByIdAsync(dto.CreadoPorUserId, cancellationToken);
        if (creador is null)
            return ServiceResult<ActaMovimientoDto>.Fail("Usuario responsable no encontrado.");

        var detalles = await BuildDetallesAsync(dto, cancellationToken);
        if (detalles.Count == 0)
            return ServiceResult<ActaMovimientoDto>.Fail("El acta debe incluir al menos un ítem.");

        ProductionOrder? order = null;
        if (dto.ProductionOrderId is int oid)
        {
            order = await _orders.GetByIdAsync(oid, cancellationToken);
            if (order is null)
                return ServiceResult<ActaMovimientoDto>.Fail("Orden de producción no encontrada.");
        }

        var now = DateTime.UtcNow;
        var last = await _actas.GetLastNumeroAsync(cancellationToken);
        var acta = new ActaMovimiento
        {
            Numero = CodigoGeneradorService.SiguienteCodigo("ACT-", last),
            Tipo = dto.Tipo,
            Origen = dto.Origen,
            FechaUtc = now,
            Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim(),
            ProductionOrderId = order?.Id,
            EstadoProductoOrigen = dto.EstadoOrigen,
            EstadoProductoDestino = dto.EstadoDestino,
            EntregaNombre = Trunc(entrega, 120),
            EntregaCargo = Trunc((dto.EntregaCargo ?? string.Empty).Trim(), 80),
            EntregaConformidadUtc = dto.EntregaConforme ? now : null,
            RecibeNombre = Trunc(recibe, 120),
            RecibeCargo = Trunc((dto.RecibeCargo ?? string.Empty).Trim(), 80),
            RecibeConformidadUtc = dto.RecibeConforme ? now : null,
            CreadoPorUserId = creador.Id,
            Detalles = detalles
        };

        await _actas.AddAsync(acta, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        var saved = await _actas.GetByIdAsync(acta.Id, cancellationToken) ?? acta;
        return ServiceResult<ActaMovimientoDto>.Ok(Map(saved), $"Acta {acta.Numero} registrada.");
    }

    public async Task<ServiceResult<ReportFileDto>> ExportPdfAsync(int id, CancellationToken cancellationToken = default)
    {
        var acta = await GetByIdAsync(id, cancellationToken);
        if (acta is null)
            return ServiceResult<ReportFileDto>.Fail("Acta no encontrada.");
        return ServiceResult<ReportFileDto>.Ok(_pdf.Render(acta));
    }

    private async Task<List<ActaMovimientoDetalle>> BuildDetallesAsync(
        CreateActaDto dto,
        CancellationToken cancellationToken)
    {
        return dto.Origen switch
        {
            ActaOrigen.Stock => await FromStockAsync(dto, cancellationToken),
            ActaOrigen.Consumo => await FromConsumosAsync(dto, cancellationToken),
            ActaOrigen.EstadoProducto => FromEstado(dto),
            _ => []
        };
    }

    private async Task<List<ActaMovimientoDetalle>> FromStockAsync(
        CreateActaDto dto,
        CancellationToken cancellationToken)
    {
        var ids = dto.StockMovementIds?.Where(id => id > 0).Distinct().ToList() ?? [];
        if (ids.Count == 0)
            return [];

        var movimientos = await _stock.GetByIdsAsync(ids, cancellationToken);
        return movimientos.Select(m => new ActaMovimientoDetalle
        {
            ItemTipo = ActaItemTipo.Material,
            MaterialId = m.MaterialId,
            StockMovementId = m.Id,
            Descripcion = $"{m.TipoMovimiento} · {(m.Material?.Name ?? $"Material #{m.MaterialId}")}",
            Cantidad = m.Cantidad,
            Unidad = m.Material is null ? null : UnitHelper.ToDisplay(m.Material.Unit)
        }).ToList();
    }

    private async Task<List<ActaMovimientoDetalle>> FromConsumosAsync(
        CreateActaDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.ProductionOrderId is not int orderId)
            return [];

        var consumos = await _consumos.GetByOrderIdAsync(orderId, cancellationToken);
        var filtro = dto.ConsumoIds?.Where(id => id > 0).ToHashSet() ?? [];
        if (filtro.Count > 0)
            consumos = consumos.Where(c => filtro.Contains(c.Id)).ToList();

        return consumos.Select(c => new ActaMovimientoDetalle
        {
            ItemTipo = ActaItemTipo.Material,
            MaterialId = c.MaterialId,
            ConsumoMaterialId = c.Id,
            ProductionOrderId = c.ProductionOrderId,
            Descripcion = c.Material?.Name ?? $"Material #{c.MaterialId}",
            Cantidad = c.Cantidad,
            Unidad = c.Material is null ? null : UnitHelper.ToDisplay(c.Material.Unit)
        }).ToList();
    }

    private static List<ActaMovimientoDetalle> FromEstado(CreateActaDto dto)
    {
        if (dto.ProductionOrderId is not int orderId || dto.EstadoDestino is not EstadoProducto destino)
            return [];

        var itemTipo = destino is EstadoProducto.ProductoTerminado or EstadoProducto.VentaEntrega
            ? ActaItemTipo.ProductoTerminado
            : ActaItemTipo.ProductoEnProceso;

        var from = dto.EstadoOrigen?.ToString() ?? "?";
        return
        [
            new ActaMovimientoDetalle
            {
                ItemTipo = itemTipo,
                ProductionOrderId = orderId,
                Descripcion = $"Transición de estado {from} → {destino}",
                Cantidad = 1,
                Unidad = "unidad"
            }
        ];
    }

    private static ActaMovimientoDto Map(ActaMovimiento a) => new(
        a.Id,
        a.Numero,
        a.Tipo,
        a.Origen,
        a.FechaUtc,
        a.Observaciones,
        a.ProductionOrderId,
        a.ProductionOrder?.OrderNumber,
        a.EstadoProductoOrigen,
        a.EstadoProductoDestino,
        a.EntregaNombre,
        a.EntregaCargo,
        a.EntregaConformidadUtc,
        a.RecibeNombre,
        a.RecibeCargo,
        a.RecibeConformidadUtc,
        a.CreadoPorUserId,
        a.CreadoPor?.Nombre ?? $"#{a.CreadoPorUserId}",
        a.Detalles.Select(d => new ActaDetalleDto(
            d.Id,
            d.ItemTipo,
            d.Descripcion,
            d.Cantidad,
            d.Unidad,
            d.MaterialId,
            d.ConsumoMaterialId,
            d.StockMovementId,
            d.ProductionOrderId)).ToList());

    private static string Trunc(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
