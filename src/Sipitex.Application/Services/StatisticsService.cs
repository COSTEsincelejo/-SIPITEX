using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// KPIs del dashboard principal
public class StatisticsService : IStatisticsService
{
    private readonly IProductionOrderService _orderService;
    private readonly IMaterialRepository _materialRepository;
    private readonly IQualityRepository _qualityRepository;

    public StatisticsService(
        IProductionOrderService orderService,
        IMaterialRepository materialRepository,
        IQualityRepository qualityRepository)
    {
        _orderService = orderService;
        _materialRepository = materialRepository;
        _qualityRepository = qualityRepository;
    }

    // Junta datos de órdenes, inventario y calidad para el home.
    // Instructor: órdenes/calidad acotadas (GetOrdersAsync). Materiales: sin filtro por instructor
    // (igual que ReportService.ExportDashboardAsync con InstructorId).
    public async Task<DashboardKpiDto> GetDashboardAsync(
        int? viewerUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderService.GetOrdersAsync(
            viewerUserId, viewerRole, viewerName, cancellationToken);
        var orderIds = orders.Select(o => o.Id).ToHashSet();

        var materials = await _materialRepository.GetAllAsync(cancellationToken);
        var qualityAll = await _qualityRepository.GetAllAsync(cancellationToken);

        IEnumerable<QualityRecord> quality = qualityAll;
        if (IsInstructorViewer(viewerRole, viewerUserId))
            quality = qualityAll.Where(q => orderIds.Contains(q.ProductionOrderId));

        var totalProduced = orders.Sum(o => o.ProducedQuantity);
        var approved = quality.Where(q => q.Result == QualityResult.Aprobada).Sum(q => q.UnitsInspected);
        var inspected = quality.Sum(q => q.UnitsInspected);
        var qualityRate = inspected > 0 ? Math.Round(approved * 100m / inspected, 1) : 0;
        var activeOrders = orders.Count(o =>
            o.Status != OrderStatus.Finalizada && o.Status != OrderStatus.Cancelada);
        var lowStock = materials.Count(m => m.Stock < m.MinStock);

        var chart = orders
            .Select(o => new ChartBarDto(o.OrderNumber, o.ProducedQuantity, o.TotalQuantity))
            .ToList();

        return new DashboardKpiDto(totalProduced, qualityRate, activeOrders, lowStock, chart);
    }

    private static bool IsInstructorViewer(string? viewerRole, int? viewerUserId) =>
        viewerUserId is > 0
        && string.Equals(viewerRole, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase);
}
