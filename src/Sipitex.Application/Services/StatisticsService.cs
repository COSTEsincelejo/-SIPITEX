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
        // Query de órdenes de producción
        var orders = await _orderRepository.GetAllAsync(cancellationToken);
        // Query de materiales para stock bajo
        var materials = await _materialRepository.GetAllAsync(cancellationToken);
        // Query de inspecciones de calidad
        var quality = await _qualityRepository.GetAllAsync(cancellationToken);

        // Total de unidades producidas en todas las órdenes
        var totalProduced = orders.Sum(o => o.ProducedQuantity);
        // Unidades aprobadas en inspección
        var approved = quality.Where(q => q.Result == QualityResult.Aprobada).Sum(q => q.UnitsInspected);
        // Todas las unidades que pasaron por inspección
        var inspected = quality.Sum(q => q.UnitsInspected);
        // Tasa de calidad en porcentaje (0 si no hubo inspecciones)
        var qualityRate = inspected > 0 ? Math.Round(approved * 100m / inspected, 1) : 0;
        // Órdenes que siguen activas (no finalizadas ni canceladas)
        var activeOrders = orders.Count(o => o.Status != OrderStatus.Finalizada && o.Status != OrderStatus.Cancelada);
        // Materiales bajo mínimo
        var lowStock = materials.Count(m => m.Stock < m.MinStock);

        // Datos para el gráfico de barras (producido vs meta por orden)
        var chart = orders.Select(o => new ChartBarDto(o.OrderNumber, o.ProducedQuantity, o.TotalQuantity)).ToList();

        // Empaqueto todo en un solo DTO para la vista del dashboard
        return new DashboardKpiDto(totalProduced, qualityRate, activeOrders, lowStock, chart);
    }
}
