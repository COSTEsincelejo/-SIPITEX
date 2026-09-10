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

/// <summary>
/// Scoping de PlantasInventarioSolicitudesController.Index por las plantasInventario asignadas al encargadoDeBodega autenticado.
/// </summary>
public class PlantaInventarioSolicitudesScopeTests
{
    private readonly Mock<ISolicitudMaterialService> _solicitudes = new();
    private readonly Mock<ISolicitudMaterialApprovalService> _approval = new();
    private readonly Mock<IInventoryService> _inventory = new();
    private readonly Mock<ICurrentPlantaInventarioAccessor> _plantaInventario = new();

    private PlantasInventarioSolicitudesController CreateController(ClaimsPrincipal user)
    {
        var controller = new PlantasInventarioSolicitudesController(
            _solicitudes.Object,
            _approval.Object,
            _inventory.Object,
            _plantaInventario.Object)
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

    private static ClaimsPrincipal Principal(int userId, string role = UserRoles.EncargadoDeBodega) =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Name, "Pedro")
        ], "Test"));

    private static SolicitudMaterialListItemDto Item(int id, string codigo) =>
        new(id, codigo, SolicitudMaterialTipo.PorFicha, "F1", SolicitudMaterialEstado.Pendiente, DateTime.UtcNow, "Laura");

    [Fact]
    public async Task Index_EncargadoDeBodegaPlantaInventario1_SoloVeSolicitudesDePlantaInventario1()
    {
        IReadOnlyList<int> ids = [1];
        _plantaInventario.SetupGet(a => a.PlantaInventarioIds).Returns(ids);
        _solicitudes
            .Setup(s => s.GetListForPlantaInventarioAsync(
                It.Is<IReadOnlyList<int>>(x => x.Count == 1 && x[0] == 1),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([Item(1, "SOL-B1")]);

        var controller = CreateController(Principal(5));
        var result = await controller.Index(estado: null, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<PlantasInventarioSolicitudesIndexViewModel>(view.Model);
        Assert.Single(vm.Solicitudes);
        Assert.Equal("SOL-B1", vm.Solicitudes[0].Codigo);

        _solicitudes.Verify(
            s => s.GetListForPlantaInventarioAsync(
                It.Is<IReadOnlyList<int>>(x => x.Count == 1 && x[0] == 1),
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _solicitudes.Verify(
            s => s.GetListForPlantaInventarioAsync(
                It.Is<IReadOnlyList<int>>(x => x.Contains(2) && x.Count == 1),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _solicitudes.Verify(
            s => s.GetListForPlantaInventarioAsync(null, It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Index_EncargadoDeBodegaConDosPlantasInventario_PasaAmbasAlServicio()
    {
        IReadOnlyList<int> ids = [1, 2];
        _plantaInventario.SetupGet(a => a.PlantaInventarioIds).Returns(ids);
        _solicitudes
            .Setup(s => s.GetListForPlantaInventarioAsync(
                It.Is<IReadOnlyList<int>>(x => x.Count == 2 && x.Contains(1) && x.Contains(2)),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([Item(1, "SOL-B1"), Item(2, "SOL-B2")]);

        var controller = CreateController(Principal(5));
        var result = await controller.Index(estado: null, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<PlantasInventarioSolicitudesIndexViewModel>(view.Model);
        Assert.Equal(2, vm.Solicitudes.Count);
    }

    [Fact]
    public async Task Index_EncargadoDeBodegaSinPlantaInventarioAsignada_BloqueaConMensaje()
    {
        _plantaInventario.SetupGet(a => a.PlantaInventarioIds).Returns(Array.Empty<int>());

        var controller = CreateController(Principal(5));
        var result = await controller.Index(estado: null, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<PlantasInventarioSolicitudesIndexViewModel>(view.Model);
        Assert.Empty(vm.Solicitudes);
        Assert.False(vm.IsSuccess);
        Assert.Contains("plantaInventario asignada", vm.Message, StringComparison.OrdinalIgnoreCase);

        _solicitudes.Verify(
            s => s.GetListForPlantaInventarioAsync(It.IsAny<IReadOnlyList<int>?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
