using System.Globalization;
using ClosedXML.Excel;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Infrastructure.Reporting;

// Mismas cifras que ActividadInstructorReporteDto. No recalcula consumos ni horas.
public class ActividadInstructorExcel : IActividadInstructorExcel
{
    public ReportFileDto Render(ActividadInstructorReporteDto reporte)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Actividad");
        var fila = 1;

        ws.Cell(fila, 1).Value = "SIPITEX · Reporte de Actividad del Instructor";
        ws.Range(fila, 1, fila, 4).Merge().Style.Font.Bold = true;
        fila += 2;

        Escribir(ws, ref fila, "Instructor", reporte.Nombre);
        Escribir(ws, ref fila, "Período", reporte.PeriodoTexto);
        Escribir(ws, ref fila, "Ficha", reporte.FichaFiltro ?? "Todas");
        Escribir(ws, ref fila, "Jornada", reporte.JornadaFiltro ?? "Todas");
        Escribir(ws, ref fila, "Tipo de actividad", reporte.TipoFiltro ?? "Todas");
        Escribir(ws, ref fila, "Código de verificación", reporte.CodigoVerificacion);
        fila++;

        Encabezado(ws, fila, "Resumen");
        fila++;
        foreach (var (etiqueta, valor) in Resumen(reporte))
            Escribir(ws, ref fila, etiqueta, valor);
        if (!reporte.TieneActividad)
            Escribir(ws, ref fila, "Mensaje", ActividadInstructorTexto.SinRegistros);
        fila++;

        Encabezado(ws, fila, "Materiales consumidos");
        fila++;
        ws.Cell(fila, 1).Value = "Material";
        ws.Cell(fila, 2).Value = "Cantidad";
        ws.Cell(fila, 3).Value = "Unidad";
        ws.Row(fila).Style.Font.Bold = true;
        fila++;
        if (reporte.Materiales.Count == 0)
        {
            ws.Cell(fila, 1).Value = ActividadInstructorTexto.SinConsumos;
            fila++;
        }
        else
        {
            foreach (var material in reporte.Materiales)
            {
                ws.Cell(fila, 1).Value = material.Material;
                ws.Cell(fila, 2).Value = material.Cantidad;
                ws.Cell(fila, 3).Value = material.Unidad;
                fila++;
            }
        }

        fila++;
        Encabezado(ws, fila, "Detalle por día");
        fila++;
        string[] columnas = ["Fecha", "Estado", "Ingreso", "Salida", "Aprendices", "Actividades", "Con quién", "Observaciones"];
        for (var c = 0; c < columnas.Length; c++)
            ws.Cell(fila, c + 1).Value = columnas[c];
        ws.Row(fila).Style.Font.Bold = true;
        fila++;
        foreach (var dia in reporte.Dias)
        {
            ws.Cell(fila, 1).Value = dia.FechaTexto;
            ws.Cell(fila, 2).Value = dia.ConActividad ? "Con actividad" : "Sin registro";
            ws.Cell(fila, 3).Value = dia.Ingreso;
            ws.Cell(fila, 4).Value = dia.Salida;
            ws.Cell(fila, 5).Value = dia.Aprendices;
            ws.Cell(fila, 6).Value = dia.Actividades.Count == 0
                ? "Sin actividades registradas este día."
                : string.Join(" | ", dia.Actividades);
            ws.Cell(fila, 7).Value = dia.ConQuien.Count == 0
                ? "No quedó asociado a una ficha o grupo."
                : string.Join(" | ", dia.ConQuien);
            ws.Cell(fila, 8).Value = dia.Observaciones.Count == 0
                ? ActividadInstructorTexto.NoRegistrado
                : string.Join(" | ", dia.Observaciones);
            fila++;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return new ReportFileDto(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            NombreArchivo(reporte));
    }

    // Las mismas etiquetas y valores que muestra la pantalla.
    public static IReadOnlyList<(string Etiqueta, string Valor)> Resumen(ActividadInstructorReporteDto reporte) =>
    [
        ("Días con actividad", reporte.DiasConActividad.ToString(CultureInfo.InvariantCulture)),
        ("Días sin actividad", reporte.DiasSinActividad.ToString(CultureInfo.InvariantCulture)),
        ("Días del período", reporte.DiasEnPeriodo.ToString(CultureInfo.InvariantCulture)),
        ("Horas registradas", reporte.HorasRegistradas),
        ("Nota de horas", reporte.NotaHoras),
        ("Aprendices", reporte.Aprendices),
        ("Nota de aprendices", reporte.NotaAprendices),
        ("Grupos", reporte.Grupos.ToString(CultureInfo.InvariantCulture)),
        ("Unidades en sesiones", reporte.UnidadesEnSesiones.ToString(CultureInfo.InvariantCulture)),
        ("Prendas en grupos", reporte.PrendasEnGrupos.ToString(CultureInfo.InvariantCulture)),
        ("Actividades realizadas", reporte.ActividadesRealizadas.Count == 0
            ? "Ninguna en este período."
            : string.Join(", ", reporte.ActividadesRealizadas)),
        ("Actividades sin registro", reporte.ActividadesNoRealizadas.Count == 0
            ? "Todas las actividades consultadas tienen registro."
            : string.Join(", ", reporte.ActividadesNoRealizadas))
    ];

    private static void Encabezado(IXLWorksheet ws, int fila, string texto)
    {
        ws.Cell(fila, 1).Value = texto;
        ws.Cell(fila, 1).Style.Font.Bold = true;
    }

    private static void Escribir(IXLWorksheet ws, ref int fila, string etiqueta, string valor)
    {
        ws.Cell(fila, 1).Value = etiqueta;
        ws.Cell(fila, 2).Value = valor;
        fila++;
    }

    private static string NombreArchivo(ActividadInstructorReporteDto reporte)
    {
        var limpio = new string(reporte.Nombre
            .Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_')
            .ToArray())
            .Replace(' ', '_');
        if (string.IsNullOrWhiteSpace(limpio))
            limpio = "instructor";
        return $"SIPITEX_Actividad_{limpio}_{reporte.Desde:yyyyMMdd}_{reporte.Hasta:yyyyMMdd}.xlsx";
    }
}
