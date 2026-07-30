using ClosedXML.Excel; // Genera archivos .xlsx
using QuestPDF.Fluent; // API fluida de QuestPDF
using QuestPDF.Helpers; // Colores, márgenes...
using QuestPDF.Infrastructure; // Licencia y tipos base
using Sipitex.Application.DTOs; // ReportFileDto
using Sipitex.Application.Helpers; // UnitHelper para mostrar unidades
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
        IStatisticsService statisticsService)
    {
        _materialRepository = materialRepository;
        _orderRepository = orderRepository;
        _qualityRepository = qualityRepository;
        _statisticsService = statisticsService;
    }

    // Reporte de inventario: stock, mínimos, estado...
    public async Task<ReportFileDto> ExportInventoryAsync(string format, CancellationToken cancellationToken = default)
    {
        var materials = await _materialRepository.GetAllAsync(cancellationToken); // Todos los materiales
        var rows = materials.Select(m => new[] // Una fila por material
        {
            m.Name,
            UnitHelper.ToDisplay(m.Unit), // "Metros", "Unidades"...
            m.Stock.ToString("0.##"), // Stock actual
            m.MinStock.ToString("0.##"), // Umbral mínimo
            m.Status.ToString(), // Bueno, Regular, Deteriorado
            m.LastEntryDate.ToString("yyyy-MM-dd"), // Última entrada
            m.Stock < m.MinStock ? "Sí" : "No" // Alerta de bajo mínimo
        }).ToList();

        var headers = new[] { "Material", "Unidad", "Stock", "Mínimo", "Estado", "Última entrada", "Bajo mínimo" };
        return Build("Inventario", "Reporte de inventario SIPITEX", headers, rows, format);
    }

    // Reporte de órdenes de producción con avance %
    public async Task<ReportFileDto> ExportOrdersAsync(string format, CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetAllAsync(cancellationToken);
        var rows = orders.Select(o => new[]
        {
            o.OrderNumber, // OP-001...
            o.ProductName, // Camisa, Pantalón...
            o.TotalQuantity.ToString(), // Meta
            o.ProducedQuantity.ToString(), // Lo que ya llevan
            o.TotalQuantity == 0 ? "0%" : $"{o.ProducedQuantity * 100 / o.TotalQuantity}%", // Porcentaje de avance
            o.Status.ToString(), // En Proceso, Finalizada...
            o.Deadline.ToString("yyyy-MM-dd") // Fecha límite
        }).ToList();

        var headers = new[] { "Orden", "Producto", "Meta", "Producido", "Avance", "Estado", "Fecha límite" };
        return Build("Ordenes", "Reporte de órdenes de producción SIPITEX", headers, rows, format);
    }

    // Reporte de control de calidad (aprobado/reproceso)
    public async Task<ReportFileDto> ExportQualityAsync(string format, CancellationToken cancellationToken = default)
    {
        var records = await _qualityRepository.GetAllAsync(cancellationToken);
        var rows = records
            .OrderByDescending(r => r.InspectionDate) // Más recientes primero
            .Select(r => new[]
            {
                r.ProductionOrder.OrderNumber, // Orden inspeccionada
                r.UnitsInspected.ToString(), // Cuántas unidades revisaron
                r.Result.ToString(), // Aprobado, Reproceso...
                r.MotivoReproceso ?? "", // Vacío si no hubo reproceso
                r.Responsable ?? "",
                r.InspectionDate.ToString("yyyy-MM-dd")
            }).ToList();

        var headers = new[] { "Orden", "Unidades", "Resultado", "Motivo", "Responsable", "Fecha" };
        return Build("Calidad", "Reporte de control de calidad SIPITEX", headers, rows, format);
    }

    // Reporte del dashboard con KPIs y datos del gráfico
    public async Task<ReportFileDto> ExportDashboardAsync(string format, CancellationToken cancellationToken = default)
    {
        var dash = await _statisticsService.GetDashboardAsync(cancellationToken); // KPIs calculados
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
        // Normalizo: "xlsx" y "excel" van al mismo camino
        var normalized = format.Equals("excel", StringComparison.OrdinalIgnoreCase) || format.Equals("xlsx", StringComparison.OrdinalIgnoreCase)
            ? "excel"
            : "pdf";

        if (normalized == "excel")
        {
            using var wb = new XLWorkbook(); // Libro Excel nuevo
            var ws = wb.Worksheets.Add(name); // Hoja con el nombre del reporte
            for (var c = 0; c < headers.Length; c++)
                ws.Cell(1, c + 1).Value = headers[c]; // Fila 1 = encabezados
            ws.Row(1).Style.Font.Bold = true; // Encabezados en negrita
            for (var r = 0; r < rows.Count; r++)
            for (var c = 0; c < headers.Length; c++)
                ws.Cell(r + 2, c + 1).Value = rows[r][c]; // Datos desde fila 2
            ws.Columns().AdjustToContents(); // Ajusto ancho de columnas
            using var stream = new MemoryStream(); // Guardo en memoria
            wb.SaveAs(stream);
            return new ReportFileDto(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"SIPITEX_{name}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // Si no es excel, genero PDF con QuestPDF
        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40); // Margen de la hoja
                page.Header().Column(col =>
                {
                    col.Item().Text("SIPITEX").SemiBold().FontSize(18).FontColor(Colors.Blue.Darken2); // Logo textual
                    col.Item().Text(title).FontSize(12).FontColor(Colors.Grey.Darken2); // Título del reporte
                    col.Item().Text($"Generado: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium); // Fecha de generación
                });
                page.Content().PaddingVertical(16).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        foreach (var _ in headers)
                            cols.RelativeColumn(); // Columnas del mismo ancho relativo
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
                page.Footer().AlignCenter().Text("CMTC · SENA · ADSO").FontSize(8).FontColor(Colors.Grey.Medium); // Pie del documento
            });
        }).GeneratePdf();

        return new ReportFileDto(pdf, "application/pdf", $"SIPITEX_{name}_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }
}
