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
    private readonly IFichaRepository _fichaRepository;
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
        IFichaRepository fichaRepository,
        IStatisticsService statisticsService)
    {
        _materialRepository = materialRepository;
        _orderRepository = orderRepository;
        _qualityRepository = qualityRepository;
        _sessionRepository = sessionRepository;
        _fichaRepository = fichaRepository;
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

    public Task<ReportFileDto> ExportMonthlyAsync(int year, int month, string format, CancellationToken cancellationToken = default) =>
        ExportFilteredAsync(new ReportFilterDto("mes", Year: year, Month: month, Format: format), cancellationToken);

    public async Task<ReportFileDto> ExportFilteredAsync(ReportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var (from, to, periodLabel, fileTag) = ResolvePeriod(filter);
        var fromDate = DateOnly.FromDateTime(from);
        var toDate = DateOnly.FromDateTime(to);

        var materials = await _materialRepository.GetAllAsync(cancellationToken);
        var orders = await _orderRepository.GetAllAsync(cancellationToken);
        var quality = await _qualityRepository.GetAllAsync(cancellationToken);
        var sessions = (await _sessionRepository.GetInDateRangeAsync(from, to, cancellationToken)).ToList();
        var fichas = await _fichaRepository.GetAllAsync(cancellationToken);

        if (filter.FichaId is > 0)
            sessions = sessions.Where(s => s.FichaId == filter.FichaId.Value).ToList();

        if (!string.IsNullOrWhiteSpace(filter.Instructor))
        {
            var instructor = filter.Instructor.Trim();
            sessions = sessions
                .Where(s => string.Equals(s.Ficha?.InstructorName, instructor, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var depleted = materials.Where(m => m.Stock <= 0).ToList();
        var low = materials.Where(m => m.Stock > 0 && m.Stock < m.MinStock).ToList();
        var ok = materials.Where(m => m.Stock >= m.MinStock).ToList();
        var entriesPeriod = materials.Where(m => m.LastEntryDate >= fromDate && m.LastEntryDate < toDate).ToList();
        var qualityPeriod = quality.Where(q => q.InspectionDate >= fromDate && q.InspectionDate < toDate).ToList();
        var ordersDue = orders.Where(o => o.Deadline >= fromDate && o.Deadline < toDate).ToList();

        var filterDesc = periodLabel;
        if (!string.IsNullOrWhiteSpace(filter.Instructor))
            filterDesc += $" · Instructor: {filter.Instructor}";
        if (filter.FichaId is > 0)
        {
            var ficha = fichas.FirstOrDefault(f => f.Id == filter.FichaId.Value);
            filterDesc += $" · Ficha: {ficha?.FichaCode ?? filter.FichaId.ToString()}";
        }

        var rows = new List<string[]>
        {
            new[] { "FILTROS", filterDesc, "", "", "", "" },
            new[] { "Período", $"{from:yyyy-MM-dd} → {to.AddDays(-1):yyyy-MM-dd}", "", "", "", "" },
            new[] { "Total materiales", materials.Count.ToString(), "", "", "", "" },
            new[] { "Agotados", depleted.Count.ToString(), "", "", "", "" },
            new[] { "Por agotarse", low.Count.ToString(), "", "", "", "" },
            new[] { "Stock normal", ok.Count.ToString(), "", "", "", "" },
            new[] { "Entradas en el período", entriesPeriod.Count.ToString(), "", "", "", "" },
            new[] { "Sesiones de producción", sessions.Count.ToString(), sessions.Sum(s => s.Units).ToString() + " uds", "", "", "" },
            new[] { "Inspecciones de calidad", qualityPeriod.Count.ToString(), "", "", "", "" },
            new[] { "Órdenes con fecha límite en período", ordersDue.Count.ToString(), "", "", "", "" },
            new[] { "", "", "", "", "", "" },
            new[] { "LISTA DE MATERIALES", "Nivel", "Stock", "Mínimo", "Estado físico", "Última entrada" }
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
        rows.Add(new[] { "PRODUCCIÓN", "Ficha", "Instructor", "Orden", "Unidades", "Fecha" });
        foreach (var s in sessions.OrderByDescending(x => x.SessionDate))
        {
            rows.Add(new[]
            {
                s.Ficha?.FichaCode ?? "",
                s.Ficha?.InstructorName ?? "",
                s.ProductionOrder?.OrderNumber ?? "",
                s.Units.ToString(),
                s.SessionDate.ToString("yyyy-MM-dd HH:mm"),
                s.Observations ?? ""
            });
        }

        if (!sessions.Any())
            rows.Add(new[] { "(Sin sesiones en el período / filtros)", "", "", "", "", "" });

        rows.Add(new[] { "", "", "", "", "", "" });
        rows.Add(new[] { "CALIDAD", "Orden", "Unidades", "Resultado", "Motivo", "Responsable" });
        foreach (var q in qualityPeriod.OrderByDescending(x => x.InspectionDate))
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

        if (!qualityPeriod.Any())
            rows.Add(new[] { "(Sin inspecciones en el período)", "", "", "", "", "" });

        var headers = new[] { "Sección / Concepto", "Detalle", "Valor", "Extra", "Campo", "Campo 2" };
        return Build(fileTag, $"Reporte SIPITEX · {filterDesc}", headers, rows, filter.Format);
    }

    private static (DateTime From, DateTime To, string Label, string FileTag) ResolvePeriod(ReportFilterDto filter)
    {
        var today = DateTime.Today;
        var period = (filter.Period ?? "mes").Trim().ToLowerInvariant();
        var culture = new System.Globalization.CultureInfo("es-CO");

        return period switch
        {
            "dia" or "diario" => ResolveDay(filter.Date ?? DateOnly.FromDateTime(today), culture),
            "semana" or "semanal" => ResolveWeek(filter.Date ?? DateOnly.FromDateTime(today), culture),
            "anio" or "año" or "anual" => ResolveYear(filter.Year ?? today.Year, culture),
            _ => ResolveMonth(filter.Year ?? today.Year, filter.Month ?? today.Month, culture)
        };
    }

    private static (DateTime From, DateTime To, string Label, string FileTag) ResolveDay(DateOnly date, System.Globalization.CultureInfo culture)
    {
        var from = date.ToDateTime(TimeOnly.MinValue);
        return (from, from.AddDays(1), $"Diario {date:yyyy-MM-dd}", $"Diario_{date:yyyyMMdd}");
    }

    private static (DateTime From, DateTime To, string Label, string FileTag) ResolveWeek(DateOnly date, System.Globalization.CultureInfo culture)
    {
        var dt = date.ToDateTime(TimeOnly.MinValue);
        var diff = ((int)dt.DayOfWeek + 6) % 7; // lunes = inicio
        var from = dt.AddDays(-diff);
        var to = from.AddDays(7);
        return (from, to, $"Semanal {from:yyyy-MM-dd} a {to.AddDays(-1):yyyy-MM-dd}", $"Semanal_{from:yyyyMMdd}");
    }

    private static (DateTime From, DateTime To, string Label, string FileTag) ResolveMonth(int year, int month, System.Globalization.CultureInfo culture)
    {
        if (month is < 1 or > 12) month = DateTime.Today.Month;
        if (year < 2000 || year > 2100) year = DateTime.Today.Year;
        var from = new DateTime(year, month, 1);
        var label = from.ToString("MMMM yyyy", culture);
        return (from, from.AddMonths(1), $"Mensual {label}", $"Mensual_{year:0000}{month:00}");
    }

    private static (DateTime From, DateTime To, string Label, string FileTag) ResolveYear(int year, System.Globalization.CultureInfo culture)
    {
        if (year < 2000 || year > 2100) year = DateTime.Today.Year;
        var from = new DateTime(year, 1, 1);
        return (from, from.AddYears(1), $"Anual {year}", $"Anual_{year}");
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
                page.Footer().AlignCenter().Text("SENA CMTC · ADSO").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf();

        return new ReportFileDto(pdf, "application/pdf", $"SIPITEX_{name}_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }
}
