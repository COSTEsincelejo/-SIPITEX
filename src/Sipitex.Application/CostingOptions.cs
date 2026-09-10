namespace Sipitex.Application;

public class CostingOptions
{
    public const string SectionName = "Costing";

    // Tarifa de mano de obra (por hora). Configurable; no se asume un valor de negocio fijo.
    public decimal LaborHourRate { get; set; }
}
