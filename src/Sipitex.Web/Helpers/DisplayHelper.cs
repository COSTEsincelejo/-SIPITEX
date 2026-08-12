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

    public static string BadgeClass(StockMovementType type) => type switch
    {
        StockMovementType.Entrada => "badge-success",
        StockMovementType.Salida => "badge-warning",
        StockMovementType.Ajuste => "badge-info",
        StockMovementType.AprobacionSolicitud => "badge-warning",
        _ => "badge-info"
    };

    public static string Label(StockMovementType type) => type switch
    {
        StockMovementType.Entrada => "Entrada",
        StockMovementType.Salida => "Salida",
        StockMovementType.Ajuste => "Ajuste",
        StockMovementType.AprobacionSolicitud => "Aprobación solicitud",
        _ => type.ToString()
    };

    public static string Label(StockEntryOrigin? origin) => origin switch
    {
        StockEntryOrigin.Compra => "Compra",
        StockEntryOrigin.Devolucion => "Devolución",
        StockEntryOrigin.OtraFuenteAutorizada => "Otra fuente autorizada",
        null => "—",
        _ => origin.Value.ToString()
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

    // Íconos Font Awesome para reconocer estado sin leer el texto
    public static string StatusIcon(OrderStatus status) => status switch
    {
        OrderStatus.Pendiente => "fa-hourglass-half",
        OrderStatus.EnProceso => "fa-play",
        OrderStatus.Finalizada => "fa-circle-check",
        OrderStatus.Cancelada => "fa-ban",
        _ => "fa-circle"
    };

    public static string StatusIcon(SolicitudMaterialEstado status) => status switch
    {
        SolicitudMaterialEstado.Pendiente => "fa-hourglass-half",
        SolicitudMaterialEstado.AprobadaTotal => "fa-circle-check",
        SolicitudMaterialEstado.AprobadaParcial => "fa-circle-half-stroke",
        SolicitudMaterialEstado.Rechazada => "fa-circle-xmark",
        _ => "fa-circle"
    };

    public static string StatusIcon(RequestStatus status) => status switch
    {
        RequestStatus.Pendiente => "fa-hourglass-half",
        RequestStatus.Aprobada => "fa-circle-check",
        RequestStatus.Rechazada => "fa-circle-xmark",
        _ => "fa-circle"
    };

    public static string StatusIcon(DetalleSolicitudEstado status) => status switch
    {
        DetalleSolicitudEstado.Pendiente => "fa-hourglass-half",
        DetalleSolicitudEstado.Aprobado => "fa-circle-check",
        DetalleSolicitudEstado.AprobadoParcial => "fa-circle-half-stroke",
        DetalleSolicitudEstado.Rechazado => "fa-circle-xmark",
        _ => "fa-circle"
    };

    public static string StatusIcon(OrderMaterialsStatus status) => status switch
    {
        OrderMaterialsStatus.ListaParaProduccion => "fa-circle-check",
        OrderMaterialsStatus.MaterialesValidados => "fa-clipboard-check",
        OrderMaterialsStatus.EntregaParcial => "fa-boxes-stacked",
        OrderMaterialsStatus.PendienteRevisionBodega => "fa-warehouse",
        OrderMaterialsStatus.NoAplica => "fa-minus",
        _ => "fa-circle"
    };

    // Niveles de stock desde backend (StockLevel); no recalcular Stock<=0 aquí
    public static string StockLevelClass(StockLevel level) => level switch
    {
        StockLevel.Critico => "stock-critical",
        StockLevel.Bajo => "stock-low",
        _ => "stock-ok"
    };

    public static string StockLevelLabel(StockLevel level) => level switch
    {
        StockLevel.Critico => "Crítico",
        StockLevel.Bajo => "Bajo",
        _ => "OK"
    };

    public static string StockLevelIcon(StockLevel level) => level switch
    {
        StockLevel.Critico => "fa-circle-exclamation",
        StockLevel.Bajo => "fa-triangle-exclamation",
        _ => "fa-circle-check"
    };

    public static string StockLevelBadgeClass(StockLevel level) => level switch
    {
        StockLevel.Critico => "badge-danger",
        StockLevel.Bajo => "badge-warning",
        _ => "badge-success"
    };

    public static string StatusIcon(StockLevel level) => StockLevelIcon(level);

    public static string BadgeClass(StockLevel level) => StockLevelBadgeClass(level);

    public static string StatusText(StockLevel level) => StockLevelLabel(level);
}
