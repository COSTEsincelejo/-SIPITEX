namespace Sipitex.Application.DTOs;

// Consulta de la pantalla Actividad del instructor. El rango es inclusivo.
public record ActividadInstructorConsulta(
    int InstructorId,
    DateOnly Desde,
    DateOnly Hasta,
    int? FichaId = null,
    string? Jornada = null,
    string? Tipo = null);

public static class ActividadInstructorTexto
{
    public const string SinRegistros =
        "No se encontraron registros de actividad para este instructor en el período seleccionado";

    public const string NoRegistrado = "No registrado";

    public const string Produccion = "produccion";
    public const string Calidad = "calidad";
    public const string Consumo = "consumo";
    public const string Grupo = "grupo";

    public static string EtiquetaTipo(string tipo) => tipo switch
    {
        Produccion => "Producción",
        Calidad => "Control de calidad",
        Consumo => "Consumo de materiales",
        Grupo => "Grupo de confección",
        _ => tipo
    };

    public static readonly string[] TiposConsultables = [Produccion, Calidad, Consumo, Grupo];
}

public record ActividadInstructorReporteDto(
    bool InstructorExiste,
    int InstructorId,
    string Nombre,
    string? PhotoPath,
    string Iniciales,
    DateOnly Desde,
    DateOnly Hasta,
    string PeriodoTexto,
    string? FichaFiltro,
    string? JornadaFiltro,
    string? TipoFiltro,
    bool TieneActividad,
    int DiasEnPeriodo,
    int DiasConActividad,
    int DiasSinActividad,
    string HorasRegistradas,
    string Aprendices,
    int Grupos,
    int UnidadesEnSesiones,
    int PrendasEnGrupos,
    IReadOnlyList<string> ActividadesRealizadas,
    IReadOnlyList<string> ActividadesNoRealizadas,
    IReadOnlyList<ActividadInstructorDiaDto> Dias,
    string CodigoVerificacion);

public record ActividadInstructorDiaDto(
    DateOnly Fecha,
    string FechaTexto,
    bool ConActividad,
    string Ingreso,
    string Salida,
    IReadOnlyList<string> Actividades,
    IReadOnlyList<string> ConQuien,
    string Aprendices,
    IReadOnlyList<string> Observaciones);
