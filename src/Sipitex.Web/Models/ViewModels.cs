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
    public RegisterProductionForm Register { get; set; } = new();
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

public class RegisterProductionForm
{
    public int ProductionOrderId { get; set; }
    public int FichaId { get; set; }
    public int Units { get; set; }
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
}

public class EstadisticasIndexViewModel
{
    public DashboardKpiDto Dashboard { get; set; } = new(0, 0, 0, 0, []);
}

