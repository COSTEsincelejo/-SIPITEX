using Sipitex.Application.DTOs;
using Sipitex.Domain.Enums;

namespace Sipitex.Web.Models;

// Aquí están los modelos usados por las vistas para mostrar datos al usuario.
// Cada pantalla suele tener un *IndexViewModel con listas + formularios de creación.

// Pantalla principal de inventario: materiales, solicitudes y formularios
public class InventarioIndexViewModel
{
    public IReadOnlyList<MaterialDto> Materials { get; set; } = [];
    public IReadOnlyList<MaterialRequestDto> Requests { get; set; } = [];
    public IReadOnlyList<ProductionOrderDto> Orders { get; set; } = []; // para el select de solicitudes
    public CreateMaterialForm CreateMaterial { get; set; } = new();
    public CreateRequestForm CreateRequest { get; set; } = new();
    public string? Message { get; set; } // mensaje flash después de una acción
    public bool IsSuccess { get; set; } // true = toast verde, false = rojo
}

// Formulario para agregar un material nuevo
public class CreateMaterialForm
{
    public string Name { get; set; } = string.Empty;
    public decimal Stock { get; set; }
    public MaterialUnit Unit { get; set; } = MaterialUnit.Metros;
}

// Formulario para pedir material a bodega
public class CreateRequestForm
{
    public int ProductionOrderId { get; set; }
    public int MaterialId { get; set; }
    public decimal Quantity { get; set; }
}

// Ajuste manual de stock (bodeguero/admin)
public class AdjustStockForm
{
    public int MaterialId { get; set; }
    public decimal NewStock { get; set; }
}

