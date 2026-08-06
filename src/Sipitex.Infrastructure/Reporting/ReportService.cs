using ClosedXML.Excel; // Genera archivos .xlsx
using QuestPDF.Fluent; // API fluida de QuestPDF
using QuestPDF.Helpers; // Colores, márgenes...
using QuestPDF.Infrastructure; // Licencia y tipos base
using Sipitex.Application.DTOs; // ReportFileDto, ReportFilterDto
using Sipitex.Application.Helpers; // UnitHelper, ReportFilterHelper
using Sipitex.Application.Interfaces.Repositories; // Repos para sacar datos
using Sipitex.Application.Interfaces.Services; // IReportService, IStatisticsService
using Sipitex.Domain.Enums; // Enums que salen en las columnas

namespace Sipitex.Infrastructure.Reporting;

// Genera reportes en Excel o PDF según lo pida el usuario
public class ReportService : IReportService
{
    private readonly IMaterialRepository _materialRepository; // Datos de inventario
    private readonly IProductionOrderRepository _orderRepository; // Órdenes OP-xxx
    private readonly IQualityRepository _qualityRepository; // Inspecciones de calidad
    private readonly IFichaRepository _fichaRepository; // Para filtrar por instructor/ficha/jornada
    private readonly IStatisticsService _statisticsService; // KPIs del dashboard

