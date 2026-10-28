using Sipitex.Domain.Entities;

namespace Sipitex.Application.DTOs;

// Consulta unificada de Inventario por planta: selector, resumen y detalle.
public sealed class InventarioPlantaConsultaDto
{
    public int? PlantaId { get; init; }
    public IReadOnlyList<PlantaInventario> Plantas { get; init; } = [];
    public IReadOnlyList<MaterialDto> Materiales { get; init; } = [];
    public IReadOnlyList<InventarioPlantaResumenDto> Resumen { get; init; } = [];
    public InventarioPlantaResumenDto Consolidado { get; init; } = new();
    public string? Q { get; init; }
    public string? Nivel { get; init; }
    public int TotalSinFiltro { get; init; }
}

// Conteos de una planta, o la suma de todas cuando Nombre es "Todas".
public sealed class InventarioPlantaResumenDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public int Insumos { get; init; }
    public int Ok { get; init; }
    public int Bajo { get; init; }
    public int Critico { get; init; }
    public int SinMinimo { get; init; }

    public static InventarioPlantaResumenDto Sumar(IReadOnlyList<InventarioPlantaResumenDto> filas) =>
        new()
        {
            Nombre = "Todas",
            Insumos = filas.Sum(f => f.Insumos),
            Ok = filas.Sum(f => f.Ok),
            Bajo = filas.Sum(f => f.Bajo),
            Critico = filas.Sum(f => f.Critico),
            SinMinimo = filas.Sum(f => f.SinMinimo)
        };
}
