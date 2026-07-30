using Sipitex.Domain.Enums;

namespace Sipitex.Web.Helpers;

// Esta clase ayuda a mostrar etiquetas visuales en la interfaz, como colores de estado.
public static class DisplayHelper
{
    // Devuelve la clase CSS del badge según el estado del material
    public static string BadgeClass(MaterialStatus status) => status switch
    {
        MaterialStatus.Bueno => "badge-success",
        MaterialStatus.Regular => "badge-warning",
        _ => "badge-danger" // Deteriorado u otro
    };

    // Colores para el estado de una orden de producción
    public static string BadgeClass(OrderStatus status) => status switch
    {
        OrderStatus.Finalizada => "badge-success",
        OrderStatus.EnProceso => "badge-info",
        OrderStatus.Pendiente => "badge-warning",
        _ => "badge-danger" // Cancelada
    };

    // Colores para solicitudes de material (pendiente/aprobada/rechazada)
    public static string BadgeClass(RequestStatus status) => status switch
    {
        RequestStatus.Pendiente => "badge-warning",
        RequestStatus.Aprobada => "badge-success",
        RequestStatus.Rechazada => "badge-danger",
        _ => "badge-info"
    };

    // Colores para resultados de inspección de calidad
    public static string BadgeClass(QualityResult result) => result switch
    {
        QualityResult.Aprobada => "badge-success",
        QualityResult.Reproceso => "badge-warning",
        _ => "badge-danger" // Rechazada
    };

    // Colores para cumplimiento normativo (si aplica en reportes)
    public static string BadgeClass(ComplianceStatus status) => status switch
    {
        ComplianceStatus.Cumple => "badge-success",
        ComplianceStatus.Parcial => "badge-warning",
        _ => "badge-danger"
    };

    // Texto legible en español; si no hay caso, deja el ToString del enum
    public static string StatusText(Enum value) => value switch
    {
        OrderStatus.EnProceso => "En proceso",
        OrderStatus.Pendiente => "Pendiente",
        OrderStatus.Finalizada => "Finalizada",
        OrderStatus.Cancelada => "Cancelada",
        RequestStatus.Pendiente => "Pendiente",
        RequestStatus.Aprobada => "Aprobada",
        RequestStatus.Rechazada => "Rechazada",
        MaterialStatus.Bueno => "Bueno",
        MaterialStatus.Regular => "Regular",
        MaterialStatus.Deteriorado => "Deteriorado",
        QualityResult.Aprobada => "Aprobada",
        QualityResult.Reproceso => "Reproceso",
        QualityResult.Rechazada => "Rechazada",
        _ => value.ToString()
    };
}
