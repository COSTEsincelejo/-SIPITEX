using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using System.Globalization;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

public class MaterialConsumptionService : IMaterialConsumptionService
{
    // La entrega de la orden (OrderMaterialService, línea ProductionOrderMaterialRequirement)
    // es la única que resta Material.Stock y escribe la salida. Este servicio anota el uso
    // y lo enlaza a esa línea. No crea otro StockMovement.
    // EntregaMaterial pertenece a SolicitudMaterial y no cubre este consumo.
    public const string MensajeSuperaEntrega =
        "No se puede consumir más de lo que fue entregado para esta orden.";

    public const string MensajeSinEntrega =
        "Este material no tiene una entrega registrada para esta orden. Entregue el material en Materiales de órdenes antes de registrar el consumo.";

    public const string MensajeOrdenCerrada =
        "No se puede registrar consumo en una orden finalizada o cancelada.";

    private readonly IConsumoMaterialRepository _consumos;
    private readonly IProductionOrderRepository _orders;
    private readonly IMaterialRepository _materials;
    private readonly IUserRepository _users;
    private readonly IOrderMaterialRequirementRepository _requirements;
    private readonly IMaterialConsumptionCostService _costService;
    private readonly IUnitOfWork _unitOfWork;

    public MaterialConsumptionService(
        IConsumoMaterialRepository consumos,
        IProductionOrderRepository orders,
        IMaterialRepository materials,
        IUserRepository users,
        IOrderMaterialRequirementRepository requirements,
        IMaterialConsumptionCostService costService,
        IUnitOfWork unitOfWork)
    {
        _consumos = consumos;
        _orders = orders;
        _materials = materials;
        _users = users;
        _requirements = requirements;
        _costService = costService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ConsumoMaterialDto>> GetByOrderAsync(
        int productionOrderId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _consumos.GetByOrderIdAsync(productionOrderId, cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<ServiceResult> RegisterAsync(
        RegisterConsumoMaterialDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.Cantidad <= 0)
            return ServiceResult.Fail("La cantidad utilizada debe ser mayor que cero.");
        if (dto.ResponsableUserId <= 0)
            return ServiceResult.Fail("Indique el responsable del consumo.");

        var order = await _orders.GetByIdAsync(dto.ProductionOrderId, cancellationToken);
        if (order is null)
            return ServiceResult.Fail("Orden de producción no encontrada.");
        if (order.Status is OrderStatus.Finalizada or OrderStatus.Cancelada)
            return ServiceResult.Fail(MensajeOrdenCerrada);

        var material = await _materials.GetByIdAsync(dto.MaterialId, cancellationToken);
        if (material is null)
            return ServiceResult.Fail("Material no encontrado.");

        var responsable = await _users.GetByIdAsync(dto.ResponsableUserId, cancellationToken);
        if (responsable is null)
            return ServiceResult.Fail("Responsable no encontrado.");

        var lineas = (await _requirements.GetByOrderIdAsync(order.Id, cancellationToken))
            .Where(l => l.MaterialId == material.Id)
            .OrderBy(l => l.Id)
            .ToList();
        var previos = (await _consumos.GetByOrderIdAsync(order.Id, cancellationToken))
            .Where(c => c.MaterialId == material.Id)
            .ToList();
        var totales = DisponibilidadMaterialOrden.DelMaterial(lineas, previos);
        if (totales.Entregado <= 0)
            return ServiceResult.Fail(MensajeSinEntrega);
        if (dto.Cantidad > totales.Disponible)
            return ServiceResult.Fail(
                $"{MensajeSuperaEntrega} Entregado: {Numero(totales.Entregado)}, reingresado: {Numero(totales.Reingresado)}, ya consumido: {Numero(totales.Consumido)}.");

        var costo = material.CostoPromedioPonderado;
        var fecha = dto.FechaUtc ?? DateTime.UtcNow;
        var partes = new List<(int LineaId, decimal Cantidad)>();
        var porColocar = dto.Cantidad;
        foreach (var linea in lineas)
        {
            if (porColocar <= 0)
                break;

            var usadoEnLinea = DisponibilidadMaterialOrden.ConsumidoEnLinea(previos, linea.Id);
            var restante = DisponibilidadMaterialOrden.DeLaLinea(linea, usadoEnLinea);
            if (restante <= 0)
                continue;

            var toma = Math.Min(restante, porColocar);
            partes.Add((linea.Id, toma));
            porColocar -= toma;
        }

        if (porColocar > 0)
            return ServiceResult.Fail(MensajeSuperaEntrega);

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            foreach (var parte in partes)
            {
                await _consumos.AddAsync(new ConsumoMaterial
                {
                    ProductionOrderId = order.Id,
                    GrupoConfeccionId = dto.GrupoConfeccionId,
                    MaterialId = material.Id,
                    OrderMaterialRequirementId = parte.LineaId,
                    Cantidad = parte.Cantidad,
                    FechaUtc = fecha,
                    ResponsableUserId = responsable.Id,
                    CostoUnitarioAlMomento = costo
                }, ct);
            }
        }, cancellationToken);

        return ServiceResult.Ok(
            $"Consumo de {dto.Cantidad} {UnitHelper.ToDisplay(material.Unit)} de «{material.Name}» registrado en {order.OrderNumber}. El inventario no se descuenta de nuevo: ya salió en la entrega.");
    }

    public async Task<ConsumoCostoResumenDto> GetCostoPromedioByOrderAsync(
        int productionOrderId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _consumos.GetByOrderIdAsync(productionOrderId, cancellationToken);
        var lineas = rows.Select(r => new ConsumoCostoLineaDto(r.Cantidad, r.CostoUnitarioAlMomento)).ToList();
        var promedio = _costService.CalcularPromedioPonderado(lineas);
        var totalCantidad = lineas.Sum(l => l.Cantidad);
        var totalCosto = lineas.Sum(l => l.Cantidad * l.CostoUnitario);
        return new ConsumoCostoResumenDto(productionOrderId, totalCantidad, promedio, totalCosto);
    }

    private static string Numero(decimal valor) => valor.ToString("0.##", CultureInfo.InvariantCulture);

    private static ConsumoMaterialDto Map(ConsumoMaterial c) => new(
        c.Id,
        c.ProductionOrderId,
        c.ProductionOrder?.OrderNumber ?? string.Empty,
        c.GrupoConfeccionId,
        c.MaterialId,
        c.Material?.Code ?? string.Empty,
        c.Material?.Name ?? string.Empty,
        c.Cantidad,
        c.Material is null ? "—" : UnitHelper.ToDisplay(c.Material.Unit),
        c.FechaUtc,
        c.ResponsableUserId,
        c.Responsable?.Nombre ?? $"#{c.ResponsableUserId}",
        c.CostoUnitarioAlMomento,
        c.Cantidad * c.CostoUnitarioAlMomento,
        c.Material?.CodigoAlmacen,
        c.Material?.Referencia,
        c.Material?.Categoria,
        c.Material?.TipoElemento);
}
