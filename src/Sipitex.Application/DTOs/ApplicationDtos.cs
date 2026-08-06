using Sipitex.Domain.Enums;

namespace Sipitex.Application.DTOs;

// --- Inventario ---

// Material tal como se muestra en la tabla de inventario
public record MaterialDto(
    int Id,
    string Name,
    string UnitDisplay, // unidad legible (m, kg, ud)
    decimal Stock,
    MaterialStatus Status,
    decimal MinStock,
    bool IsLowStock, // true si hay que alertar
    DateOnly LastEntryDate);

// Datos para crear material nuevo
public record CreateMaterialDto(string Name, decimal Stock, MaterialUnit Unit);

// Ajuste manual de stock por id de material
public record AdjustStockDto(int MaterialId, decimal NewStock);

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
    string MrpHint);

// Alta de orden nueva
public record CreateProductionOrderDto(string ProductName, int TotalQuantity, DateOnly Deadline);

// --- MRP / BOM ---

// Una línea del listado BOM
public record BomItemDto(string ProductName, string MaterialName, decimal QuantityPerUnit, string UnitDisplay);

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

// Instructor asignado a una ficha (para chips / quitar)
public record FichaInstructorDto(int UserId, string Nombre);

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
    int? ProductionOrderId = null);

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

// Resultado genérico de operaciones del servicio (éxito/error + mensaje)
public record ServiceResult(bool Success, string? Message = null)
{
    public static ServiceResult Ok(string? message = null) => new(true, message);
    public static ServiceResult Fail(string message) => new(false, message);
}
