using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.DTOs;

// Archivo binario para descargas de reportes (PDF, Excel, etc.)
public record ReportFileDto(byte[] Content, string ContentType, string FileName);

public record AlertPreferenceDto(AlertType AlertType, string Title, string Description, bool Enabled, IReadOnlyList<string> SuggestedRoles);

public record AlertDeliveryDto(AlertType AlertType, string Subject, DateTime SentAt, string Channel);

// Resumen después de correr la evaluación de alertas
public record AlertEvaluationResultDto(int AlertsFound, int EmailsSent, IReadOnlyList<string> Details);

// Catálogo fijo de tipos de alerta (lo usamos en la UI y en AlertService)
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
