using Microsoft.Extensions.Logging;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Application.Services;

// Costo de prenda = Σ (cantidad consumida × CostoUnitarioAlMomento)
// + (horas de GrupoConfeccion × tarifa configurable). El snapshot no se recalcula.
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
            return new GarmentCostDto(productionOrderId, string.Empty, 0, 0, tarifa, 0, 0, Formula(), tarifaOk);

        var consumos = await _consumos.GetByOrderIdAsync(productionOrderId, cancellationToken);
        decimal materiales = 0;
        foreach (var consumo in consumos)
            materiales += consumo.Cantidad * consumo.CostoUnitarioAlMomento;

        var grupos = await _grupos.GetByOrderIdAsync(productionOrderId, cancellationToken);
        var horas = grupos.Sum(Horas);
        var manoObra = decimal.Round(horas * tarifa, 4, MidpointRounding.AwayFromZero);
        var total = decimal.Round(materiales + manoObra, 4, MidpointRounding.AwayFromZero);

        var dto = new GarmentCostDto(
            order.Id,
            order.OrderNumber,
            decimal.Round(materiales, 4, MidpointRounding.AwayFromZero),
            decimal.Round(horas, 4, MidpointRounding.AwayFromZero),
            tarifa,
            manoObra,
            total,
            Formula(),
            tarifaOk);

        if (!tarifaOk)
        {
            _logger.LogWarning(
                "Costeo de orden {OrderId}: tarifa de mano de obra no configurada; costo de mano de obra no incluido",
                productionOrderId);
        }
        else
        {
            _logger.LogInformation(
                "Costeo de orden {OrderId} ({OrderNumber}): materiales={Materiales}, horas={Horas}, tarifa={Tarifa}, total={Total}",
                order.Id, order.OrderNumber, dto.CostoMateriales, dto.HorasManoObra, dto.TarifaHora, dto.Total);
        }

        return dto;
    }

    public static string Formula() =>
        "costo materiales (Σ cantidad consumida × costo unitario al momento) + (horas de grupo de confección × Costing:LaborHourRate)";

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
