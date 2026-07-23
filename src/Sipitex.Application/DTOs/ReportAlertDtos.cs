using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.DTOs;

public record ReportFileDto(byte[] Content, string ContentType, string FileName);

public record ReportFilterDto(
    string Period,
    DateOnly? Date = null,
    int? Year = null,
    int? Month = null,
    string? Instructor = null,
    int? FichaId = null,
    string Format = "pdf");

public record AlertPreferenceDto(AlertType AlertType, string Title, string Description, bool Enabled, IReadOnlyList<string> SuggestedRoles);

public record AlertDeliveryDto(AlertType AlertType, string Subject, DateTime SentAt, string Channel);

public record AlertEvaluationResultDto(int AlertsFound, int EmailsSent, IReadOnlyList<string> Details);

public static class AlertCatalog
{
    public static IReadOnlyList<(AlertType Type, string Title, string Description, string[] Roles)> All { get; } =
    [
        (AlertType.StockBajo, "Stock bajo mínimo", "Materiales con stock por debajo del mínimo.", [UserRoles.Administrador, UserRoles.Bodeguero]),
        (AlertType.SolicitudPendiente, "Solicitudes pendientes", "Solicitudes de material sin aprobar/rechazar.", [UserRoles.Administrador, UserRoles.Bodeguero]),
        (AlertType.OrdenPorVencer, "Órdenes por vencer", "Órdenes activas con fecha límite en 7 días o menos.", [UserRoles.Administrador, UserRoles.Instructor]),
        (AlertType.ReprocesoCalidad, "Reprocesos de calidad", "Inspecciones recientes con resultado Reproceso.", [UserRoles.Administrador, UserRoles.Instructor]),
        (AlertType.OrdenAtrasada, "Órdenes atrasadas", "Órdenes con avance menor al 50% y plazo cercano.", [UserRoles.Administrador, UserRoles.Instructor])
    ];
}
