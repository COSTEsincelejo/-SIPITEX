using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;

namespace Sipitex.Infrastructure.Reporting;

// Genera el reporte Word (.docx) del catálogo de funcionalidades SIPITEX
public class FuncionalidadesReportService : IFuncionalidadesReportService
{
    private readonly IReadOnlyList<FuncionalidadCatalogItem> _catalog;

    // DI usa el catálogo por defecto
    public FuncionalidadesReportService()
        : this(FuncionalidadesCatalog.Default)
    {
    }

    // Constructor para pruebas (catálogo vacío o personalizado)
    public FuncionalidadesReportService(IReadOnlyList<FuncionalidadCatalogItem> catalog)
    {
        _catalog = catalog ?? Array.Empty<FuncionalidadCatalogItem>();
    }

    public IReadOnlyList<FuncionalidadCatalogItem> GetCatalog() => _catalog;

    public ReportFileDto GenerateDocx(DateTime? generatedAt = null)
    {
        var stamp = generatedAt ?? DateTime.Now;
        var bytes = BuildDocument(_catalog, stamp);
        var fileName = $"SIPITEX_Funcionalidades_{stamp:yyyyMMdd_HHmm}.docx";
        return new ReportFileDto(
            bytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            fileName);
    }

    private static byte[] BuildDocument(IReadOnlyList<FuncionalidadCatalogItem> catalog, DateTime stamp)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            var body = mainPart.Document.Body!;

            AppendCover(body, stamp);
            AppendPageBreak(body);

            var byModule = catalog
                .GroupBy(i => i.Modulo, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (byModule.Count == 0)
            {
                body.AppendChild(CreateParagraph(
                    "No hay funcionalidades registradas en el catálogo.",
                    fontSize: "22",
                    color: "666666",
                    italic: true));
            }
            else
            {
                body.AppendChild(CreateParagraph(
                    "Listado de funcionalidades por módulo",
                    bold: true,
                    fontSize: "28",
                    color: "1B4F72",
                    spacingAfter: 200));

                body.AppendChild(CreateParagraph(
                    $"Total: {catalog.Count} funcionalidad(es) en {byModule.Count} módulo(s).",
                    fontSize: "20",
                    color: "555555",
                    spacingAfter: 300));

                foreach (var module in byModule)
                {
                    AppendModuleSection(body, module.Key, module.ToList());
                }
            }

            AppendFooterNote(body);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static void AppendCover(Body body, DateTime stamp)
    {
        // Espacio superior para centrar visualmente la portada
        for (var i = 0; i < 4; i++)
            body.AppendChild(CreateParagraph(""));

        body.AppendChild(CreateParagraph(
            "SIPITEX",
            bold: true,
            fontSize: "56",
            color: "1B4F72",
            justify: JustificationValues.Center,
            spacingAfter: 120));

        body.AppendChild(CreateParagraph(
            "Sistema Integrado de Aprendizaje Producción e Inventario Textil",
            fontSize: "22",
            color: "555555",
            justify: JustificationValues.Center,
            spacingAfter: 400));

        body.AppendChild(CreateParagraph(
            "Reporte de funcionalidades del sistema",
            bold: true,
            fontSize: "32",
            color: "2E86C1",
            justify: JustificationValues.Center,
            spacingAfter: 200));

        body.AppendChild(CreateParagraph(
            $"Fecha de generación: {stamp:dd/MM/yyyy HH:mm}",
            fontSize: "20",
            color: "666666",
            justify: JustificationValues.Center,
            spacingAfter: 80));

        body.AppendChild(CreateParagraph(
            "CMTC · SENA · ADSO",
            fontSize: "18",
            color: "888888",
            justify: JustificationValues.Center));
    }

    private static void AppendModuleSection(Body body, string moduleName, IReadOnlyList<FuncionalidadCatalogItem> items)
    {
        body.AppendChild(CreateParagraph(
            moduleName,
            bold: true,
            fontSize: "24",
            color: "1B4F72",
            spacingBefore: 280,
            spacingAfter: 120));

        var table = new Table();
        table.AppendChild(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = "BFBFBF" },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "BFBFBF" },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "BFBFBF" },
                new RightBorder { Val = BorderValues.Single, Size = 4, Color = "BFBFBF" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "BFBFBF" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "BFBFBF" })));

        table.AppendChild(CreateHeaderRow("Funcionalidad", "Descripción", "Rol que la usa"));

        foreach (var item in items)
            table.AppendChild(CreateDataRow(item.Funcionalidad, item.Descripcion, item.Rol));

        body.AppendChild(table);
    }

    private static void AppendFooterNote(Body body)
    {
        body.AppendChild(CreateParagraph(""));
        body.AppendChild(CreateParagraph(
            "Documento generado automáticamente por SIPITEX. El catálogo refleja los módulos implementados en la aplicación.",
            fontSize: "16",
            color: "888888",
            italic: true,
            spacingBefore: 400));
    }

    private static void AppendPageBreak(Body body)
    {
        body.AppendChild(new Paragraph(
            new Run(new Break { Type = BreakValues.Page })));
    }

    private static TableRow CreateHeaderRow(params string[] cells)
    {
        var row = new TableRow();
        foreach (var text in cells)
        {
            row.AppendChild(CreateTableCell(text, bold: true, shading: "1B4F72", color: "FFFFFF"));
        }
        return row;
    }

    private static TableRow CreateDataRow(params string[] cells)
    {
        var row = new TableRow();
        foreach (var text in cells)
            row.AppendChild(CreateTableCell(text));
        return row;
    }

    private static TableCell CreateTableCell(string text, bool bold = false, string? shading = null, string color = "333333")
    {
        var runProps = new RunProperties(
            new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
            new FontSize { Val = "18" },
            new Color { Val = color });
        if (bold)
            runProps.AppendChild(new Bold());

        var paragraph = new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { After = "40", Before = "40", Line = "240", LineRule = LineSpacingRuleValues.Auto }),
            new Run(runProps, new Text(text)));

        var cellProps = new TableCellProperties(
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
            new TableCellWidth { Type = TableWidthUnitValues.Auto });

        if (!string.IsNullOrWhiteSpace(shading))
            cellProps.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Fill = shading, Color = "auto" });

        return new TableCell(cellProps, paragraph);
    }

    private static Paragraph CreateParagraph(
        string text,
        bool bold = false,
        string fontSize = "22",
        string color = "333333",
        bool italic = false,
        JustificationValues? justify = null,
        int spacingBefore = 0,
        int spacingAfter = 120)
    {
        var runProps = new RunProperties(
            new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
            new FontSize { Val = fontSize },
            new Color { Val = color });
        if (bold) runProps.AppendChild(new Bold());
        if (italic) runProps.AppendChild(new Italic());

        var paraProps = new ParagraphProperties(
            new SpacingBetweenLines
            {
                Before = spacingBefore.ToString(),
                After = spacingAfter.ToString()
            });
        if (justify is not null)
            paraProps.AppendChild(new Justification { Val = justify });

        return new Paragraph(paraProps, new Run(runProps, new Text(text)));
    }
}
