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
            .Index(CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<InventarioPlantaViewModel>(view.Model);
        Assert.Null(vm.PlantaId);
        Assert.Equal(2, vm.Plantas.Count);
        Assert.Equal(2, vm.Resumen.Count);
        Assert.Equal(1, vm.Resumen.Single(r => r.Id == 2).Critico);
        Assert.Equal(2, vm.Consolidado.Insumos);
        Assert.Equal(vm.Resumen.Sum(r => r.Insumos), vm.Consolidado.Insumos);
        Assert.Equal(vm.Resumen.Sum(r => r.Ok), vm.Consolidado.Ok);
        Assert.Equal(vm.Resumen.Sum(r => r.Critico), vm.Consolidado.Critico);
        Assert.Equal(2, vm.Materials.Count);
        Assert.True(vm.PuedeAdministrarPlantas);
    }

    [Fact]
    public async Task Consultar_Resumen_CuadraConStockNivelHelper()
    {
        SetupTwoPlantas();
        MaterialDto[] materials =
        [
            Mat(1, "Tela", 20, 5, 1, "Planta 1"),
            Mat(2, "Hilo", 2, 10, 1, "Planta 1"),
            Mat(3, "Botón", 0, 4, 1, "Planta 1"),
            Mat(4, "Guata", 0, 0, 1, "Planta 1"),
            Mat(5, "Forro", 3, 8, 2, "Planta 2")
        ];
        _inventory.Setup(s => s.GetMaterialsByPlantaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(materials);

        var vm = Assert.IsType<InventarioPlantaViewModel>(
            Assert.IsType<ViewResult>(await CreateController(
                    Principal(UserRoles.Administrador),
                    NullCurrentPlantaInventarioAccessor.Instance)
                .Index(CancellationToken.None)).Model);

        var planta1 = vm.Resumen.Single(r => r.Id == 1);
        var esperado = StockNivelHelper.Contar(materials.Where(m => m.PlantaInventarioId == 1).Select(m => StockNivelHelper.Classify(m.Stock, m.MinStock)));
        Assert.Equal(esperado.Ok, planta1.Ok);
        Assert.Equal(esperado.Bajo, planta1.Bajo);
        Assert.Equal(esperado.Critico, planta1.Critico);
        Assert.Equal(esperado.SinMinimo, planta1.SinMinimo);
        Assert.Equal(1, planta1.Ok);
        Assert.Equal(1, planta1.Bajo);
        Assert.Equal(1, planta1.Critico);
        Assert.Equal(1, planta1.SinMinimo);
        Assert.Equal(planta1.Insumos, planta1.Ok + planta1.Bajo + planta1.Critico + planta1.SinMinimo);
        Assert.Equal(esperado.Total, planta1.Insumos);
        var todos = StockNivelHelper.Contar(materials.Select(m => StockNivelHelper.Classify(m.Stock, m.MinStock)));
        Assert.Equal(todos.Total, vm.Consolidado.Insumos);
        Assert.Equal(todos.Ok, vm.Consolidado.Ok);
        Assert.Equal(todos.Bajo, vm.Consolidado.Bajo);
        Assert.Equal(todos.Critico, vm.Consolidado.Critico);
        Assert.Equal(todos.SinMinimo, vm.Consolidado.SinMinimo);
        Assert.Equal(vm.Resumen.Sum(r => r.Insumos), vm.Consolidado.Insumos);
    }

    [Fact]
    public async Task Consultar_AdminCambiaDePlanta_SoloMaterialesDeEsaPlanta()
    {
        SetupTwoPlantas();
        _inventory.Setup(s => s.GetMaterialsByPlantaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Mat(1, "Tela", 20, 5, 1, "Planta 1"),
                Mat(3, "Botón", 4, 10, 1, "Planta 1"),
                Mat(2, "Hilo", 8, 10, 2, "Planta 2")
            ]);

        var controller = CreateController(
            Principal(UserRoles.Administrador),
            NullCurrentPlantaInventarioAccessor.Instance);

        var planta1 = Assert.IsType<InventarioPlantaViewModel>(
            Assert.IsType<ViewResult>(await controller.Index(CancellationToken.None, plantaId: 1)).Model);
        Assert.Equal(1, planta1.PlantaId);
        Assert.Equal(2, planta1.Materials.Count);
        Assert.All(planta1.Materials, m => Assert.Equal(1, m.PlantaInventarioId));
        Assert.DoesNotContain(planta1.Materials, m => m.Name == "Hilo");

        var planta2 = Assert.IsType<InventarioPlantaViewModel>(
            Assert.IsType<ViewResult>(await controller.Index(CancellationToken.None, plantaId: 2)).Model);
        Assert.Equal(2, planta2.PlantaId);
        Assert.Single(planta2.Materials);
        Assert.Equal("Hilo", planta2.Materials[0].Name);
    }

    [Fact]
    public async Task Consultar_AdminPlantaSinInsumos_ListaVacia()
    {
        SetupTwoPlantas();
        _inventory.Setup(s => s.GetMaterialsByPlantaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateController(
                Principal(UserRoles.Administrador),
                NullCurrentPlantaInventarioAccessor.Instance)
            .Index(CancellationToken.None, plantaId: 2);

        var vm = Assert.IsType<InventarioPlantaViewModel>(
            Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(2, vm.PlantaId);
        Assert.Empty(vm.Materials);
        Assert.Equal(0, vm.TotalSinFiltro);
        Assert.Equal(0, vm.Resumen.Single(r => r.Id == 2).Insumos);
    }

    [Fact]
    public async Task Consultar_FiltroPorNivelFaltantes_SoloBajoYCritico()
    {
        SetupTwoPlantas();
        _inventory.Setup(s => s.GetMaterialsByPlantaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Mat(1, "Tela", 20, 5, 1, "Planta 1"),
                Mat(2, "Hilo", 3, 10, 1, "Planta 1"),
                Mat(3, "Botón", 0, 20, 1, "Planta 1")
            ]);

        var result = await CreateController(
                Principal(UserRoles.Administrador),
                NullCurrentPlantaInventarioAccessor.Instance)
            .Index(CancellationToken.None, plantaId: 1, nivel: InventarioConsultaFilter.Faltantes);

        var vm = Assert.IsType<InventarioPlantaViewModel>(
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
        _inventory.Setup(s => s.GetMaterialsByPlantaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Mat(1, "Tela Jersey", 20, 5, 1, "Planta 1"),
                Mat(2, "Hilo Poliéster", 3, 10, 1, "Planta 1")
            ]);

        var result = await CreateController(
                Principal(UserRoles.Administrador),
                NullCurrentPlantaInventarioAccessor.Instance)
            .Index(CancellationToken.None, plantaId: 1, q: "hilo");

        var vm = Assert.IsType<InventarioPlantaViewModel>(
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
            .Index(CancellationToken.None, plantaId: 2);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<InventarioPlantaViewModel>(view.Model);
        Assert.Null(vm.PlantaId);
        Assert.False(vm.PuedeAdministrarPlantas);
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

    [Fact]
    public void Consultar_Redirige301AlIndexConservandoPlantaYFiltros()
    {
        var result = CreateController(
                Principal(UserRoles.Instructor),
                NullCurrentPlantaInventarioAccessor.Instance)
            .Consultar(2, q: "hilo", nivel: "Critico");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.True(redirect.Permanent);
        Assert.Equal(nameof(PlantasInventarioController.Index), redirect.ActionName);
        Assert.Equal(2, redirect.RouteValues!["plantaId"]);
        Assert.Equal("hilo", redirect.RouteValues["q"]);
        Assert.Equal("Critico", redirect.RouteValues["nivel"]);
    }

    [Fact]
    public async Task Index_QueryAntiguaPlantaInventarioId_RedirigeAPlantaId()
    {
        var result = await CreateController(
                Principal(UserRoles.Administrador),
                NullCurrentPlantaInventarioAccessor.Instance)
            .Index(CancellationToken.None, plantaInventarioId: 1, q: "tela");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.True(redirect.Permanent);
        Assert.Equal(1, redirect.RouteValues!["plantaId"]);
        Assert.Equal("tela", redirect.RouteValues["q"]);
        _inventory.Verify(s => s.GetMaterialsByPlantaAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Index_Instructor_ConsultaSinAdministrarPlantas()
    {
        SetupTwoPlantas();
        _inventory.Setup(s => s.GetMaterialsByPlantaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Mat(1, "Tela", 20, 5, 1, "Planta 1")]);

        var vm = Assert.IsType<InventarioPlantaViewModel>(
            Assert.IsType<ViewResult>(await CreateController(
                    Principal(UserRoles.Instructor),
                    NullCurrentPlantaInventarioAccessor.Instance)
                .Index(CancellationToken.None)).Model);

        Assert.Equal(2, vm.Plantas.Count);
        Assert.False(vm.PuedeAdministrarPlantas);
    }
}
