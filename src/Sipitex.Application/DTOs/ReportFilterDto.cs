namespace Sipitex.Application.DTOs;

// Filtros opcionales para exportación de reportes (query string del controller)
public record ReportFilterDto(
    int? InstructorId = null,
    int? FichaId = null,
    string? Jornada = null,
    DateOnly? Fecha = null,
    int? Mes = null,
    int? Anio = null)
{
    public bool HasAny =>
        InstructorId is > 0
        || FichaId is > 0
        || !string.IsNullOrWhiteSpace(Jornada)
        || Fecha is not null
        || Mes is > 0
        || Anio is > 0;

    // Resumen legible para PDF/UI (sin nombres resueltos)
    public string ToSummaryLabel()
    {
        if (!HasAny) return string.Empty;
        var parts = new List<string>();
        if (InstructorId is > 0) parts.Add($"InstructorId={InstructorId}");
        if (FichaId is > 0) parts.Add($"FichaId={FichaId}");
        if (!string.IsNullOrWhiteSpace(Jornada)) parts.Add($"Jornada={Jornada.Trim()}");
        if (Fecha is DateOnly d) parts.Add($"Fecha={d:yyyy-MM-dd}");
        else if (Mes is > 0 && Anio is > 0) parts.Add($"Periodo={Mes:00}/{Anio}");
        else if (Anio is > 0) parts.Add($"Año={Anio}");
        return string.Join(", ", parts);
    }
}
