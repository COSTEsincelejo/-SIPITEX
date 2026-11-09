using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

public interface IActividadInstructorService
{
    Task<ActividadInstructorReporteDto> ConsultarAsync(
        ActividadInstructorConsulta consulta,
        CancellationToken cancellationToken = default);
}

// El PDF vive en Infrastructure (QuestPDF). La web solo pide el archivo.
public interface IActividadInstructorPdf
{
    ReportFileDto Render(ActividadInstructorReporteDto reporte);
}

// El Excel sale del mismo reporte que la pantalla y el PDF.
public interface IActividadInstructorExcel
{
    ReportFileDto Render(ActividadInstructorReporteDto reporte);
}
