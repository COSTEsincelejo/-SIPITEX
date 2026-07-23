using Sipitex.Domain.Enums;

namespace Sipitex.Web.Helpers;

// Esta clase ayuda a mostrar etiquetas visuales en la interfaz, como colores de estado.
public static class DisplayHelper
{
    public static string BadgeClass(MaterialStatus status) => status switch
    {
        MaterialStatus.Bueno => "badge-success",
        MaterialStatus.Regular => "badge-warning",
        _ => "badge-danger"
    };

    public static string BadgeClass(OrderStatus status) => status switch
    {
        OrderStatus.Finalizada => "badge-success",
        OrderStatus.EnProceso => "badge-info",
        OrderStatus.Pendiente => "badge-warning",
        _ => "badge-danger"
    };

    public static string BadgeClass(RequestStatus status) => status switch
    {
        RequestStatus.Pendiente => "badge-warning",
        RequestStatus.Aprobada => "badge-success",
        RequestStatus.Rechazada => "badge-danger",
        _ => "badge-info"
    };

    public static string BadgeClass(QualityResult result) => result switch
    {
        QualityResult.Aprobada => "badge-success",
        QualityResult.Reproceso => "badge-warning",
        _ => "badge-danger"
    };

    public static string BadgeClass(ComplianceStatus status) => status switch
    {
        ComplianceStatus.Cumple => "badge-success",
        ComplianceStatus.Parcial => "badge-warning",
        _ => "badge-danger"
    };

    public static string StatusText(Enum value) => value switch
    {
        OrderStatus.EnProceso => "En Proceso",
        _ => value.ToString()
    };
}
