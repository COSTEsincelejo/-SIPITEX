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

    public static string BadgeClass(OrderMaterialsStatus status) => status switch
    {
        OrderMaterialsStatus.NoAplica => "badge-info",
        OrderMaterialsStatus.ListaParaProduccion => "badge-success",
        OrderMaterialsStatus.MaterialesValidados => "badge-info",
        OrderMaterialsStatus.EntregaParcial => "badge-warning",
        OrderMaterialsStatus.PendienteRevisionBodega => "badge-warning",
        _ => "badge-info"
    };

    public static string BadgeClass(ProductionStageStatus status) => status switch
    {
        ProductionStageStatus.Finalizado => "badge-success",
        ProductionStageStatus.EnProceso => "badge-info",
        ProductionStageStatus.Pausado => "badge-warning",
        _ => "badge-warning"
    };

    public static string BadgeClass(MaterialStockAvailability availability) => availability switch
    {
        MaterialStockAvailability.Suficiente => "badge-success",
        MaterialStockAvailability.Insuficiente => "badge-warning",
        MaterialStockAvailability.SinExistencias => "badge-danger",
        _ => "badge-info"
    };

    // Colores para solicitudes de material (pendiente/aprobada/rechazada)
    public static string BadgeClass(RequestStatus status) => status switch
    {
        RequestStatus.Pendiente => "badge-warning",
        RequestStatus.Aprobada => "badge-success",
        RequestStatus.Rechazada => "badge-danger",
        _ => "badge-info"
    };

    // Colores para SolicitudMaterial (flujo Ficha multi-ítem)
    public static string BadgeClass(SolicitudMaterialEstado status) => status switch
    {
        SolicitudMaterialEstado.Pendiente => "badge-warning",
        SolicitudMaterialEstado.AprobadaTotal => "badge-success",
        SolicitudMaterialEstado.AprobadaParcial => "badge-info",
        SolicitudMaterialEstado.Rechazada => "badge-danger",
        _ => "badge-info"
    };

    public static string BadgeClass(DetalleSolicitudEstado status) => status switch
    {
        DetalleSolicitudEstado.Pendiente => "badge-warning",
        DetalleSolicitudEstado.Aprobado => "badge-success",
        DetalleSolicitudEstado.AprobadoParcial => "badge-info",
        DetalleSolicitudEstado.Rechazado => "badge-danger",
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
        OrderMaterialsStatus.NoAplica => "Sin materiales",
        OrderMaterialsStatus.PendienteRevisionBodega => "Pendiente revisión bodega",
        OrderMaterialsStatus.MaterialesValidados => "Materiales validados",
        OrderMaterialsStatus.EntregaParcial => "Entrega parcial",
        OrderMaterialsStatus.ListaParaProduccion => "Lista para producción",
        MaterialStockAvailability.Suficiente => "Stock suficiente",
        MaterialStockAvailability.Insuficiente => "Stock insuficiente",
        MaterialStockAvailability.SinExistencias => "Sin existencias",
        ProductionStageStatus.Pendiente => "Pendiente",
        ProductionStageStatus.EnProceso => "En proceso",
        ProductionStageStatus.Pausado => "Pausado",
        ProductionStageStatus.Finalizado => "Finalizado",
        RequestStatus.Pendiente => "Pendiente",
        RequestStatus.Aprobada => "Aprobada",
        RequestStatus.Rechazada => "Rechazada",
        SolicitudMaterialEstado.Pendiente => "Pendiente",
        SolicitudMaterialEstado.AprobadaTotal => "Aprobada total",
        SolicitudMaterialEstado.AprobadaParcial => "Aprobada parcial",
        SolicitudMaterialEstado.Rechazada => "Rechazada",
        DetalleSolicitudEstado.Pendiente => "Pendiente",
        DetalleSolicitudEstado.Aprobado => "Aprobado",
        DetalleSolicitudEstado.AprobadoParcial => "Aprobado parcial",
        DetalleSolicitudEstado.Rechazado => "Rechazado",
        MaterialStatus.Bueno => "Bueno",
        MaterialStatus.Regular => "Regular",
        MaterialStatus.Deteriorado => "Deteriorado",
        QualityResult.Aprobada => "Aprobada",
        QualityResult.Reproceso => "Reproceso",
        QualityResult.Rechazada => "Rechazada",
        _ => value.ToString()
    };
}
