using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Seguridad;
using Sipitex.Domain.Enums;

namespace Sipitex.Infrastructure.Reporting;

public class ActaPdfService : IActaPdfService
{
    private readonly PrivacidadOptions _privacidad;

    static ActaPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public ActaPdfService(IOptions<PrivacidadOptions>? privacidad = null)
    {
        _privacidad = privacidad?.Value ?? new PrivacidadOptions();
    }

    public ReportFileDto Render(ActaMovimientoDto acta, string? urlVerificacion = null)
    {
        var titulo = acta.Tipo == ActaTipo.Ingreso
            ? "Acta de ingreso"
            : "Acta de egreso";
        var verificacion = CodigoVerificacion(acta);
        var plantas = acta.Detalles
            .Select(d => d.PlantaInventario)
            .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
            .Select(nombre => nombre!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(nombre => nombre, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var pieInstitucional = PieInstitucional();

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
                    if (!string.IsNullOrWhiteSpace(acta.ProductoOrden))
                        col.Item().Text($"Producto: {acta.ProductoOrden}").FontSize(10);
                    col.Item().Text($"Estado: {EtiquetaActa(acta.Estado)}").FontSize(10);
                    col.Item().Text($"Planta origen: {NombrePlanta(acta.PlantaOrigenNombre)}").FontSize(10);
                    col.Item().Text($"Planta destino: {NombrePlanta(acta.PlantaDestinoNombre)}").FontSize(10);
                    if (acta.Estado == ActaEstado.Anulada)
                    {
                        col.Item().PaddingTop(4).Text("ANULADA").FontSize(28).Bold().FontColor(Colors.Red.Darken2);
                        col.Item().Text($"Motivo de anulación: {acta.MotivoAnulacion}").FontSize(10).FontColor(Colors.Red.Darken2);
                    }
                    else if (acta.Estado == ActaEstado.PendienteFirma)
                    {
                        col.Item().Text("Firma de quien recibe pendiente").FontSize(11).SemiBold();
                    }
                    if (plantas.Count > 0)
                        col.Item().Text($"Planta de inventario: {string.Join(", ", plantas)}").FontSize(10);
                    if (acta.EstadoProductoOrigen is { } from && acta.EstadoProductoDestino is { } to)
                        col.Item().Text($"Estado de producto: {EtiquetaEstado(from)} → {EtiquetaEstado(to)}").FontSize(10);
                    if (!string.IsNullOrWhiteSpace(acta.Observaciones))
                        col.Item().Text($"Observaciones: {acta.Observaciones}").FontSize(10);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1.6f);
                            cols.RelativeColumn(4);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                        });
                        table.Header(header =>
                        {
                            foreach (var h in new[] { "Tipo de ítem", "Código", "Descripción", "Cantidad", "Unidad" })
                                header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text(h).FontColor(Colors.White).FontSize(9).SemiBold();
                        });
                        foreach (var d in acta.Detalles)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(LabelItem(d.ItemTipo)).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(string.IsNullOrWhiteSpace(d.CodigoMaterial) ? "—" : d.CodigoMaterial).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(d.Descripcion).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(d.Cantidad.ToString("0.####")).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(d.Unidad ?? "—").FontSize(9);
                        }
                    });

                    col.Item().PaddingTop(24).Row(row =>
                    {
                        row.RelativeItem().Element(c => Firma(c, "Quien entrega", acta.EntregaNombre, acta.EntregaCargo, acta.EntregaConformidadUtc, acta.EntregaFirmaPng));
                        row.ConstantItem(24);
                        row.RelativeItem().Element(c => Firma(c, "Quien recibe", acta.RecibeNombre, acta.RecibeCargo, acta.RecibeConformidadUtc, acta.RecibeFirmaPng));
                    });

                    col.Item().PaddingTop(12).Text("Validez: nombre, cargo, marca de tiempo UTC y firma gráfica de quien entrega y quien recibe.")
                        .FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                });

                page.Footer().AlignCenter().Column(col =>
                {
                    if (!string.IsNullOrWhiteSpace(urlVerificacion))
                    {
                        col.Item().AlignCenter().Width(62).Height(62).Image(CodigoQr(urlVerificacion));
                        col.Item().AlignCenter().Text("Verifique este documento escaneando el código")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                    }

                    col.Item().AlignCenter().Text(pieInstitucional).FontSize(8).FontColor(Colors.Grey.Medium);
                    col.Item().AlignCenter().Text($"Código de verificación: {verificacion}").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();

        return new ReportFileDto(pdf, "application/pdf", $"SIPITEX_{acta.Numero}_{acta.FechaUtc:yyyyMMdd_HHmm}.pdf");
    }

    string IActaPdfService.CodigoVerificacion(ActaMovimientoDto acta) => CodigoVerificacion(acta);

    private string PieInstitucional()
    {
        var partes = new List<string>();
        var responsable = PrivacidadConfiguracion.ValorPublico(_privacidad.ResponsableNombre);
        var nit = PrivacidadConfiguracion.ValorPublico(_privacidad.NIT);
        var ciudad = PrivacidadConfiguracion.ValorPublico(_privacidad.Ciudad);
        if (responsable is not null)
            partes.Add(responsable);
        if (nit is not null)
            partes.Add("NIT " + nit);
        if (ciudad is not null)
            partes.Add(ciudad);
        if (partes.Count == 0)
            return "CMTC · SENA · ADSO";
        return string.Join(" · ", partes);
    }

    private static byte[] CodigoQr(string url)
    {
        using var generador = new QRCodeGenerator();
        using var datos = generador.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(datos);
        return png.GetGraphic(4);
    }

    public static string CodigoVerificacion(ActaMovimientoDto acta)
    {
        var contenido = new StringBuilder();
        contenido.Append(acta.Numero).Append('|')
            .Append(acta.Tipo).Append('|')
            .Append(acta.Origen).Append('|')
            .Append(acta.FechaUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)).Append('|')
            .Append(acta.OrderNumber ?? string.Empty).Append('|')
            .Append(acta.ProductoOrden ?? string.Empty).Append('|')
            .Append(acta.Observaciones ?? string.Empty).Append('|');
        foreach (var detalle in acta.Detalles)
        {
            contenido.Append(detalle.CodigoMaterial ?? string.Empty).Append(';')
                .Append(detalle.Descripcion).Append(';')
                .Append(detalle.Cantidad.ToString("0.####", CultureInfo.InvariantCulture)).Append(';')
                .Append(detalle.Unidad ?? string.Empty).Append(';')
                .Append(detalle.PlantaInventario ?? string.Empty).Append('|');
        }

        contenido.Append(acta.EntregaNombre).Append('|')
            .Append(acta.EntregaCargo).Append('|')
            .Append(acta.RecibeNombre).Append('|')
            .Append(acta.RecibeCargo);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(contenido.ToString()));
        return Convert.ToHexString(hash)[..12].ToLowerInvariant();
    }

    private static string EtiquetaActa(ActaEstado estado) => estado switch
    {
        ActaEstado.PendienteFirma => "Pendiente de firma",
        ActaEstado.Firmada => "Firmada",
        ActaEstado.Anulada => "Anulada",
        _ => estado.ToString()
    };

    private static string NombrePlanta(string? nombre) =>
        string.IsNullOrWhiteSpace(nombre) ? "—" : nombre;

    private static string EtiquetaEstado(EstadoProducto estado) => estado switch
    {
        EstadoProducto.MateriaPrima => "Materia prima",
        EstadoProducto.Corte => "Corte",
        EstadoProducto.Confeccion => "Confección",
        EstadoProducto.Calidad => "Calidad",
        EstadoProducto.ProductoTerminado => "Producto terminado",
        EstadoProducto.VentaEntrega => "Venta / entrega",
        _ => estado.ToString()
    };

    private static void Firma(IContainer container, string titulo, string nombre, string cargo, DateTime? conformeUtc, byte[]? firmaPng)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Column(col =>
        {
            col.Item().Text(titulo).SemiBold().FontSize(10);
            col.Item().Text(nombre).FontSize(11);
            col.Item().Text(string.IsNullOrWhiteSpace(cargo) ? "Cargo no indicado" : cargo).FontSize(9).FontColor(Colors.Grey.Darken1);
            if (firmaPng is { Length: > 0 })
                col.Item().PaddingTop(6).Height(48).Image(firmaPng).FitArea();
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
