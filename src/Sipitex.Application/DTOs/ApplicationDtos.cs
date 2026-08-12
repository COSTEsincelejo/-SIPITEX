using Sipitex.Domain.Enums;

namespace Sipitex.Application.DTOs;

// --- Inventario ---

// Material tal como se muestra en la tabla de inventario
public record MaterialDto(
    int Id,
    string Name,
    string UnitDisplay, // unidad legible (m, kg, ud)
    MaterialUnit Unit,
    decimal Stock,
    MaterialStatus Status,
    decimal MinStock,
    bool IsLowStock, // true si hay que alertar
    DateOnly LastEntryDate);

// Datos para crear material nuevo (origen tipifica la Entrada del ledger)
public record CreateMaterialDto(string Name, decimal Stock, MaterialUnit Unit, StockEntryOrigin Origen);

// Ajuste manual de stock; Origen obligatorio cuando NewStock > stock actual
public record AdjustStockDto(int MaterialId, decimal NewStock, StockEntryOrigin? Origen = null);

// Edición de metadatos del material (nombre, unidad, mínimo) — no toca stock
public record UpdateMaterialDto(int MaterialId, string Name, MaterialUnit Unit, decimal MinStock);

// Cambiar estado físico del material
public record UpdateMaterialStatusDto(int MaterialId, MaterialStatus Status);

// Solicitud de material hacia bodega (vista lista)
public record MaterialRequestDto(
    int Id,
    string MaterialName,
    decimal Quantity,
    string OrderNumber,
    RequestStatus Status);

// Crear solicitud: orden + material + cantidad
public record CreateMaterialRequestDto(int ProductionOrderId, int MaterialId, decimal Quantity);

// Movimiento del ledger de inventario (consulta de historial)
public record StockMovementDto(
    int Id,
    DateTime FechaUtc,
    string UsuarioNombre,
    int UsuarioId,
    StockMovementType TipoMovimiento,
    StockEntryOrigin? Origen,
    int MaterialId,
    string MaterialName,
    decimal Cantidad,
    decimal StockResultante,
    string? Referencia);

// --- Producción ---

// Orden de producción con avance y hint del BOM
public record ProductionOrderDto(
    int Id,
    string OrderNumber,
    string ProductName,
    int TotalQuantity,
    int ProducedQuantity,
    int ProgressPercent,
    OrderStatus Status,
    DateOnly Deadline,
    string MrpHint,
    OrderMaterialsStatus MaterialsStatus = OrderMaterialsStatus.NoAplica,
    bool HasMaterialRequirements = false,
    bool CanRegisterProduction = true,
    string? ClientName = null,
    string? CurrentStageName = null,
    int FlowProgressPercent = 0,
    int CombinedProgressPercent = 0);

// Alta de orden nueva
public record CreateProductionOrderDto(
    string ProductName,
    int TotalQuantity,
    DateOnly Deadline,
    string? ClientName = null,
    // Si lo crea un Instructor, se asigna como responsable en etapas MES (visible vía scope gap #12/#7)
    int? ResponsibleInstructorUserId = null);

// Edición de los mismos campos de Create (gap #2)
public record UpdateProductionOrderDto(
    int OrderId,
    string ProductName,
    int TotalQuantity,
    DateOnly Deadline,
    string? ClientName = null);

public record OrderChangeLogDto(
    int Id,
    DateTime FechaUtc,
    string UsuarioNombre,
    int UsuarioId,
    string Campo,
    string? ValorAnterior,
    string? ValorNuevo);

// --- MRP / BOM ---

// Una línea del listado BOM
public record BomItemDto(string ProductName, string MaterialName, decimal QuantityPerUnit, string UnitDisplay);

// Fila del listado de fichas técnicas
public record BomProductListItemDto(
    int Id,
    string ProductName,
    int MaterialCount,
    bool IsReference,
    bool HabilitadoParaOrdenes,
    string? Notes,
    IReadOnlyList<BomProductInstructorDto>? Instructors = null);

// Instructor asignado a una ficha técnica (BOM)
public record BomProductInstructorDto(int UserId, string Nombre);

// Línea editable de la receta
public record BomRecipeLineDto(
    int? ItemId,
    int? MaterialId,
    string? NewMaterialName,
    MaterialUnit? NewMaterialUnit,
    decimal QuantityPerUnit,
    MaterialUnit Unit);

// Alta / edición de ficha técnica
public record UpsertBomProductDto(
    string ProductName,
    bool IsReference,
    string? Notes,
    bool HabilitadoParaOrdenes,
    IReadOnlyList<BomRecipeLineDto> Lines);

// Detalle para la pantalla de edición
public record BomProductDetailDto(
    int Id,
    string ProductName,
    bool IsReference,
    string? Notes,
    bool HabilitadoParaOrdenes,
    IReadOnlyList<BomRecipeLineDetailDto> Lines);

public record BomRecipeLineDetailDto(
    int ItemId,
    int MaterialId,
    string MaterialName,
    decimal QuantityPerUnit,
    MaterialUnit Unit,
    string UnitDisplay);

