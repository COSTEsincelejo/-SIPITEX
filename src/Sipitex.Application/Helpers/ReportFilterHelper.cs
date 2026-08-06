using Sipitex.Application.DTOs;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Helpers;

// Reglas de filtrado compartidas para reportes (según campos reales de cada entidad)
public static class ReportFilterHelper
{
    // Día puntual tiene prioridad; si no, mes+año; si no, solo año
    public static bool MatchesDate(DateOnly date, ReportFilterDto? filter)
    {
        if (filter is null || !filter.HasAny) return true;

        if (filter.Fecha is DateOnly day)
            return date == day;

        if (filter.Mes is int mes and > 0 && filter.Anio is int anio and > 0)
            return date.Month == mes && date.Year == anio;

        if (filter.Anio is int soloAnio and > 0)
            return date.Year == soloAnio;

        return true;
    }

    public static bool MatchesDateTime(DateTime dateTime, ReportFilterDto? filter) =>
        MatchesDate(DateOnly.FromDateTime(dateTime), filter);

    // Instructor / ficha / jornada viven en Ficha (no en Material ni en QualityRecord directo)
    public static bool MatchesFicha(Ficha ficha, ReportFilterDto? filter)
    {
        if (filter is null) return true;

        if (filter.FichaId is int fichaId and > 0 && ficha.Id != fichaId)
            return false;

        if (!string.IsNullOrWhiteSpace(filter.Jornada)
            && !string.Equals(ficha.Turno, filter.Jornada.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        if (filter.InstructorId is int instructorId and > 0
            && !BelongsToInstructor(ficha, instructorId))
            return false;

        return true;
    }

    public static bool NeedsFichaScope(ReportFilterDto? filter) =>
        filter is not null
        && (filter.InstructorId is > 0
            || filter.FichaId is > 0
            || !string.IsNullOrWhiteSpace(filter.Jornada));

    public static HashSet<int> MatchingOrderIds(
        IEnumerable<Ficha> fichas,
        ReportFilterDto? filter)
    {
        if (!NeedsFichaScope(filter))
            return [];

        return fichas
            .Where(f => MatchesFicha(f, filter))
            .Where(f => f.ProductionOrderId is > 0)
            .Select(f => f.ProductionOrderId!.Value)
            .ToHashSet();
    }

    private static bool BelongsToInstructor(Ficha ficha, int instructorUserId)
    {
        if (ficha.Instructors.Any(i => i.UserId == instructorUserId))
            return true;
        return ficha.InstructorUserId == instructorUserId;
    }
}
