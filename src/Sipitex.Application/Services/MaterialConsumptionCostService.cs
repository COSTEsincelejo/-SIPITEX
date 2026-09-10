using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Application.Services;

public class MaterialConsumptionCostService : IMaterialConsumptionCostService
{
    public decimal CalcularPromedioPonderado(IReadOnlyList<ConsumoCostoLineaDto> lineas)
    {
        if (lineas is null || lineas.Count == 0)
            return 0;

        decimal cantidad = 0;
        decimal importe = 0;
        foreach (var linea in lineas)
        {
            if (linea.Cantidad <= 0)
                continue;
            cantidad += linea.Cantidad;
            importe += linea.Cantidad * linea.CostoUnitario;
        }

        return cantidad == 0 ? 0 : decimal.Round(importe / cantidad, 4, MidpointRounding.AwayFromZero);
    }
}