// Resultado completo de simular MRP
public record MrpSimulationResultDto(
    string ProductName,
    decimal Quantity,
    IReadOnlyList<MrpLineDto> Lines);

// Una línea de la simulación (requerido vs disponible)
public record MrpLineDto(
    string MaterialName,
    decimal Required,
    decimal Available,
    decimal Deficit,
    string UnitDisplay,
    bool IsOk); // true si no hay déficit

// --- Fichas y sesiones ---

// Instructor asignado a una ficha (para chips / quitar / editar proceso)
public record FichaInstructorDto(int UserId, string Nombre, string? Proceso = null);

// Opción de instructor activo para selects
public record InstructorOptionDto(int Id, string Nombre);

// Ficha de formación / grupo de producción
public record FichaDto(
    int Id,
    string FichaCode,
    string ProcessName,
    string InstructorName,
    string? AssignedOrderNumber,
    int? InstructorUserId = null,
    string Turno = "",
    IReadOnlyList<FichaInstructorDto>? Instructors = null);

// Datos para crear ficha (instructores = IDs de usuarios con rol Instructor)
public record CreateFichaDto(
    string FichaCode,
    string ProcessName,
    IReadOnlyList<int> InstructorUserIds,
    string Turno,
    int? ProductionOrderId = null,
    string? AssignedOrderText = null);

// Sesión diaria registrada por el instructor
public record ProductionSessionDto(
    int Id,
    string FichaCode,
    string OrderNumber,
    int Units,
    string Observations,
    DateTime SessionDate,
    string InstructorName,
    int? RegisteredByUserId = null,
    string Turno = "");

// Registrar producción desde formulario
public record RegisterProductionDto(int ProductionOrderId, int FichaId, int Units, string? Observations = null);

// --- Calidad ---

// Inspección ya guardada (vista lista)
public record QualityRecordDto(
    string OrderNumber,
    int Units,
    QualityResult Result,
    DateOnly Date,
    string? MotivoReproceso,
    string? Responsable);

// Crear inspección nueva
public record CreateQualityRecordDto(
    int ProductionOrderId,
    int Units,
    QualityResult Result,
    string? MotivoReproceso = null,
    string? Responsable = null);

// --- Dashboard ---

// KPIs del home
public record DashboardKpiDto(
    int TotalProduced,
    decimal QualityRate,
    int ActiveOrders,
    int LowStockCount,
    IReadOnlyList<ChartBarDto> ChartData);

// Una barra del gráfico de órdenes
public record ChartBarDto(string Label, int Produced, int Target);

// --- Requisitos del proyecto (matriz RF/RNF) ---

// Resumen de cumplimiento (cuántos cumplen, parcial, ausente)
public record RequirementSummaryDto(int Cumple, int Parcial, int Ausente);

// Requisito funcional individual
public record FunctionalRequirementDto(
    string Code,
    string Description,
    string Module,
    ComplianceStatus Status,
    string Observation);

// Requisito no funcional individual
public record NonFunctionalRequirementDto(
    string Code,
    string Description,
    ComplianceStatus Status,
    string Observation);

// Vista completa de la matriz RF/RNF
public record RequirementsViewDto(
    RequirementSummaryDto FunctionalSummary,
    RequirementSummaryDto NonFunctionalSummary,
    IReadOnlyList<FunctionalRequirementDto> Functional,
    IReadOnlyList<NonFunctionalRequirementDto> NonFunctional);

// --- SolicitudMaterial (flujo Ficha multi-ítem; paralelo a MaterialRequest) ---

// Ítem al crear una solicitud
public record CreateDetalleSolicitudDto(int MaterialId, decimal CantidadSolicitada);

// Alta de solicitud ligada a Ficha
public record CreateSolicitudMaterialDto(
    int FichaId,
    IReadOnlyList<CreateDetalleSolicitudDto> Detalles,
    string? Observaciones = null);

// Fila del listado "Mis solicitudes"
public record SolicitudMaterialListItemDto(
    int Id,
    string Codigo,
    string FichaCode,
    SolicitudMaterialEstado Estado,
    DateTime FechaSolicitud,
    string SolicitanteNombre);

// Ítem en el detalle de una solicitud
public record DetalleSolicitudMaterialDto(
    int Id,
    string MaterialName,
    string UnitDisplay,
    decimal CantidadSolicitada,
    decimal? CantidadAprobada,
    DetalleSolicitudEstado EstadoItem);

// Detalle completo (cabecera + líneas)
public record SolicitudMaterialDetailDto(
    int Id,
    string Codigo,
    string FichaCode,
    string SolicitanteNombre,
    SolicitudMaterialEstado Estado,
    DateTime FechaSolicitud,
    DateTime? FechaResolucion,
    string? Observaciones,
    IReadOnlyList<DetalleSolicitudMaterialDto> Detalles);

