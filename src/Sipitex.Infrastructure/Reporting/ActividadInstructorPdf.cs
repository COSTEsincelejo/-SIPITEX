using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Infrastructure.Reporting;

public class ActividadInstructorPdf : IActividadInstructorPdf
{
    private const string Logo = """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 40 40">
          <rect width="40" height="40" rx="10" fill="#0B6FE0"/>
          <path d="M10 14h20M10 20h20M10 26h20" stroke="#D6E6FA" stroke-width="2" stroke-linecap="round"/>
          <path d="M15 11v18M20 11v18M25 11v18" stroke="#E7F6F4" stroke-width="2" stroke-linecap="round"/>
          <circle cx="20" cy="20" r="6.5" fill="#ffffff"/>
          <path d="M17 20h6M20 17v6" stroke="#0A2540" stroke-width="2" stroke-linecap="round"/>
        </svg>
        """;

    static ActividadInstructorPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public ReportFileDto Render(ActividadInstructorReporteDto reporte)
    {
        var generado = AhoraBogota();
        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Grey.Darken4));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(34).Height(34).Svg(Logo);
                        row.ConstantItem(10);
                        row.RelativeItem().Column(texto =>
                        {
                            texto.Item().Text("SIPITEX · CMTC SENA").SemiBold().FontSize(9).FontColor(Colors.Blue.Darken2);
                            texto.Item().Text("Reporte de Actividad del Instructor").SemiBold().FontSize(16).FontColor(Colors.Blue.Darken3);
                        });
                    });
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Blue.Darken2);
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text("Datos del instructor").SemiBold().FontSize(12).FontColor(Colors.Blue.Darken2);
                    col.Item().Text($"Instructor: {reporte.Nombre}");
                    col.Item().Text($"Período: {reporte.PeriodoTexto}");
                    col.Item().Text($"Ficha: {reporte.FichaFiltro ?? "Todas"}");
                    col.Item().Text($"Jornada: {reporte.JornadaFiltro ?? "Todas"}");
                    col.Item().Text($"Tipo de actividad: {reporte.TipoFiltro ?? "Todas"}");

                    col.Item().PaddingTop(6).Text("Resumen ejecutivo").SemiBold().FontSize(12).FontColor(Colors.Blue.Darken2);
                    col.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });
                        Metrica(tabla, "Días con actividad", reporte.DiasConActividad.ToString(CultureInfo.InvariantCulture));
                        Metrica(tabla, "Días sin actividad", reporte.DiasSinActividad.ToString(CultureInfo.InvariantCulture));
                        Metrica(tabla, "Horas registradas", reporte.HorasRegistradas);
                        Metrica(tabla, "Días del período", reporte.DiasEnPeriodo.ToString(CultureInfo.InvariantCulture));
                        Metrica(tabla, "Unidades en sesiones", reporte.UnidadesEnSesiones.ToString(CultureInfo.InvariantCulture));
                        Metrica(tabla, "Prendas en grupos", reporte.PrendasEnGrupos.ToString(CultureInfo.InvariantCulture));
                        Metrica(tabla, "Grupos", reporte.Grupos.ToString(CultureInfo.InvariantCulture));
                        Metrica(tabla, "Aprendices", reporte.Aprendices);
                    });

                    col.Item().Text(texto =>
                    {
                        texto.Span("Realizó: ").SemiBold();
                        texto.Span(reporte.ActividadesRealizadas.Count == 0
                            ? "Ninguna en este período."
                            : string.Join(", ", reporte.ActividadesRealizadas) + ".");
                    });
                    col.Item().Text(texto =>
                    {
                        texto.Span("No registró: ").SemiBold();
                        texto.Span(reporte.ActividadesNoRealizadas.Count == 0
                            ? "Todas las actividades consultadas tienen registro."
                            : string.Join(", ", reporte.ActividadesNoRealizadas) + ".");
                    });

                    col.Item().Text(reporte.NotaHoras);
                    col.Item().Text(reporte.NotaAprendices);

                    col.Item().PaddingTop(6).Text("Materiales consumidos").SemiBold().FontSize(12).FontColor(Colors.Blue.Darken2);
                    if (reporte.Materiales.Count == 0)
                    {
                        col.Item().Text(ActividadInstructorTexto.SinConsumos).Italic();
                    }
                    else
                    {
                        col.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            tabla.Header(header =>
                            {
                                header.Cell().Text("Material").SemiBold();
                                header.Cell().Text("Cantidad").SemiBold();
                                header.Cell().Text("Unidad").SemiBold();
                            });
                            foreach (var material in reporte.Materiales)
                            {
                                tabla.Cell().Text(material.Material);
                                tabla.Cell().Text(material.Cantidad);
                                tabla.Cell().Text(material.Unidad);
                            }
                        });
                    }

                    if (!reporte.TieneActividad)
                    {
                        col.Item().PaddingTop(8).Background(Colors.Grey.Lighten4).Padding(10)
                            .Text(ActividadInstructorTexto.SinRegistros).Italic();
                    }

                    col.Item().PaddingTop(6).Text("Detalle día por día").SemiBold().FontSize(12).FontColor(Colors.Blue.Darken2);
                    foreach (var dia in reporte.Dias)
                    {
                        col.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(bloque =>
                        {
                            bloque.Spacing(3);
                            bloque.Item().Row(row =>
                            {
                                row.RelativeItem().Text(dia.FechaTexto).SemiBold();
                                row.ConstantItem(110).AlignRight().Text(dia.ConActividad ? "Con actividad" : "Sin registro")
                                    .FontSize(9)
                                    .FontColor(dia.ConActividad ? Colors.Green.Darken2 : Colors.Grey.Darken1);
                            });
                            bloque.Item().Text($"Ingreso: {dia.Ingreso}    Salida: {dia.Salida}");
                            if (!dia.ConActividad)
                            {
                                bloque.Item().Text("Sin actividades registradas este día.").FontColor(Colors.Grey.Darken1);
                            }
                            else
                            {
                                bloque.Item().Text("Actividades").SemiBold().FontSize(9);
                                foreach (var actividad in dia.Actividades)
                                    bloque.Item().Text("• " + actividad);
                                bloque.Item().Text("Aprendices / grupos").SemiBold().FontSize(9);
                                bloque.Item().Text(dia.ConQuien.Count == 0
                                    ? "Grupo: no quedó asociado a una ficha."
                                    : string.Join("; ", dia.ConQuien));
                                bloque.Item().Text($"Aprendices: {dia.Aprendices}");
                                bloque.Item().Text(dia.Observaciones.Count == 0
                                    ? "Observaciones: No registrado"
                                    : "Observaciones: " + string.Join(" ", dia.Observaciones));
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Column(col =>
                {
                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(4).Text(
                            $"Generado: {generado:yyyy-MM-dd HH:mm} (Bogotá)  ·  Código de verificación: {reporte.CodigoVerificacion}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    col.Item().AlignCenter().Text(texto =>
                    {
                        texto.DefaultTextStyle(t => t.FontSize(8).FontColor(Colors.Grey.Medium));
                        texto.Span("Página ");
                        texto.CurrentPageNumber();
                        texto.Span(" de ");
                        texto.TotalPages();
                    });
                });
            });
        }).GeneratePdf();

        var archivo = NombreArchivo(reporte);
        return new ReportFileDto(pdf, "application/pdf", archivo);
    }

    private static void Metrica(TableDescriptor tabla, string etiqueta, string valor)
    {
        tabla.Cell().Border(0.4f).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(col =>
        {
            col.Item().Text(etiqueta).FontSize(8).FontColor(Colors.Grey.Darken1);
            col.Item().Text(valor).SemiBold().FontSize(11);
        });
    }

    private static string NombreArchivo(ActividadInstructorReporteDto reporte)
    {
        var limpio = new string(reporte.Nombre
            .Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_')
            .ToArray())
            .Replace(' ', '_');
        if (string.IsNullOrWhiteSpace(limpio))
            limpio = "instructor";
        return $"SIPITEX_Actividad_{limpio}_{reporte.Desde:yyyyMMdd}_{reporte.Hasta:yyyyMMdd}.pdf";
    }

    private static DateTime AhoraBogota()
    {
        var utc = DateTime.UtcNow;
        try
        {
            var zona = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
            return TimeZoneInfo.ConvertTimeFromUtc(utc, zona);
        }
        catch (TimeZoneNotFoundException)
        {
            return utc;
        }
    }
}
