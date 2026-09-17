using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
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

    private static MaterialDto Mat(
        int id,
        string name,
        decimal stock,
        decimal min,
        int plantaId,
        string plantaNombre) =>
        new(
            id,
            name,
            "m",
            MaterialUnit.Metros,
            stock,
            MaterialStatus.Bueno,
            min,
            stock < min,
            new DateOnly(2026, 1, 1),
            0,
            plantaId,
            plantaNombre);

    private void SetupTwoPlantas()
    {
        _plantas.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new PlantaInventario { Id = 1, Nombre = "Planta 1" },
                new PlantaInventario { Id = 2, Nombre = "Planta 2" }
            ]);
    }

    [Fact]
    public async Task Consultar_AdminSinFiltro_MuestraResumenDeTodasLasPlantas()
    {
        SetupTwoPlantas();
        _inventory.Setup(s => s.GetMaterialsByPlantaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Mat(1, "Tela", 20, 5, 1, "Planta 1"),
                Mat(2, "Hilo", 0, 10, 2, "Planta 2")
            ]);

        var result = await CreateController(
                Principal(UserRoles.Administrador),
                NullCurrentPlantaInventarioAccessor.Instance)
            .Consultar(null, cancellationToken: CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<ConsultarPlantasInventarioViewModel>(view.Model);
        Assert.Null(vm.PlantaInventarioId);
        Assert.Equal(2, vm.Plantas.Count);
        Assert.Equal(2, vm.Resumen.Count);
        Assert.Equal(1, vm.Resumen.Single(r => r.Id == 2).Critico);
    }

    [Fact]
    public async Task Consultar_AdminCambiaDePlanta_SoloMaterialesDeEsaPlanta()
    {
        SetupTwoPlantas();
        _inventory.Setup(s => s.GetMaterialsByPlantaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Mat(1, "Tela", 20, 5, 1, "Planta 1"),
                Mat(3, "Botón", 4, 10, 1, "Planta 1")
            ]);
        _inventory.Setup(s => s.GetMaterialsByPlantaAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Mat(2, "Hilo", 8, 10, 2, "Planta 2")
            ]);

        var controller = CreateController(
            Principal(UserRoles.Administrador),
            NullCurrentPlantaInventarioAccessor.Instance);

        var planta1 = Assert.IsType<ConsultarPlantasInventarioViewModel>(
            Assert.IsType<ViewResult>(await controller.Consultar(1, cancellationToken: CancellationToken.None)).Model);
        Assert.Equal(1, planta1.PlantaInventarioId);
        Assert.Equal(2, planta1.Materials.Count);
        Assert.All(planta1.Materials, m => Assert.Equal(1, m.PlantaInventarioId));
        Assert.DoesNotContain(planta1.Materials, m => m.Name == "Hilo");

        var planta2 = Assert.IsType<ConsultarPlantasInventarioViewModel>(
            Assert.IsType<ViewResult>(await controller.Consultar(2, cancellationToken: CancellationToken.None)).Model);
        Assert.Equal(2, planta2.PlantaInventarioId);
        Assert.Single(planta2.Materials);
        Assert.Equal("Hilo", planta2.Materials[0].Name);
    }

    [Fact]
    public async Task Consultar_AdminPlantaSinInsumos_ListaVacia()
    {
        SetupTwoPlantas();
        _inventory.Setup(s => s.GetMaterialsByPlantaAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateController(
                Principal(UserRoles.Administrador),
                NullCurrentPlantaInventarioAccessor.Instance)
            .Consultar(2, cancellationToken: CancellationToken.None);

        var vm = Assert.IsType<ConsultarPlantasInventarioViewModel>(
            Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(2, vm.PlantaInventarioId);
        Assert.Empty(vm.Materials);
        Assert.Equal(0, vm.TotalSinFiltro);
        Assert.Empty(vm.Resumen);
    }

    [Fact]
    public async Task Consultar_FiltroPorNivelFaltantes_SoloBajoYCritico()
    {
        SetupTwoPlantas();
        _inventory.Setup(s => s.GetMaterialsByPlantaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Mat(1, "Tela", 20, 5, 1, "Planta 1"),
                Mat(2, "Hilo", 3, 10, 1, "Planta 1"),
                Mat(3, "Botón", 0, 20, 1, "Planta 1")
            ]);

        var result = await CreateController(
                Principal(UserRoles.Administrador),
                NullCurrentPlantaInventarioAccessor.Instance)
            .Consultar(1, nivel: InventarioConsultaFilter.Faltantes, cancellationToken: CancellationToken.None);

        var vm = Assert.IsType<ConsultarPlantasInventarioViewModel>(
            Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(3, vm.TotalSinFiltro);
        Assert.Equal(2, vm.Materials.Count);
        Assert.Contains(vm.Materials, m => m.Name == "Hilo");
        Assert.Contains(vm.Materials, m => m.Name == "Botón");
        Assert.DoesNotContain(vm.Materials, m => m.Name == "Tela");
        Assert.All(
            vm.Materials,
            m => Assert.NotEqual(StockNivel.Ok, StockNivelHelper.Classify(m.Stock, m.MinStock)));
    }

    [Fact]
    public async Task Consultar_FiltroPorNombre_SoloCoincidentes()
    {
        SetupTwoPlantas();
        _inventory.Setup(s => s.GetMaterialsByPlantaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Mat(1, "Tela Jersey", 20, 5, 1, "Planta 1"),
                Mat(2, "Hilo Poliéster", 3, 10, 1, "Planta 1")
            ]);

        var result = await CreateController(
                Principal(UserRoles.Administrador),
                NullCurrentPlantaInventarioAccessor.Instance)
            .Consultar(1, q: "hilo", cancellationToken: CancellationToken.None);

        var vm = Assert.IsType<ConsultarPlantasInventarioViewModel>(
            Assert.IsType<ViewResult>(result).Model);
        Assert.Single(vm.Materials);
        Assert.Equal("Hilo Poliéster", vm.Materials[0].Name);
        Assert.Equal(2, vm.TotalSinFiltro);
    }

    [Fact]
    public async Task Consultar_EncargadoPlantaAjena_NoListaNiConsultaOtraPlanta()
    {
        SetupTwoPlantas();
        _inventory.Setup(s => s.GetMaterialsByPlantaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Mat(1, "Tela", 20, 5, 1, "Planta 1")
            ]);
        _inventory.Setup(s => s.GetMaterialsByPlantaAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Mat(9, "Insumo ajeno", 50, 1, 2, "Planta 2")
            ]);

        var result = await CreateController(
                Principal(UserRoles.EncargadoDeBodega),
                new FixedCurrentPlantaInventarioAccessor([1]))
            .Consultar(2, cancellationToken: CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<ConsultarPlantasInventarioViewModel>(view.Model);
        Assert.Null(vm.PlantaInventarioId);
        Assert.Single(vm.Plantas);
        Assert.Equal(1, vm.Plantas[0].Id);
        Assert.DoesNotContain(vm.Plantas, p => p.Id == 2);
        Assert.DoesNotContain(vm.Materials, m => m.Name == "Insumo ajeno");
        Assert.All(vm.Materials, m => Assert.Equal(1, m.PlantaInventarioId));
        _inventory.Verify(s => s.GetMaterialsByPlantaAsync(2, It.IsAny<CancellationToken>()), Times.Never);
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