// Ítem para resolución en bodega (incluye stock actual)
public record DetalleResolucionDto(
    int Id,
    string MaterialName,
    string UnitDisplay,
    decimal CantidadSolicitada,
    decimal StockDisponible,
    decimal? CantidadAprobada,
    DetalleSolicitudEstado EstadoItem);

// Detalle para Bodeguero (resolución)
public record SolicitudMaterialResolucionDto(
    int Id,
    string Codigo,
    string FichaCode,
    string SolicitanteNombre,
    SolicitudMaterialEstado Estado,
    DateTime FechaSolicitud,
    string? Observaciones,
    string? EntregaCodigo,
    IReadOnlyList<DetalleResolucionDto> Detalles);

// Una línea del formulario de resolución
public record ResolveDetalleDto(int DetalleId, decimal CantidadAprobada);

// Resultado genérico de operaciones del servicio (éxito/error + mensaje)
public record ServiceResult(bool Success, string? Message = null)
{
    public static ServiceResult Ok(string? message = null) => new(true, message);
    public static ServiceResult Fail(string message) => new(false, message);
}

// --- Materiales de orden (bodega) ---

public record OrderMaterialLineDto(
    int Id,
    int MaterialId,
    string MaterialCode,
    string MaterialName,
    decimal QuantityRequired,
    decimal QuantityDelivered,
    decimal QuantityPending,
    decimal StockAvailable,
    decimal Difference, // Stock - QuantityPending (para lo que aún falta entregar)
    string UnitDisplay,
    MaterialUnit Unit,
    string? Observations,
    MaterialStockAvailability Availability,
    bool IsFullyDelivered);

public record OrderMaterialsDetailDto(
    int OrderId,
    string OrderNumber,
    string ProductName,
    OrderStatus Status,
    OrderMaterialsStatus MaterialsStatus,
    int TotalQuantity,
    int ProducedQuantity,
    bool CanRegisterProduction,
    bool CanEditRequirements,
    IReadOnlyList<OrderMaterialLineDto> Lines);

public record AddOrderMaterialDto(
    int OrderId,
    int MaterialId,
    decimal QuantityRequired,
    string? Observations);

public record DeliverOrderMaterialItemDto(int LineId, decimal QuantityToDeliver);

public record DeliverOrderMaterialsDto(
    int OrderId,
    IReadOnlyList<DeliverOrderMaterialItemDto> Items,
    string? Observations);

// --- Flujo MES de producción ---

public record OrderStageDto(
    int Id,
    string Name,
    int SortOrder,
    bool IsOptional,
    ProductionStageStatus Status,
    int? InstructorUserId,
    string? InstructorName,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? Observations,
    int QuantityReceived,
    int QuantityProcessed,
    int QuantitySent,
    int QuantityWithdrawn,
    int QuantityAvailable,
    bool IsCurrent);

public record OrderHistoryDto(
    int Id,
    DateTime AtUtc,
    ProductionHistoryEventType EventType,
    string Message,
    string? ActorUserName,
    string? StageName,
    int? Quantity);

public record OrderStageMovementDto(
    int Id,
    string MovementType,
    int Quantity,
    DateTime AtUtc,
    string ActorName,
    string? FromStage,
    string? ToStage,
    string? Motive,
    string? Observations);

public record FinishedGoodMovementDto(
    int Id,
    string ProductName,
    decimal Quantity,
    DateTime AtUtc,
    string ActorName,
    string? Observations);

public record OrderMesDetailDto(
    int OrderId,
    string OrderNumber,
    string ProductName,
    string? ClientName,
    OrderStatus Status,
    OrderMaterialsStatus MaterialsStatus,
    int TotalQuantity,
    int ProducedQuantity,
    int QtyProgressPercent,
    int FlowProgressPercent,
    int CombinedProgressPercent,
    string? CurrentStageName,
    DateOnly Deadline,
    string MrpHint,
    decimal FinishedGoodStock,
    bool CanManageFlow,
    IReadOnlyList<OrderStageDto> Stages,
    IReadOnlyList<OrderMaterialLineDto> MaterialLines,
    IReadOnlyList<OrderHistoryDto> History,
    IReadOnlyList<OrderStageMovementDto> Movements,
    IReadOnlyList<FinishedGoodMovementDto> InventoryIns);

public record AddOrderStageDto(int OrderId, string Name, bool IsOptional = false);
public record AssignStageInstructorDto(int StageId, int? InstructorUserId);
public record ProcessStageUnitsDto(int StageId, int Quantity, string? Observations);
public record SendToNextStageDto(int FromStageId, int Quantity, string? Observations);
public record PartialInventoryInDto(int OrderId, int StageId, int Quantity, string? Observations);

// Reingreso desde etapa MES: material de bodega (MaterialId) o producto terminado (MaterialId null)
public record StageReentryDto(
    int OrderId,
    int StageId,
    int Quantity,
    int? MaterialId,
    string? Observations);

public record PartialWithdrawalDto(
    int StageId,
    int Quantity,
    string Motive,
    string? Observations,
    int? AuthorizedByUserId);
public record UpsertStagePermissionDto(int UserId, string StageName, bool Allowed);
