using Moq;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class InventoryGetMaterialsByPlantaTests
{
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<IMaterialRequestRepository> _requests = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IBomRepository> _boms = new();
    private readonly Mock<IStockMovementRepository> _stockMovements = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private InventoryService CreateSut(ICurrentPlantaInventarioAccessor? accessor = null) => new(
        _materials.Object,
        _requests.Object,
        _orders.Object,
        _boms.Object,
        _stockMovements.Object,
        _uow.Object,
        accessor);

    [Fact]
    public async Task GetMaterialsByPlantaAsync_SinFiltro_AdminVeTodasLasPlantas()
    {
        _materials.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleMaterials());

        var result = await CreateSut().GetMaterialsByPlantaAsync(null);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, m => m.Name == "Tela" && m.PlantaInventarioId == 1 && m.PlantaInventarioNombre == "Planta 1");
        Assert.Contains(result, m => m.Name == "Hilo" && m.PlantaInventarioId == 2);
        Assert.Contains(result, m => m.Name == "Botón" && m.PlantaInventarioId == 1);
    }

    [Fact]
    public async Task GetMaterialsByPlantaAsync_ConPlantaValida_SoloEsaPlanta()
    {
        _materials.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleMaterials());

        var result = await CreateSut().GetMaterialsByPlantaAsync(1);

        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.Equal(1, m.PlantaInventarioId));
        Assert.DoesNotContain(result, m => m.Name == "Hilo");
    }

    [Fact]
    public async Task GetMaterialsByPlantaAsync_EncargadoConsultaPlantaAjena_ListaVacia()
    {
        _materials.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleMaterials());
        var accessor = new FixedCurrentPlantaInventarioAccessor([1]);

        var result = await CreateSut(accessor).GetMaterialsByPlantaAsync(2);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMaterialsByPlantaAsync_EncargadoSinFiltro_SoloSusPlantas()
    {
        _materials.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleMaterials());
        var accessor = new FixedCurrentPlantaInventarioAccessor([1]);

        var result = await CreateSut(accessor).GetMaterialsByPlantaAsync(null);

        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.Equal(1, m.PlantaInventarioId));
    }

    private static IReadOnlyList<Material> SampleMaterials()
    {
        var p1 = new PlantaInventario { Id = 1, Nombre = "Planta 1" };
        var p2 = new PlantaInventario { Id = 2, Nombre = "Planta 2" };
        return
        [
            new Material
            {
                Id = 1,
                Name = "Tela",
                Unit = MaterialUnit.Metros,
                Stock = 20,
                MinStock = 5,
                Status = MaterialStatus.Bueno,
                LastEntryDate = new DateOnly(2026, 1, 10),
                PlantaInventarioId = 1,
                PlantaInventario = p1
            },
            new Material
            {
                Id = 2,
                Name = "Hilo",
                Unit = MaterialUnit.Metros,
                Stock = 8,
                MinStock = 10,
                Status = MaterialStatus.Bueno,
                LastEntryDate = new DateOnly(2026, 1, 11),
                PlantaInventarioId = 2,
                PlantaInventario = p2
            },
            new Material
            {
                Id = 3,
                Name = "Botón",
                Unit = MaterialUnit.Unidades,
                Stock = 0,
                MinStock = 20,
                Status = MaterialStatus.Regular,
                LastEntryDate = new DateOnly(2026, 1, 12),
                PlantaInventarioId = 1,
                PlantaInventario = p1
            }
        ];
    }
}
