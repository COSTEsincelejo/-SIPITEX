using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

// Arma la pantalla Inventario por planta: plantas visibles, resumen y detalle filtrado.
public class InventarioPlantaService : IInventarioPlantaService
{
    private readonly IPlantaInventarioService _plantas;
    private readonly IInventoryService _inventory;
    private readonly ICurrentPlantaInventarioAccessor _accessor;

    public InventarioPlantaService(
        IPlantaInventarioService plantas,
        IInventoryService inventory,
        ICurrentPlantaInventarioAccessor accessor)
    {
        _plantas = plantas;
        _inventory = inventory;
        _accessor = accessor;
    }

    public async Task<InventarioPlantaConsultaDto> ConsultarAsync(
        int? plantaId,
        string? q,
        string? nivel,
        CancellationToken cancellationToken = default)
    {
        var plantas = Visible(await _plantas.GetAllAsync(cancellationToken));
        if (plantaId is int requested && plantas.All(p => p.Id != requested))
            plantaId = null;

        // Una sola lectura. El accessor ya limita al encargado; una planta ajena no se consulta.
        // Si igual llega un material de una planta no visible, no entra al detalle ni al consolidado.
        var permitidas = plantas.Select(p => p.Id).ToHashSet();
        var todos = (await _inventory.GetMaterialsByPlantaAsync(null, cancellationToken))
            .Where(m => permitidas.Contains(m.PlantaInventarioId))
            .ToList();
        var resumen = plantas.Select(p => Resumir(p, todos)).ToList();
        var deLaPlanta = plantaId is int id
            ? todos.Where(m => m.PlantaInventarioId == id).ToList()
            : todos.ToList();

        return new InventarioPlantaConsultaDto
        {
            PlantaId = plantaId,
            Plantas = plantas,
            Materiales = InventarioConsultaFilter.Apply(deLaPlanta, q, nivel),
            Resumen = resumen,
            Consolidado = InventarioPlantaResumenDto.Sumar(resumen),
            Q = q,
            Nivel = nivel,
            TotalSinFiltro = deLaPlanta.Count
        };
    }

    private IReadOnlyList<PlantaInventario> Visible(IReadOnlyList<PlantaInventario> plantas)
    {
        var allowed = _accessor.PlantaInventarioIds;
        if (allowed is null)
            return plantas;

        return plantas.Where(p => allowed.Contains(p.Id)).ToList();
    }

    private static InventarioPlantaResumenDto Resumir(
        PlantaInventario planta,
        IReadOnlyList<MaterialDto> materiales)
    {
        var conteos = StockNivelHelper.Contar(
            materiales
                .Where(m => m.PlantaInventarioId == planta.Id)
                .Select(m => StockNivelHelper.Classify(m.Stock, m.MinStock)));

        return new InventarioPlantaResumenDto
        {
            Id = planta.Id,
            Nombre = planta.Nombre,
            Insumos = conteos.Total,
            Ok = conteos.Ok,
            Bajo = conteos.Bajo,
            Critico = conteos.Critico,
            SinMinimo = conteos.SinMinimo
        };
    }
}
