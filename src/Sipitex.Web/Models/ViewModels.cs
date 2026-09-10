using Sipitex.Application.DTOs;
using Sipitex.Domain.Entities;
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

// Historial de movimientos de inventario (Admin / EncargadoDeBodega)
public class InventarioMovimientosViewModel
{
    public IReadOnlyList<StockMovementDto> Movimientos { get; set; } = [];
    public IReadOnlyList<MaterialDto> Materials { get; set; } = [];
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public int? MaterialId { get; set; }
}

// Formulario para agregar un material nuevo
public class CreateMaterialForm
{
    public string Name { get; set; } = string.Empty;
    public decimal Stock { get; set; }
    public MaterialUnit Unit { get; set; } = MaterialUnit.Metros;
    public StockEntryOrigin Origen { get; set; } = StockEntryOrigin.Compra;
    public decimal CostoAdquisicion { get; set; }
}

// Formulario para pedir material a plantaInventario
public class CreateRequestForm
{
    public int ProductionOrderId { get; set; }
    public int MaterialId { get; set; }
    public decimal Quantity { get; set; }
}

// Ajuste manual de stock (encargadoDeBodega/admin); Origen requerido si sube el stock
public class AdjustStockForm
{
    public int MaterialId { get; set; }
    public decimal NewStock { get; set; }
    public StockEntryOrigin? Origen { get; set; }
}

// Edición de metadatos de material (solo Administrador)
public class EditMaterialForm
{
    public int MaterialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public MaterialUnit Unit { get; set; }
    public decimal MinStock { get; set; }
    public decimal CostoAdquisicion { get; set; }
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
    public string? ClientName { get; set; }
    public DateOnly Deadline { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}

// Pantalla MRP: ficha técnica + BOM + simulación
public class MrpIndexViewModel
{
    public IReadOnlyList<BomItemDto> Bom { get; set; } = [];
    public IReadOnlyList<BomProductListItemDto> Products { get; set; } = [];
    public IReadOnlyList<string> ProductNames { get; set; } = [];
    public IReadOnlyList<InstructorOptionDto> Instructors { get; set; } = [];
    public bool IsAdministrator { get; set; }
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

    // Fase A — metadatos base (opcionales)
    public string? Referencia { get; set; }
    public string? Linea { get; set; }
    public string? TallaInicial { get; set; }
    public string? TipoEmpaque { get; set; }
    public string? DescripcionPrenda { get; set; }
    public DateOnly? FechaSolicitud { get; set; }
    public DateOnly? FechaElaboracion { get; set; }
    public int? AnioMuestrario { get; set; }
    public bool EsDisenoNuevo { get; set; }
    public bool EsReplica { get; set; }
    public bool EsBancoDeMuestras { get; set; }
    public string? Disenador { get; set; }
    public string? Patronista { get; set; }
    public string? Digitacion { get; set; }
    public List<BomProductTallaForm> Tallas { get; set; } = [];

    // Fase B — piezas y medidas
    public List<BomProductPiezaForm> Piezas { get; set; } = [];
    public List<BomProductMedidaForm> MedidasPatron { get; set; } = [];
    public List<BomProductMedidaForm> MedidasPrenda { get; set; } = [];

    public List<BomRecipeLineForm> Lines { get; set; } = [new()];
}

public class BomProductTallaForm
{
    public int? Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }
}

public class BomProductPiezaForm
{
    public int? Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; } = 1;
    public string Tela { get; set; } = string.Empty;
    public int Orden { get; set; }
}

public class BomProductMedidaForm
{
    public int? Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? Tolerancia { get; set; }
    public string? ComoMedir { get; set; }
    public int Orden { get; set; }
    public List<BomProductMedidaValorForm> Valores { get; set; } = [];
}

