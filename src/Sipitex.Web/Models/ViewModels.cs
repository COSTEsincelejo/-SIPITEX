using Sipitex.Application.DTOs;
using Sipitex.Domain.Enums;

namespace Sipitex.Web.Models;

// Aquí están los modelos usados por las vistas para mostrar datos al usuario.
public class InventarioIndexViewModel
{
    public IReadOnlyList<MaterialDto> Materials { get; set; } = [];
    public IReadOnlyList<MaterialRequestDto> Requests { get; set; } = [];
    public IReadOnlyList<ProductionOrderDto> Orders { get; set; } = [];
    public CreateMaterialForm CreateMaterial { get; set; } = new();
    public CreateRequestForm CreateRequest { get; set; } = new();
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

public class CreateMaterialForm
{
    public string Name { get; set; } = string.Empty;
    public decimal Stock { get; set; }
    public MaterialUnit Unit { get; set; } = MaterialUnit.Metros;
}

public class CreateRequestForm
{
    public int ProductionOrderId { get; set; }
    public int MaterialId { get; set; }
    public decimal Quantity { get; set; }
}

public class AdjustStockForm
{
    public int MaterialId { get; set; }
    public decimal NewStock { get; set; }
}

public class OrdenesIndexViewModel
{
    public IReadOnlyList<ProductionOrderDto> Orders { get; set; } = [];
    public CreateOrderForm CreateOrder { get; set; } = new();
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

public class CreateOrderForm
{
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public DateOnly Deadline { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}

public class MrpIndexViewModel
{
    public IReadOnlyList<BomItemDto> Bom { get; set; } = [];
    public MrpSimulationForm Simulation { get; set; } = new();
    public MrpSimulationResultDto? Result { get; set; }
}

public class MrpSimulationForm
{
    public string ProductName { get; set; } = "Camisa";
    public decimal Quantity { get; set; } = 50;
}

public class FichasIndexViewModel
{
    public IReadOnlyList<FichaDto> Fichas { get; set; } = [];
    public IReadOnlyList<ProductionOrderDto> Orders { get; set; } = [];
    public IReadOnlyList<ProductionSessionDto> Sessions { get; set; } = [];
    public CreateFichaForm CreateFicha { get; set; } = new();
    public RegisterProductionForm Register { get; set; } = new();
    public bool IsAdministrator { get; set; }
    public string? FichaCodeFilter { get; set; }
    public string? InstructorFilter { get; set; }
    public string? TurnoFilter { get; set; }
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

public class CreateFichaForm
{
    public string FichaCode { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string Turno { get; set; } = "Mañana";
    public int? ProductionOrderId { get; set; }
}

public class RegisterProductionForm
{
    public int ProductionOrderId { get; set; }
    public int FichaId { get; set; }
    public int Units { get; set; }
    public string? Observations { get; set; }
}

public class CalidadIndexViewModel
{
    public IReadOnlyList<QualityRecordDto> Records { get; set; } = [];
    public IReadOnlyList<ProductionOrderDto> Orders { get; set; } = [];
    public CreateQualityForm Create { get; set; } = new();
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

public class CreateQualityForm
{
    public int ProductionOrderId { get; set; }
    public int Units { get; set; }
    public QualityResult Result { get; set; } = QualityResult.Aprobada;
    public string? MotivoReproceso { get; set; }
    public string? Responsable { get; set; }
}

public class EstadisticasIndexViewModel
{
    public DashboardKpiDto Dashboard { get; set; } = new(0, 0, 0, 0, []);
}

public class AlertasIndexViewModel
{
    public IReadOnlyList<AlertPreferenceDto> Preferences { get; set; } = [];
    public IReadOnlyList<AlertDeliveryDto> Deliveries { get; set; } = [];
    public bool SmtpConfigured { get; set; }
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

public class AlertPreferencesForm
{
    public List<string> EnabledTypes { get; set; } = [];
}

public class EmptyStateModel
{
    public string Icon { get; set; } = "fa-inbox";
    public string Title { get; set; } = "Sin registros";
    public string Text { get; set; } = "Todavía no hay información para mostrar.";
    public string? ActionText { get; set; }
    public string? ActionHref { get; set; }
}

