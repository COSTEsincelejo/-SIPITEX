using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// KPIs del dashboard principal
public class StatisticsService : IStatisticsService
{
    private readonly IProductionOrderRepository _orderRepository;
    private readonly IMaterialRepository _materialRepository;
    private readonly IQualityRepository _qualityRepository;

    public StatisticsService(
        IProductionOrderRepository orderRepository,
        IMaterialRepository materialRepository,
        IQualityRepository qualityRepository)
    {
        _orderRepository = orderRepository;
        _materialRepository = materialRepository;
        _qualityRepository = qualityRepository;
    }

    // Junta datos de órdenes, inventario y calidad para el home
    public async Task<DashboardKpiDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetAllAsync(cancellationToken);
        var materials = await _materialRepository.GetAllAsync(cancellationToken);
        var quality = await _qualityRepository.GetAllAsync(cancellationToken);

        var totalProduced = orders.Sum(o => o.ProducedQuantity);
        var approved = quality.Where(q => q.Result == QualityResult.Aprobada).Sum(q => q.UnitsInspected);
        var inspected = quality.Sum(q => q.UnitsInspected);
        var qualityRate = inspected > 0 ? Math.Round(approved * 100m / inspected, 1) : 0;
        var activeOrders = orders.Count(o => o.Status != OrderStatus.Finalizada && o.Status != OrderStatus.Cancelada);
        var lowStock = materials.Count(m => m.Stock < m.MinStock);

        var chart = orders.Select(o => new ChartBarDto(o.OrderNumber, o.ProducedQuantity, o.TotalQuantity)).ToList();

        return new DashboardKpiDto(totalProduced, qualityRate, activeOrders, lowStock, chart);
    }
}