public class BomProductMedidaValorForm
{
    public int? TallaId { get; set; }
    public int TallaOrden { get; set; }
    public string? TallaNombre { get; set; }
    public decimal? Valor { get; set; }
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
    public string? NumeroGrupoFilter { get; set; }
    public string? InstructorFilter { get; set; }
    public string? TurnoFilter { get; set; }
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

// Alta de una ficha de aprendices
public class CreateFichaForm
{
    public string NumeroGrupo { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    // IDs de usuarios con rol Instructor (multi-select)
    public List<int> InstructorUserIds { get; set; } = [];
    public string Turno { get; set; } = "Mañana";
    public int? ProductionOrderId { get; set; } // opcional (orden existente)
    public string? AssignedOrderText { get; set; } // opcional (texto manual)
}

// Formulario multi-ítem para crear SolicitudMaterial desde Fichas (PorFicha)
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

// Formulario InsumosLibres (descripción por ítem)
public class CreateInsumosLibresForm
{
    public string? DescripcionLibre { get; set; }
    public int? FichaId { get; set; }
    public int? ProductionOrderId { get; set; }
    // Destino de insumos libres; si no se envía, el servicio usa PlantaInventario 1.
    public int? PlantaInventarioId { get; set; } = 1;
    public string? Observaciones { get; set; }
    public List<CreateInsumoLibreItemForm> Detalles { get; set; } = [new()];
}

public class CreateInsumoLibreItemForm
{
    public string DescripcionItem { get; set; } = string.Empty;
    public decimal CantidadSolicitada { get; set; } = 1;
}

public class SolicitarInsumosViewModel
{
    public CreateInsumosLibresForm Form { get; set; } = new();
    public IReadOnlyList<(int Id, string Label)> Fichas { get; set; } = [];
    public IReadOnlyList<(int Id, string Label)> Ordenes { get; set; } = [];
    // Catálogo seed AddPlantasInventario (PlantaInventario 1 / PlantaInventario 2); el form pasa Form.PlantaInventarioId al DTO.
    public IReadOnlyList<(int Id, string Label)> PlantasInventario { get; set; } =
    [
        (1, "PlantaInventario 1"),
        (2, "PlantaInventario 2")
    ];
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
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

// Listado EncargadoDeBodega: solicitudes de materiales
public class PlantasInventarioSolicitudesIndexViewModel
{
    public IReadOnlyList<SolicitudMaterialListItemDto> Solicitudes { get; set; } = [];
    public bool SoloPendientes { get; set; } = true;
    public bool CanResolver { get; set; } = true;
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

// Detalle / resolución EncargadoDeBodega
public class PlantaInventarioSolicitudDetailViewModel
{
    public SolicitudMaterialResolucionDto Solicitud { get; set; } = null!;
    public IReadOnlyList<MaterialDto> Materials { get; set; } = [];
    public bool CanResolver { get; set; } = true;
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
    public int? MaterialId { get; set; }
    public string? NewMaterialName { get; set; }
    public MaterialUnit? NewMaterialUnit { get; set; }
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
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public QualityResult Result { get; set; } = QualityResult.Aprobada;
    public CalidadClasificacion Clasificacion { get; set; } = CalidadClasificacion.Bueno;
    public string? MotivoReproceso { get; set; } // solo si es reproceso
    public string? Responsable { get; set; }
}

// Dashboard de KPIs (estadísticas)
public class EstadisticasIndexViewModel
{
    public DashboardKpiDto Dashboard { get; set; } = new(0, 0, 0, 0, 0, []);
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

// Catálogo de plantasInventario (admin): listado + alta
public class PlantasInventarioIndexViewModel
{
    public IReadOnlyList<PlantaInventario> PlantasInventario { get; set; } = [];
    public CreatePlantaInventarioForm Form { get; set; } = new();
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

public class CreatePlantaInventarioForm
{
    public string Nombre { get; set; } = string.Empty;
}

public class EditPlantaInventarioViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

public class DeletePlantaInventarioViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public int Materiales { get; set; }
    public int Solicitudes { get; set; }
    public int Encargados { get; set; }
    public decimal StockTotal { get; set; }
    public bool RequiresReassignment => Materiales > 0 || Solicitudes > 0 || Encargados > 0;
    public IReadOnlyList<PlantaInventario> Destinos { get; set; } = [];
    public int DestinoId { get; set; }
    public string? Message { get; set; }
}

// Pantalla de reportes con filtros opcionales
public class ReportesIndexViewModel
{
    public IReadOnlyList<InstructorOptionDto> Instructors { get; set; } = [];
    public IReadOnlyList<FichaDto> Fichas { get; set; } = [];
    // Gap #11: Instructor no elige otro instructor ni ve Inventario global
    public bool IsInstructorScoped { get; set; }
    public int? ForcedInstructorId { get; set; }
}

// Detalle de materiales asociados a una orden (legacy VM; Detail usa OrdenMesDetailViewModel)
public class OrdenMaterialDetailViewModel
{
    public OrderMaterialsDetailDto Detail { get; set; } = null!;
    public IReadOnlyList<MaterialDto> Materials { get; set; } = [];
    public AddOrderMaterialForm AddMaterial { get; set; } = new();
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

// Detalle MES completo de una orden
public class OrdenMesDetailViewModel
{
    public OrderMesDetailDto Mes { get; set; } = null!;
    public IReadOnlyList<MaterialDto> Materials { get; set; } = [];
    public IReadOnlyList<Sipitex.Domain.Entities.User> Instructors { get; set; } = [];
    public AddOrderMaterialForm AddMaterial { get; set; } = new();
    public IReadOnlyList<OrderChangeLogDto> ChangeLogs { get; set; } = [];
    public bool CanManageMaterials { get; set; }
    public bool CanOperateProduction { get; set; }
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

public class OrdenEditViewModel
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public IReadOnlyList<string> ProductNames { get; set; } = [];
    public EditOrderForm Form { get; set; } = new();
    public IReadOnlyList<OrderChangeLogDto> ChangeLogs { get; set; } = [];
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

public class EditOrderForm
{
    public int OrderId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public string? ClientName { get; set; }
    public DateOnly Deadline { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}

public class AddOrderMaterialForm
{
    public int OrderId { get; set; }
    public int MaterialId { get; set; }
    public decimal QuantityRequired { get; set; } = 1;
    public string? Observations { get; set; }
}

public class PlantasInventarioOrdenesIndexViewModel
{
    public IReadOnlyList<ProductionOrderDto> Orders { get; set; } = [];
    public bool CanEntregar { get; set; } = true;
    public bool CanReingresar { get; set; } = true;
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

public class PlantaInventarioOrdenDetailViewModel
{
    public OrderMaterialsDetailDto Detail { get; set; } = null!;
    public bool CanEntregar { get; set; } = true;
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

public class PlantaInventarioReingresoViewModel
{
    public IReadOnlyList<ProductionOrderDto> Orders { get; set; } = [];
    public IReadOnlyList<MaterialDto> Materials { get; set; } = [];
    public IReadOnlyList<OrderStageDto> Stages { get; set; } = [];
    public IReadOnlyList<string> StageNames { get; set; } = [];
    public PlantaInventarioReingresoForm Form { get; set; } = new();
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

public class PlantaInventarioReingresoForm
{
    public int OrderId { get; set; }
    public int StageId { get; set; }
    public int Quantity { get; set; } = 1;
    public int MaterialId { get; set; }
    public bool EsProductoTerminado { get; set; }
    public string? Observations { get; set; }
}

public class DeliverOrderMaterialsForm
{
    public int OrderId { get; set; }
    public string? Observations { get; set; }
    public List<DeliverOrderMaterialItemForm> Items { get; set; } = [];
}

public class DeliverOrderMaterialItemForm
{
    public int LineId { get; set; }
    public decimal QuantityToDeliver { get; set; }
}

// Bitácora global (solo Administrador)
public class AuditoriaIndexViewModel
{
    public IReadOnlyList<ActivityLogDto> Entries { get; set; } = [];
    public IReadOnlyList<string> Actions { get; set; } = [];
    public IReadOnlyList<string> Entities { get; set; } = [];
    public IReadOnlyList<ActivityLogActorDto> Actors { get; set; } = [];
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public string? Action { get; set; }
    public string? Entity { get; set; }
    public int? UserId { get; set; }
}

public class FichaTecnicaMaterialsViewModel
{
    public FichaTecnicaMaterialsDto? Ficha { get; set; }
    public string? Codigo { get; set; }
    public string? Message { get; set; }
}

public class ConsumosIndexViewModel
{
    public IReadOnlyList<ProductionOrderDto> Orders { get; set; } = [];
    public IReadOnlyList<MaterialDto> Materials { get; set; } = [];
    public IReadOnlyList<ConsumoMaterialDto> Consumos { get; set; } = [];
    public ConsumoCostoResumenDto? Costo { get; set; }
    public RegisterConsumoForm Form { get; set; } = new();
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

public class RegisterConsumoForm
{
    public int ProductionOrderId { get; set; }
    public int MaterialId { get; set; }
    public decimal Cantidad { get; set; }
    public DateTime? FechaLocal { get; set; }
}

public class GruposConfeccionIndexViewModel
{
    public IReadOnlyList<GrupoConfeccionDto> Grupos { get; set; } = [];
    public IReadOnlyList<ProductionOrderDto> Orders { get; set; } = [];
    public IReadOnlyList<InstructorOptionDto> Instructors { get; set; } = [];
    public CreateGrupoConfeccionForm Form { get; set; } = new();
    public GrupoConsumoCruzadoDto? Cruzado { get; set; }
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

public class CreateGrupoConfeccionForm
{
    public int ProductionOrderId { get; set; }
    public int InstructorUserId { get; set; }
    public DateOnly FechaRealizacion { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public TimeOnly HoraInicio { get; set; } = new(8, 0);
    public TimeOnly? HoraFin { get; set; }
    public int CantidadPrendas { get; set; }
}

public class CostosIndexViewModel
{
    public IReadOnlyList<ProductionOrderDto> Orders { get; set; } = [];
    public int? OrderId { get; set; }
    public GarmentCostDto? Costo { get; set; }
}

public class ActasIndexViewModel
{
    public IReadOnlyList<ActaMovimientoDto> Actas { get; set; } = [];
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

public class ActaCreateViewModel
{
    public IReadOnlyList<ProductionOrderDto> Orders { get; set; } = [];
    public IReadOnlyList<StockMovementDto> Movimientos { get; set; } = [];
    public IReadOnlyList<ConsumoMaterialDto> Consumos { get; set; } = [];
    public CreateActaForm Form { get; set; } = new();
}

public class CreateActaForm
{
    public ActaTipo Tipo { get; set; } = ActaTipo.Egreso;
    public ActaOrigen Origen { get; set; } = ActaOrigen.Manual;
    public string EntregaNombre { get; set; } = string.Empty;
    public string EntregaCargo { get; set; } = string.Empty;
    public bool EntregaConforme { get; set; } = true;
    public string RecibeNombre { get; set; } = string.Empty;
    public string RecibeCargo { get; set; } = string.Empty;
    public bool RecibeConforme { get; set; } = true;
    public string? Observaciones { get; set; }
    public int? ProductionOrderId { get; set; }
    public EstadoProducto EstadoOrigen { get; set; } = EstadoProducto.MateriaPrima;
    public EstadoProducto EstadoDestino { get; set; } = EstadoProducto.Corte;
    public int[] StockMovementIds { get; set; } = [];
    public int[] ConsumoIds { get; set; } = [];
}
