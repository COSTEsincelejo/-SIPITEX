using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Enums;

namespace Sipitex.Infrastructure.Reporting;

public class ActaPdfService : IActaPdfService
{
    static ActaPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public ReportFileDto Render(ActaMovimientoDto acta)
    {
        var titulo = acta.Tipo == ActaTipo.Ingreso
            ? "Acta de ingreso"
            : "Acta de egreso";

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Column(col =>
                {
                    col.Item().Text("SIPITEX · CMTC SENA").SemiBold().FontSize(11).FontColor(Colors.Grey.Darken1);
                    col.Item().Text(titulo.ToUpperInvariant()).SemiBold().FontSize(18).FontColor(Colors.Blue.Darken2);
                    col.Item().Text($"{acta.Numero} · {acta.FechaUtc:yyyy-MM-dd HH:mm} UTC").FontSize(10);
                });

                page.Content().PaddingVertical(16).Column(col =>
                {
                    col.Spacing(10);
                    if (!string.IsNullOrWhiteSpace(acta.OrderNumber))
                        col.Item().Text($"Orden: {acta.OrderNumber}").FontSize(10);
                    if (acta.EstadoProductoOrigen is { } from && acta.EstadoProductoDestino is { } to)
                        col.Item().Text($"Estado de producto: {from} → {to}").FontSize(10);
                    if (!string.IsNullOrWhiteSpace(acta.Observaciones))
                        col.Item().Text($"Observaciones: {acta.Observaciones}").FontSize(10);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(4);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                        });
                        table.Header(header =>
                        {
                            foreach (var h in new[] { "Tipo de ítem", "Descripción", "Cantidad", "Unidad" })
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text(h).FontColor(Colors.White).FontSize(9).SemiBold();
                        });
                        foreach (var d in acta.Detalles)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(LabelItem(d.ItemTipo)).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(d.Descripcion).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(d.Cantidad.ToString("0.####")).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(d.Unidad ?? "—").FontSize(9);
                        }
                    });

                    col.Item().PaddingTop(24).Row(row =>
                    {
                        row.RelativeItem().Element(c => Firma(c, "Quien entrega", acta.EntregaNombre, acta.EntregaCargo, acta.EntregaConformidadUtc));
                        row.ConstantItem(24);
                        row.RelativeItem().Element(c => Firma(c, "Quien recibe", acta.RecibeNombre, acta.RecibeCargo, acta.RecibeConformidadUtc));
                    });

                    col.Item().PaddingTop(12).Text("Validez por conformidad simple: nombre, cargo y marca de tiempo UTC de quien entrega y quien recibe.")
                        .FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                });

                page.Footer().AlignCenter().Text("CMTC · SENA · ADSO").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf();

        return new ReportFileDto(pdf, "application/pdf", $"SIPITEX_{acta.Numero}_{acta.FechaUtc:yyyyMMdd_HHmm}.pdf");
    }

    private static void Firma(IContainer container, string titulo, string nombre, string cargo, DateTime? conformeUtc)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Column(col =>
        {
            col.Item().Text(titulo).SemiBold().FontSize(10);
            col.Item().Text(nombre).FontSize(11);
            col.Item().Text(string.IsNullOrWhiteSpace(cargo) ? "Cargo no indicado" : cargo).FontSize(9).FontColor(Colors.Grey.Darken1);
            col.Item().PaddingTop(8).Text(conformeUtc is DateTime ts
                ? $"Conforme · {ts:yyyy-MM-dd HH:mm} UTC"
                : "Pendiente de conformidad").FontSize(9);
        });
    }

    private static string LabelItem(ActaItemTipo tipo) => tipo switch
    {
        ActaItemTipo.Material => "Material",
        ActaItemTipo.ProductoEnProceso => "Producto en proceso",
        ActaItemTipo.ProductoTerminado => "Producto terminado",
        _ => tipo.ToString()
    };
}
