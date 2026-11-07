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

public class SolicitarInsumosPlantasTests
{
    [Fact]
    public async Task SolicitarInsumos_ListaSoloLasPlantasActivas_OrdenadasPorId()
    {
        var plantas = new Mock<IPlantaInventarioService>();
        plantas.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new PlantaInventario { Id = 9, Nombre = "centro logistico CMTC", Activo = true },
                new PlantaInventario { Id = 4, Nombre = "bodega 1 corte CMTC", Activo = true },
                new PlantaInventario { Id = 2, Nombre = "Planta de Inventario 2", Activo = false },
                new PlantaInventario { Id = 11, Nombre = "anexo corte", Activo = true },
                new PlantaInventario { Id = 6, Nombre = "taller de confección", Activo = true }
            ]);

        var fichas = new Mock<IFichaService>();
        fichas.Setup(s => s.GetFichasAsync(
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var ordenes = new Mock<IProductionOrderService>();
        ordenes.Setup(s => s.GetOrdersAsync(
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var controller = new SolicitudesMaterialController(
            Mock.Of<ISolicitudMaterialService>(),
            fichas.Object,
            ordenes.Object,
            plantas.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, "10"),
                        new Claim(ClaimTypes.Role, UserRoles.Instructor),
                        new Claim(ClaimTypes.Name, "Laura")
                    ], "Test"))
                }
            }
        };
        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

        var result = await controller.SolicitarInsumos(CancellationToken.None);
        var vm = Assert.IsType<SolicitarInsumosViewModel>(Assert.IsType<ViewResult>(result).Model);

        Assert.Equal([4, 6, 9, 11], vm.PlantasInventario.Select(p => p.Id).ToArray());
        Assert.Equal(
            ["bodega 1 corte CMTC", "taller de confección", "centro logistico CMTC", "anexo corte"],
            vm.PlantasInventario.Select(p => p.Label).ToArray());
        Assert.Null(vm.Form.PlantaInventarioId);
    }
}
