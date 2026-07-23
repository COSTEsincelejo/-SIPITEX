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

public class ReportService : IReportService
{
    private readonly IMaterialRepository _materialRepository;
    private readonly IProductionOrderRepository _orderRepository;
    private readonly IQualityRepository _qualityRepository;
    private readonly IProductionSessionRepository _sessionRepository;
    private readonly IStatisticsService _statisticsService;

    static ReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public ReportService(
        IMaterialRepository materialRepository,
        IProductionOrderRepository orderRepository,
        IQualityRepository qualityRepository,
        IProductionSessionRepository sessionRepository,
        IStatisticsService statisticsService)
    {
        _materialRepository = materialRepository;
        _orderRepository = orderRepository;
        _qualityRepository = qualityRepository;
        _sessionRepository = sessionRepository;
        _statisticsService = statisticsService;
    }

    public async Task<ReportFileDto> ExportInventoryAsync(string format, CancellationToken cancellationToken = default)
    {
        var materials = await _materialRepository.GetAllAsync(cancellationToken);
        var rows = materials.Select(m =>
        {
            var level = StockLevel(m.Stock, m.MinStock);
            return new[]
            {
                m.Name,
                UnitHelper.ToDisplay(m.Unit),
                m.Stock.ToString("0.##"),
                m.MinStock.ToString("0.##"),
                level,
                m.Status.ToString(),
                m.LastEntryDate.ToString("yyyy-MM-dd")
            };
        }).ToList();

        var headers = new[] { "Material", "Unidad", "Stock", "Mínimo", "Nivel stock", "Estado físico", "Última entrada" };
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
        rows.AddRange(dash.ChartData.Select(c => new[] { "Orden", c.Label, c.Produced.ToString(), c.Target.ToString(), "" }));

        var headers = new[] { "Indicador", "Valor / Orden", "Producido", "Meta", "" };
        return Build("Dashboard", "Reporte KPI SIPITEX", headers, rows, format);
    }

    public async Task<ReportFileDto> ExportMonthlyAsync(int year, int month, string format, CancellationToken cancellationToken = default)
    {
        if (month is < 1 or > 12) month = DateTime.Today.Month;
        if (year < 2000 || year > 2100) year = DateTime.Today.Year;

        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1);
        var fromDate = DateOnly.FromDateTime(from);
        var toDate = DateOnly.FromDateTime(to);
        var periodLabel = from.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-CO"));

        var materials = await _materialRepository.GetAllAsync(cancellationToken);
        var orders = await _orderRepository.GetAllAsync(cancellationToken);
        var quality = await _qualityRepository.GetAllAsync(cancellationToken);
        var sessions = await _sessionRepository.GetInDateRangeAsync(from, to, cancellationToken);

        var depleted = materials.Where(m => m.Stock <= 0).ToList();
        var low = materials.Where(m => m.Stock > 0 && m.Stock < m.MinStock).ToList();
        var ok = materials.Where(m => m.Stock >= m.MinStock).ToList();
        var entriesThisMonth = materials.Where(m => m.LastEntryDate >= fromDate && m.LastEntryDate < toDate).ToList();
        var qualityMonth = quality.Where(q => q.InspectionDate >= fromDate && q.InspectionDate < toDate).ToList();
        var ordersDue = orders.Where(o => o.Deadline >= fromDate && o.Deadline < toDate).ToList();

        var rows = new List<string[]>
        {
            new[] { "RESUMEN", periodLabel, "", "", "", "" },
            new[] { "Total materiales", materials.Count.ToString(), "", "", "", "" },
            new[] { "Agotados", depleted.Count.ToString(), "", "", "", "" },
            new[] { "Por agotarse", low.Count.ToString(), "", "", "", "" },
            new[] { "Stock normal", ok.Count.ToString(), "", "", "", "" },
            new[] { "Entradas del mes", entriesThisMonth.Count.ToString(), "", "", "", "" },
            new[] { "Sesiones de producción", sessions.Count.ToString(), sessions.Sum(s => s.Units).ToString() + " uds", "", "", "" },
            new[] { "Inspecciones de calidad", qualityMonth.Count.ToString(), "", "", "", "" },
            new[] { "Órdenes con fecha límite en el mes", ordersDue.Count.ToString(), "", "", "", "" },
            new[] { "", "", "", "", "", "" },
            new[] { "LISTA DE PRODUCTOS / MATERIALES", "Nivel", "Stock", "Mínimo", "Estado físico", "Última entrada" }
        };

