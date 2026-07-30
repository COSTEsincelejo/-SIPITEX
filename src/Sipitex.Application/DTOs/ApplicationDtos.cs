using Sipitex.Domain.Enums;

namespace Sipitex.Application.DTOs;

// --- Inventario ---

public record MaterialDto(
    int Id,
    string Name,
    string UnitDisplay,
    decimal Stock,
    MaterialStatus Status,
    decimal MinStock,
    bool IsLowStock,
    DateOnly LastEntryDate);

public record CreateMaterialDto(string Name, decimal Stock, MaterialUnit Unit);

public record AdjustStockDto(int MaterialId, decimal NewStock);

public record UpdateMaterialStatusDto(int MaterialId, MaterialStatus Status);

public record MaterialRequestDto(
    int Id,
    string MaterialName,
    decimal Quantity,
    string OrderNumber,
    RequestStatus Status);

public record CreateMaterialRequestDto(int ProductionOrderId, int MaterialId, decimal Quantity);

// --- Producción ---

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

public record CreateProductionOrderDto(string ProductName, int TotalQuantity, DateOnly Deadline);

// --- MRP / BOM ---

public record BomItemDto(string ProductName, string MaterialName, decimal QuantityPerUnit, string UnitDisplay);

public record MrpSimulationResultDto(
    string ProductName,
    decimal Quantity,
    IReadOnlyList<MrpLineDto> Lines);

public record MrpLineDto(
    string MaterialName,
    decimal Required,
    decimal Available,
    decimal Deficit,
    string UnitDisplay,
    bool IsOk);

// --- Fichas y sesiones ---

public record FichaDto(
    int Id,
    string FichaCode,
    string ProcessName,
    string InstructorName,
    string? AssignedOrderNumber,
    int? InstructorUserId = null,
    string Turno = "");

public record CreateFichaDto(
    string FichaCode,
    string ProcessName,
    string InstructorName,
    string Turno,
    int? ProductionOrderId = null);

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

public record RegisterProductionDto(int ProductionOrderId, int FichaId, int Units, string? Observations = null);

// --- Calidad ---

public record QualityRecordDto(
    string OrderNumber,
    int Units,
    QualityResult Result,
    DateOnly Date,
    string? MotivoReproceso,
    string? Responsable);

public record CreateQualityRecordDto(
    int ProductionOrderId,
    int Units,
    QualityResult Result,
    string? MotivoReproceso = null,
    string? Responsable = null);

// --- Dashboard ---

public record DashboardKpiDto(
    int TotalProduced,
    decimal QualityRate,
    int ActiveOrders,
    int LowStockCount,
    IReadOnlyList<ChartBarDto> ChartData);

public record ChartBarDto(string Label, int Produced, int Target);

// --- Requisitos del proyecto (matriz RF/RNF) ---

public record RequirementSummaryDto(int Cumple, int Parcial, int Ausente);

public record FunctionalRequirementDto(
    string Code,
    string Description,
    string Module,
    ComplianceStatus Status,
    string Observation);

public record NonFunctionalRequirementDto(
    string Code,
    string Description,
    ComplianceStatus Status,
    string Observation);

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
