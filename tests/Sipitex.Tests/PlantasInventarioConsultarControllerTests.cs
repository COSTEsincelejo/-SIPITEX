using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Controllers;
using Sipitex.Web.Models;

namespace Sipitex.Tests;

public class PlantasInventarioConsultarControllerTests
{
    private readonly Mock<IPlantaInventarioService> _plantas = new();
    private readonly Mock<IPlantaInventarioReassignmentService> _reassignment = new();
    private readonly Mock<IActivityLogService> _activity = new();
    private readonly Mock<IInventoryService> _inventory = new();

    private PlantasInventarioController CreateController(
        ClaimsPrincipal user,
        ICurrentPlantaInventarioAccessor accessor)
    {
        var controller = new PlantasInventarioController(
            _plantas.Object,
            _reassignment.Object,
            _activity.Object,
            _inventory.Object,
            accessor)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
        controller.TempData = new TempDataDictionary(
            controller.HttpContext,
            Mock.Of<ITempDataProvider>());
        return controller;
    }

    private static ClaimsPrincipal Principal(string role)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Name, "Usuario")
        ], "Test");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task Consultar_AdminSinFiltro_MuestraResumenDeTodasLasPlantas()
    {
        _plantas.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new PlantaInventario { Id = 1, Nombre = "Planta 1" },
                new PlantaInventario { Id = 2, Nombre = "Planta 2" }
            ]);
        _inventory.Setup(s => s.GetMaterialsByPlantaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new MaterialDto(1, "Tela", "m", MaterialUnit.Metros, 20, MaterialStatus.Bueno, 5, false, new DateOnly(2026, 1, 1), 0, 1, "Planta 1"),
                new MaterialDto(2, "Hilo", "m", MaterialUnit.Metros, 0, MaterialStatus.Bueno, 10, true, new DateOnly(2026, 1, 1), 0, 2, "Planta 2")
            ]);

        var result = await CreateController(
                Principal(UserRoles.Administrador),
                NullCurrentPlantaInventarioAccessor.Instance)
            .Consultar(null, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<ConsultarPlantasInventarioViewModel>(view.Model);
        Assert.Null(vm.PlantaInventarioId);
        Assert.Equal(2, vm.Plantas.Count);
        Assert.Equal(2, vm.Resumen.Count);
        Assert.Equal(1, vm.Resumen.Single(r => r.Id == 2).Critico);
    }

    [Fact]
    public async Task Consultar_EncargadoPlantaAjena_NoRecibeMaterialesDeOtraPlanta()
    {
        _plantas.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new PlantaInventario { Id = 1, Nombre = "Planta 1" },
                new PlantaInventario { Id = 2, Nombre = "Planta 2" }
            ]);
        _inventory.Setup(s => s.GetMaterialsByPlantaAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateController(
                Principal(UserRoles.EncargadoDeBodega),
                new FixedCurrentPlantaInventarioAccessor([1]))
            .Consultar(2, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<ConsultarPlantasInventarioViewModel>(view.Model);
        Assert.Empty(vm.Materials);
        Assert.Single(vm.Plantas);
        Assert.Equal(1, vm.Plantas[0].Id);
        Assert.DoesNotContain(vm.Plantas, p => p.Id == 2);
    }

    [Fact]
    public void Consultar_NoExponeMetodosDeMutacionAlInstructor()
    {
        var consultar = typeof(PlantasInventarioController).GetMethod(nameof(PlantasInventarioController.Consultar));
        Assert.NotNull(consultar);
        Assert.Equal(typeof(HttpGetAttribute), consultar!.GetCustomAttribute<HttpGetAttribute>()?.GetType() ?? typeof(HttpGetAttribute));
        Assert.Null(consultar.GetCustomAttribute<HttpPostAttribute>());
    }
}