        foreach (var m in materials.OrderBy(x => StockLevel(x.Stock, x.MinStock) == "Agotado" ? 0 : StockLevel(x.Stock, x.MinStock) == "Por agotarse" ? 1 : 2).ThenBy(x => x.Name))
        {
            rows.Add(new[]
            {
                m.Name,
                StockLevel(m.Stock, m.MinStock),
                m.Stock.ToString("0.##"),
                m.MinStock.ToString("0.##"),
                m.Status.ToString(),
                m.LastEntryDate.ToString("yyyy-MM-dd")
            });
        }

        rows.Add(new[] { "", "", "", "", "", "" });
        rows.Add(new[] { "PRODUCCIÓN DEL MES", "Ficha", "Orden", "Unidades", "Fecha", "Observaciones" });
        foreach (var s in sessions)
        {
            rows.Add(new[]
            {
                s.Ficha?.FichaCode ?? "",
                s.ProductionOrder?.OrderNumber ?? "",
                s.Units.ToString(),
                s.SessionDate.ToString("yyyy-MM-dd"),
                s.Observations ?? "",
                ""
            });
        }

        if (!sessions.Any())
            rows.Add(new[] { "(Sin sesiones registradas en el período)", "", "", "", "", "" });

        rows.Add(new[] { "", "", "", "", "", "" });
        rows.Add(new[] { "CALIDAD DEL MES", "Orden", "Unidades", "Resultado", "Motivo", "Responsable" });
        foreach (var q in qualityMonth.OrderByDescending(x => x.InspectionDate))
        {
            rows.Add(new[]
            {
                q.InspectionDate.ToString("yyyy-MM-dd"),
                q.ProductionOrder.OrderNumber,
                q.UnitsInspected.ToString(),
                q.Result.ToString(),
                q.MotivoReproceso ?? "",
                q.Responsable ?? ""
            });
        }

        if (!qualityMonth.Any())
            rows.Add(new[] { "(Sin inspecciones en el período)", "", "", "", "", "" });

        var headers = new[] { "Sección / Material", "Detalle", "Valor", "Extra", "Campo", "Campo 2" };
        return Build($"Mensual_{year:0000}{month:00}", $"Reporte mensual SIPITEX · {periodLabel}", headers, rows, format);
    }

    private static string StockLevel(decimal stock, decimal minStock) =>
        stock <= 0 ? "Agotado" : stock < minStock ? "Por agotarse" : "Normal";

    private static ReportFileDto Build(string name, string title, string[] headers, IReadOnlyList<string[]> rows, string format)
    {
        var normalized = format.Equals("excel", StringComparison.OrdinalIgnoreCase) || format.Equals("xlsx", StringComparison.OrdinalIgnoreCase)
            ? "excel"
            : "pdf";

        if (normalized == "excel")
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(name.Length > 31 ? name[..31] : name);
            for (var c = 0; c < headers.Length; c++)
                ws.Cell(1, c + 1).Value = headers[c];
            ws.Row(1).Style.Font.Bold = true;
            for (var r = 0; r < rows.Count; r++)
            for (var c = 0; c < headers.Length; c++)
                ws.Cell(r + 2, c + 1).Value = c < rows[r].Length ? rows[r][c] : "";
            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return new ReportFileDto(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"SIPITEX_{name}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(36);
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
                            header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text(h).FontColor(Colors.White).FontSize(8).SemiBold();
                    });
                    foreach (var row in rows)
                    {
                        for (var i = 0; i < headers.Length; i++)
                        {
                            var cell = i < row.Length ? row[i] : "";
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(cell).FontSize(8);
                        }
                    }
                });
                page.Footer().AlignCenter().Text("CMTC · SENA · ADSO").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf();

        return new ReportFileDto(pdf, "application/pdf", $"SIPITEX_{name}_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }
}
