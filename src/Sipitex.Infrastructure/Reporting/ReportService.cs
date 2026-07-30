using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Enums;

namespace Sipitex.Infrastructure.Reporting;

// Genera reportes en Excel o PDF según lo pida el usuario
public class ReportService : IReportService
{
    private readonly IMaterialRepository _materialRepository;
    private readonly IProductionOrderRepository _orderRepository;
    private readonly IQualityRepository _qualityRepository;
    private readonly IStatisticsService _statisticsService;

    static ReportService()
    {
        // Licencia community de QuestPDF (gratis para proyectos chicos)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public ReportService(
        IMaterialRepository materialRepository,
        IProductionOrderRepository orderRepository,
        IQualityRepository qualityRepository,
        IStatisticsService statisticsService)
    {
        _materialRepository = materialRepository;
        _orderRepository = orderRepository;
        _qualityRepository = qualityRepository;
        _statisticsService = statisticsService;
    }

    public async Task<ReportFileDto> ExportInventoryAsync(string format, CancellationToken cancellationToken = default)
    {
        var materials = await _materialRepository.GetAllAsync(cancellationToken);
        var rows = materials.Select(m => new[]
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
        return Build("Inventario", "Reporte de inventario SIPITEX", headers, rows, format);
    }

    public async Task<ReportFileDto> ExportOrdersAsync(string format, CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetAllAsync(cancellationToken);
        var rows = orders.Select(o => new[]
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
        return Build("Ordenes", "Reporte de órdenes de producción SIPITEX", headers, rows, format);
    }

    public async Task<ReportFileDto> ExportQualityAsync(string format, CancellationToken cancellationToken = default)
    {
        var records = await _qualityRepository.GetAllAsync(cancellationToken);
        var rows = records
            .OrderByDescending(r => r.InspectionDate)
            .Select(r => new[]
            {
                r.ProductionOrder.OrderNumber,
                r.UnitsInspected.ToString(),
                r.Result.ToString(),
                r.MotivoReproceso ?? "",
                r.Responsable ?? "",
                r.InspectionDate.ToString("yyyy-MM-dd")
            }).ToList();

        var headers = new[] { "Orden", "Unidades", "Resultado", "Motivo", "Responsable", "Fecha" };
        return Build("Calidad", "Reporte de control de calidad SIPITEX", headers, rows, format);
    }

    public async Task<ReportFileDto> ExportDashboardAsync(string format, CancellationToken cancellationToken = default)
    {
        var dash = await _statisticsService.GetDashboardAsync(cancellationToken);
        var rows = new List<string[]>
        {
            new[] { "Prendas producidas", dash.TotalProduced.ToString(), "", "", "" },
            new[] { "Tasa de calidad", $"{dash.QualityRate}%", "", "", "" },
            new[] { "Órdenes activas", dash.ActiveOrders.ToString(), "", "", "" },
            new[] { "Materiales bajo mínimo", dash.LowStockCount.ToString(), "", "", "" }
        };
        // Abajo van los datos del gráfico de avance por orden
        rows.AddRange(dash.ChartData.Select(c => new[] { "Orden", c.Label, c.Produced.ToString(), c.Target.ToString(), "" }));

        var headers = new[] { "Indicador", "Valor / Orden", "Producido", "Meta", "" };
        return Build("Dashboard", "Reporte KPI SIPITEX", headers, rows, format);
    }

    // Arma el archivo final — excel con ClosedXML o pdf con QuestPDF
    private static ReportFileDto Build(string name, string title, string[] headers, IReadOnlyList<string[]> rows, string format)
    {
        var normalized = format.Equals("excel", StringComparison.OrdinalIgnoreCase) || format.Equals("xlsx", StringComparison.OrdinalIgnoreCase)
            ? "excel"
            : "pdf";

        if (normalized == "excel")
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(name);
            for (var c = 0; c < headers.Length; c++)
                ws.Cell(1, c + 1).Value = headers[c];
            ws.Row(1).Style.Font.Bold = true;
            for (var r = 0; r < rows.Count; r++)
            for (var c = 0; c < headers.Length; c++)
                ws.Cell(r + 2, c + 1).Value = rows[r][c];
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
