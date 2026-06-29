using Sipitex.Domain.Enums;

namespace Sipitex.Application.DTOs;

public record MaterialDto(
    int Id,
    string Name,
    string UnitDisplay,
    decimal Stock,
    MaterialStatus Status,
    decimal MinStock,
    bool IsLowStock);

public record CreateMaterialDto(string Name, decimal Stock, MaterialUnit Unit);

public record AdjustStockDto(int MaterialId, decimal NewStock);

public record MaterialRequestDto(
    int Id,
    string MaterialName,
    decimal Quantity,
    string OrderNumber,
    RequestStatus Status);

public record CreateMaterialRequestDto(int ProductionOrderId, int MaterialId, decimal Quantity);

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

public record FichaDto(
    int Id,
    string FichaCode,
    string ProcessName,
    string InstructorName,
    string? AssignedOrderNumber);

public record RegisterProductionDto(int ProductionOrderId, int FichaId, int Units);

public record QualityRecordDto(
    string OrderNumber,
    int Units,
    QualityResult Result,
    DateOnly Date);

public record CreateQualityRecordDto(int ProductionOrderId, int Units, QualityResult Result);

public record DashboardKpiDto(
    int TotalProduced,
    decimal QualityRate,
    int ActiveOrders,
    int LowStockCount,
    IReadOnlyList<ChartBarDto> ChartData);

public record ChartBarDto(string Label, int Produced, int Target);

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

public record ServiceResult(bool Success, string? Message = null)
{
    public static ServiceResult Ok(string? message = null) => new(true, message);
    public static ServiceResult Fail(string message) => new(false, message);
}