// Pantalla de órdenes de producción
public class OrdenesIndexViewModel
{
    public IReadOnlyList<ProductionOrderDto> Orders { get; set; } = [];
    public IReadOnlyList<string> ProductNames { get; set; } = [];
    public CreateOrderForm CreateOrder { get; set; } = new();
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

// Crear orden nueva (dispara MRP automático)
public class CreateOrderForm
{
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public DateOnly Deadline { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}

// Pantalla MRP: fichas técnicas + BOM + simulación
public class MrpIndexViewModel
{
    public IReadOnlyList<BomItemDto> Bom { get; set; } = [];
    public IReadOnlyList<BomProductListItemDto> Products { get; set; } = [];
    public IReadOnlyList<string> ProductNames { get; set; } = [];
    public MrpSimulationForm Simulation { get; set; } = new();
    public MrpSimulationResultDto? Result { get; set; }
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

// Datos del formulario de simulación MRP
public class MrpSimulationForm
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 50;
}

// Crear / editar ficha técnica
public class BomProductEditViewModel
{
    public BomProductEditForm Form { get; set; } = new();
    public IReadOnlyList<MaterialDto> Materials { get; set; } = [];
    public bool IsEdit { get; set; }
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

public class BomProductEditForm
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public bool IsReference { get; set; }
    public string? Notes { get; set; }
    public bool HabilitadoParaOrdenes { get; set; } = true;
    public List<BomRecipeLineForm> Lines { get; set; } = [new()];
}

public class BomRecipeLineForm
{
    public int? ItemId { get; set; }
    public int MaterialId { get; set; }
    public string? NewMaterialName { get; set; }
    public MaterialUnit? NewMaterialUnit { get; set; }
    public decimal QuantityPerUnit { get; set; }
    public MaterialUnit Unit { get; set; } = MaterialUnit.Metros;
}

// Pantalla de fichas y registro de producción diaria
public class FichasIndexViewModel
{
    public IReadOnlyList<FichaDto> Fichas { get; set; } = [];
    public IReadOnlyList<ProductionOrderDto> Orders { get; set; } = [];
    public IReadOnlyList<InstructorOptionDto> Instructors { get; set; } = [];
    public IReadOnlyList<ProductionSessionDto> Sessions { get; set; } = [];
    public IReadOnlyList<MaterialDto> Materials { get; set; } = []; // dropdown solicitud materiales
    public CreateFichaForm CreateFicha { get; set; } = new();
    public RegisterProductionForm Register { get; set; } = new();
    public bool IsAdministrator { get; set; } // cambia textos y alcance de datos
    // Filtros de la tabla de fichas (query string del GET)
    public string? FichaCodeFilter { get; set; }
    public string? InstructorFilter { get; set; }
    public string? TurnoFilter { get; set; }
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

// Alta de una ficha de aprendices
public class CreateFichaForm
{
    public string FichaCode { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    // IDs de usuarios con rol Instructor (multi-select)
    public List<int> InstructorUserIds { get; set; } = [];
    public string Turno { get; set; } = "Mañana";
    public int? ProductionOrderId { get; set; } // opcional (orden existente)
    public string? AssignedOrderText { get; set; } // opcional (texto manual)
}

// Formulario multi-ítem para crear SolicitudMaterial desde Fichas
public class CreateSolicitudMaterialForm
{
    public int FichaId { get; set; }
    public string? Observaciones { get; set; }
    public List<CreateDetalleSolicitudForm> Detalles { get; set; } = [new()];
}

public class CreateDetalleSolicitudForm
{
    public int MaterialId { get; set; }
    public decimal CantidadSolicitada { get; set; }
}

// Listado "Mis solicitudes"
public class SolicitudesMaterialIndexViewModel
{
    public IReadOnlyList<SolicitudMaterialListItemDto> Solicitudes { get; set; } = [];
    public bool IsAdministrator { get; set; }
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

// Detalle de una SolicitudMaterial
public class SolicitudMaterialDetailViewModel
{
    public SolicitudMaterialDetailDto Solicitud { get; set; } = null!;
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

// Listado Bodeguero: solicitudes de materiales
public class BodegaSolicitudesIndexViewModel
{
    public IReadOnlyList<SolicitudMaterialListItemDto> Solicitudes { get; set; } = [];
    public bool SoloPendientes { get; set; } = true;
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

// Detalle / resolución Bodeguero
public class BodegaSolicitudDetailViewModel
{
    public SolicitudMaterialResolucionDto Solicitud { get; set; } = null!;
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

public class ResolveSolicitudForm
{
    public int SolicitudId { get; set; }
    public string? Observaciones { get; set; }
    public List<ResolveDetalleFormItem> Items { get; set; } = [];
}

public class ResolveDetalleFormItem
{
    public int DetalleId { get; set; }
    public decimal CantidadAprobada { get; set; }
}

// Registro formal de sesión de producción
public class RegisterProductionForm
{
    public int ProductionOrderId { get; set; }
    public int FichaId { get; set; }
    public int Units { get; set; }
    public string? Observations { get; set; }
}

// Pantalla de control de calidad
public class CalidadIndexViewModel
{
    public IReadOnlyList<QualityRecordDto> Records { get; set; } = [];
    public IReadOnlyList<ProductionOrderDto> Orders { get; set; } = [];
    public CreateQualityForm Create { get; set; } = new();
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

// Nueva inspección de calidad
public class CreateQualityForm
{
    public int ProductionOrderId { get; set; }
    public int Units { get; set; }
    public QualityResult Result { get; set; } = QualityResult.Aprobada;
    public string? MotivoReproceso { get; set; } // solo si es reproceso
    public string? Responsable { get; set; }
}

// Dashboard de KPIs (estadísticas)
public class EstadisticasIndexViewModel
{
    public DashboardKpiDto Dashboard { get; set; } = new(0, 0, 0, 0, []);
}

// Preferencias de alertas por correo
public class AlertasIndexViewModel
{
    public IReadOnlyList<AlertPreferenceDto> Preferences { get; set; } = [];
    public IReadOnlyList<AlertDeliveryDto> Deliveries { get; set; } = [];
    public bool SmtpConfigured { get; set; } // cambia el mensaje informativo
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

// Checkboxes de tipos de alerta habilitados
public class AlertPreferencesForm
{
    public List<string> EnabledTypes { get; set; } = [];
}

// Componente reutilizable cuando una lista viene vacía
public class EmptyStateModel
{
    public string Icon { get; set; } = "fa-inbox"; // clase Font Awesome
    public string Title { get; set; } = "Sin registros";
    public string Text { get; set; } = "Todavía no hay información para mostrar.";
    public string? ActionText { get; set; } // texto del botón o hint
    public string? ActionHref { get; set; } // link del botón (opcional)
}

// Pantalla de reportes con filtros opcionales
public class ReportesIndexViewModel
{
    public IReadOnlyList<InstructorOptionDto> Instructors { get; set; } = [];
    public IReadOnlyList<FichaDto> Fichas { get; set; } = [];
}