    static ReportService()
    {
        // Licencia community de QuestPDF (gratis para proyectos chicos)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // El DI inyecta los repos y el servicio de estadísticas
    public ReportService(
        IMaterialRepository materialRepository,
        IProductionOrderRepository orderRepository,
        IQualityRepository qualityRepository,
        IFichaRepository fichaRepository,
        IStatisticsService statisticsService)
    {
        _materialRepository = materialRepository;
        _orderRepository = orderRepository;
        _qualityRepository = qualityRepository;
        _fichaRepository = fichaRepository;
        _statisticsService = statisticsService;
    }

    // Reporte de inventario: stock, mínimos, estado...
    // Filtros aplicables: fecha (LastEntryDate). Instructor/ficha/jornada se ignoran (no existen en Material).
    public async Task<ReportFileDto> ExportInventoryAsync(
        string format,
        ReportFilterDto? filter = null,
        CancellationToken cancellationToken = default)
    {
        var materials = await _materialRepository.GetAllAsync(cancellationToken);
        var filtered = materials
            .Where(m => ReportFilterHelper.MatchesDate(m.LastEntryDate, filter))
            .ToList();

        var rows = filtered.Select(m => new[]
        {
            m.Name,
            UnitHelper.ToDisplay(m.Unit),
            m.Stock.ToString("0.##"),
            m.MinStock.ToString("0.##"),
            m.Status.ToString(),
            m.LastEntryDate.ToString("yyyy-MM-dd"),
            m.Stock < m.MinStock ? "Sí" : "No"
        }).ToList();

        var headers = new[] { "Material", "Unidad", "Stock", "Mínimo", "Estado", "Última entrada", "Bajo mínimo" };
        return Build("Inventario", "Reporte de inventario SIPITEX", headers, rows, format, filter);
    }

    // Reporte de órdenes: filtros vía Fichas ligadas (instructor/ficha/jornada) + Deadline
    public async Task<ReportFileDto> ExportOrdersAsync(
        string format,
        ReportFilterDto? filter = null,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetAllAsync(cancellationToken);
        var filtered = await FilterOrdersAsync(orders, filter, cancellationToken);

        var rows = filtered.Select(o => new[]
        {
            o.OrderNumber,
            o.ProductName,
            o.TotalQuantity.ToString(),
            o.ProducedQuantity.ToString(),
            o.TotalQuantity == 0 ? "0%" : $"{o.ProducedQuantity * 100 / o.TotalQuantity}%",
            o.Status.ToString(),
            o.Deadline.ToString("yyyy-MM-dd")
        }).ToList();

        var headers = new[] { "Orden", "Producto", "Meta", "Producido", "Avance", "Estado", "Fecha límite" };
        return Build("Ordenes", "Reporte de órdenes de producción SIPITEX", headers, rows, format, filter);
    }

    // Reporte de calidad: fecha InspectionDate + alcance por fichas de la orden
    public async Task<ReportFileDto> ExportQualityAsync(
        string format,
        ReportFilterDto? filter = null,
        CancellationToken cancellationToken = default)
    {
        var records = await _qualityRepository.GetAllAsync(cancellationToken);
        var orderIds = await ResolveOrderScopeAsync(filter, cancellationToken);

        var filtered = records
            .Where(r => ReportFilterHelper.MatchesDate(r.InspectionDate, filter))
            .Where(r => !ReportFilterHelper.NeedsFichaScope(filter) || orderIds.Contains(r.ProductionOrderId))
            .OrderByDescending(r => r.InspectionDate)
            .ToList();

        var rows = filtered.Select(r => new[]
        {
            r.ProductionOrder.OrderNumber,
            r.UnitsInspected.ToString(),
            r.Result.ToString(),
            r.MotivoReproceso ?? "",
            r.Responsable ?? "",
            r.InspectionDate.ToString("yyyy-MM-dd")
        }).ToList();

        var headers = new[] { "Orden", "Unidades", "Resultado", "Motivo", "Responsable", "Fecha" };
        return Build("Calidad", "Reporte de control de calidad SIPITEX", headers, rows, format, filter);
    }

    // Dashboard: recalcula KPIs sobre los mismos conjuntos filtrados
    public async Task<ReportFileDto> ExportDashboardAsync(
        string format,
        ReportFilterDto? filter = null,
        CancellationToken cancellationToken = default)
    {
        // Sin filtros: comportamiento idéntico al actual (StatisticsService)
        if (filter is null || !filter.HasAny)
        {
            var dash = await _statisticsService.GetDashboardAsync(cancellationToken);
            var rowsFull = new List<string[]>
            {
                new[] { "Prendas producidas", dash.TotalProduced.ToString(), "", "", "" },
                new[] { "Tasa de calidad", $"{dash.QualityRate}%", "", "", "" },
                new[] { "Órdenes activas", dash.ActiveOrders.ToString(), "", "", "" },
                new[] { "Materiales bajo mínimo", dash.LowStockCount.ToString(), "", "", "" }
            };
            rowsFull.AddRange(dash.ChartData.Select(c => new[] { "Orden", c.Label, c.Produced.ToString(), c.Target.ToString(), "" }));
            var headersFull = new[] { "Indicador", "Valor / Orden", "Producido", "Meta", "" };
            return Build("Dashboard", "Reporte KPI SIPITEX", headersFull, rowsFull, format, filter);
        }

        var orders = await FilterOrdersAsync(
            await _orderRepository.GetAllAsync(cancellationToken),
            filter,
            cancellationToken);

        var qualityAll = await _qualityRepository.GetAllAsync(cancellationToken);
        var orderIds = orders.Select(o => o.Id).ToHashSet();
        var quality = qualityAll
            .Where(q => ReportFilterHelper.MatchesDate(q.InspectionDate, filter))
            .Where(q => !ReportFilterHelper.NeedsFichaScope(filter) || orderIds.Contains(q.ProductionOrderId))
            .ToList();

        // Inventario solo entiende fecha (LastEntryDate); instructor/ficha/jornada se ignoran
        var materials = (await _materialRepository.GetAllAsync(cancellationToken))
            .Where(m => ReportFilterHelper.MatchesDate(m.LastEntryDate, filter))
            .ToList();

        var totalProduced = orders.Sum(o => o.ProducedQuantity);
        var approved = quality.Where(q => q.Result == QualityResult.Aprobada).Sum(q => q.UnitsInspected);
        var inspected = quality.Sum(q => q.UnitsInspected);
        var qualityRate = inspected > 0 ? Math.Round(approved * 100m / inspected, 1) : 0;
        var activeOrders = orders.Count(o => o.Status != OrderStatus.Finalizada && o.Status != OrderStatus.Cancelada);
        var lowStock = materials.Count(m => m.Stock < m.MinStock);

        var rows = new List<string[]>
        {
            new[] { "Prendas producidas", totalProduced.ToString(), "", "", "" },
            new[] { "Tasa de calidad", $"{qualityRate}%", "", "", "" },
            new[] { "Órdenes activas", activeOrders.ToString(), "", "", "" },
            new[] { "Materiales bajo mínimo", lowStock.ToString(), "", "", "" }
        };
        rows.AddRange(orders.Select(o => new[]
        {
            "Orden",
            o.OrderNumber,
            o.ProducedQuantity.ToString(),
            o.TotalQuantity.ToString(),
            ""
        }));

        var headers = new[] { "Indicador", "Valor / Orden", "Producido", "Meta", "" };
        return Build("Dashboard", "Reporte KPI SIPITEX", headers, rows, format, filter);
    }

    private async Task<IReadOnlyList<Domain.Entities.ProductionOrder>> FilterOrdersAsync(
        IReadOnlyList<Domain.Entities.ProductionOrder> orders,
        ReportFilterDto? filter,
        CancellationToken cancellationToken)
    {
        IEnumerable<Domain.Entities.ProductionOrder> query = orders;

        if (ReportFilterHelper.NeedsFichaScope(filter))
        {
            var orderIds = await ResolveOrderScopeAsync(filter, cancellationToken);
            query = query.Where(o => orderIds.Contains(o.Id));
        }

        query = query.Where(o => ReportFilterHelper.MatchesDate(o.Deadline, filter));
        return query.ToList();
    }

    private async Task<HashSet<int>> ResolveOrderScopeAsync(
        ReportFilterDto? filter,
        CancellationToken cancellationToken)
    {
        if (!ReportFilterHelper.NeedsFichaScope(filter))
            return [];

        var fichas = await _fichaRepository.GetAllAsync(cancellationToken);
        return ReportFilterHelper.MatchingOrderIds(fichas, filter);
    }

    // Arma el archivo final — excel con ClosedXML o pdf con QuestPDF
    private static ReportFileDto Build(
        string name,
        string title,
        string[] headers,
        IReadOnlyList<string[]> rows,
        string format,
        ReportFilterDto? filter = null)
    {
        var filterNote = filter is { HasAny: true }
            ? $"Filtros: {filter.ToSummaryLabel()}"
            : null;

        var normalized = format.Equals("excel", StringComparison.OrdinalIgnoreCase) || format.Equals("xlsx", StringComparison.OrdinalIgnoreCase)
            ? "excel"
            : "pdf";

        if (normalized == "excel")
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(name);
            var startRow = 1;
            if (!string.IsNullOrWhiteSpace(filterNote))
            {
                ws.Cell(1, 1).Value = filterNote;
                ws.Range(1, 1, 1, headers.Length).Merge();
                ws.Row(1).Style.Font.Italic = true;
                startRow = 2;
            }
            for (var c = 0; c < headers.Length; c++)
                ws.Cell(startRow, c + 1).Value = headers[c];
            ws.Row(startRow).Style.Font.Bold = true;
            for (var r = 0; r < rows.Count; r++)
            for (var c = 0; c < headers.Length; c++)
                ws.Cell(r + startRow + 1, c + 1).Value = rows[r][c];
            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return new ReportFileDto(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"SIPITEX_{name}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Column(col =>
                {
                    col.Item().Text("SIPITEX").SemiBold().FontSize(18).FontColor(Colors.Blue.Darken2);
                    col.Item().Text(title).FontSize(12).FontColor(Colors.Grey.Darken2);
                    col.Item().Text($"Generado: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
                    if (!string.IsNullOrWhiteSpace(filterNote))
                        col.Item().Text(filterNote).FontSize(9).FontColor(Colors.Grey.Darken1);
                });
                page.Content().PaddingVertical(16).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        foreach (var _ in headers)
                            cols.RelativeColumn();
                    });
                    table.Header(header =>
                    {
                        foreach (var h in headers)
                            header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text(h).FontColor(Colors.White).FontSize(9).SemiBold();
                    });
                    foreach (var row in rows)
                    {
                        foreach (var cell in row)
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(cell).FontSize(9);
                    }
                });
                page.Footer().AlignCenter().Text("CMTC · SENA · ADSO").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf();

        return new ReportFileDto(pdf, "application/pdf", $"SIPITEX_{name}_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }
}
