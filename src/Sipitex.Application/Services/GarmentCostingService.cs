using Microsoft.Extensions.Logging;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Application.Services;

// Costo de prenda = solo materiales congelados al consumo.
// Por unidad = Σ (cantidad × CostoUnitarioAlMomento) / volumen de la orden.
// Por orden   = costo por unidad × volumen.
// Mano de obra y overhead quedan fuera del total.
public class GarmentCostingService : IGarmentCostingService
{
    private readonly IConsumoMaterialRepository _consumos;
    private readonly IGrupoConfeccionRepository _grupos;
    private readonly IProductionOrderRepository _orders;
    private readonly ICostingSettingsService _costingSettings;
    private readonly ILogger<GarmentCostingService> _logger;

    public GarmentCostingService(
        IConsumoMaterialRepository consumos,
        IGrupoConfeccionRepository grupos,
        IProductionOrderRepository orders,
        ICostingSettingsService costingSettings,
        ILogger<GarmentCostingService> logger)
    {
        _consumos = consumos;
        _grupos = grupos;
        _orders = orders;
        _costingSettings = costingSettings;
        _logger = logger;
    }

    public async Task<GarmentCostDto> CalcularAsync(
        int productionOrderId,
        CancellationToken cancellationToken = default)
    {
        var tarifa = await _costingSettings.GetLaborHourRateAsync(cancellationToken);
        var tarifaOk = tarifa > 0;

        var order = await _orders.GetByIdAsync(productionOrderId, cancellationToken);
        if (order is null)
            return Empty(productionOrderId, tarifa, tarifaOk);

        var consumos = await _consumos.GetByOrderIdAsync(productionOrderId, cancellationToken);
        decimal materialesOrden = 0;
        foreach (var consumo in consumos)
            materialesOrden += consumo.Cantidad * consumo.CostoUnitarioAlMomento;
        materialesOrden = decimal.Round(materialesOrden, 4, MidpointRounding.AwayFromZero);

        var volumen = Math.Max(0, order.TotalQuantity);
        var costoPorUnidad = volumen > 0
            ? decimal.Round(materialesOrden / volumen, 4, MidpointRounding.AwayFromZero)
            : 0m;
        var costoFinal = materialesOrden;

        var grupos = await _grupos.GetByOrderIdAsync(productionOrderId, cancellationToken);
        var horas = grupos.Sum(Horas);

        var dto = new GarmentCostDto(
            order.Id,
            order.OrderNumber,
            volumen,
            costoPorUnidad,
            costoFinal,
            decimal.Round(horas, 4, MidpointRounding.AwayFromZero),
            tarifa,
            0,
            costoFinal,
            Formula(),
            tarifaOk);

        _logger.LogInformation(
            "Costeo de orden {OrderId} ({OrderNumber}): volumen={Volumen}, materiales/ud={Unitario}, total={Total} (mano de obra excluida)",
            order.Id, order.OrderNumber, dto.Volumen, dto.CostoMaterialesPorUnidad, dto.Total);

        return dto;
    }

    public static string Formula() =>
        "costo materiales por unidad (Σ cantidad consumida × costo unitario al momento / volumen) × volumen de la orden. Mano de obra no incluida.";

    private static GarmentCostDto Empty(int productionOrderId, decimal tarifa, bool tarifaOk) =>
        new(productionOrderId, string.Empty, 0, 0, 0, 0, tarifa, 0, 0, Formula(), tarifaOk);

    private static decimal Horas(Domain.Entities.GrupoConfeccion g)
    {
        if (g.HoraFin is not TimeOnly fin)
            return 0;
        var span = fin - g.HoraInicio;
        if (span < TimeSpan.Zero)
            span += TimeSpan.FromHours(24);
        return (decimal)span.TotalHours;
    }
}
