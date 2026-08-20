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
/// Scoping de BodegaSolicitudesController.Index por User.BodegaId del bodeguero autenticado.
/// </summary>
public class BodegaSolicitudesScopeTests
{
    private readonly Mock<ISolicitudMaterialService> _solicitudes = new();
    private readonly Mock<ISolicitudMaterialApprovalService> _approval = new();
    private readonly Mock<IInventoryService> _inventory = new();
    private readonly Mock<ICurrentBodegaAccessor> _bodega = new();

    private BodegaSolicitudesController CreateController(ClaimsPrincipal user)
    {
        var controller = new BodegaSolicitudesController(
            _solicitudes.Object,
            _approval.Object,
            _inventory.Object,
            _bodega.Object)
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

    private static ClaimsPrincipal Principal(int userId, string role = UserRoles.Bodeguero) =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Name, "Pedro")
        ], "Test"));

    private static SolicitudMaterialListItemDto Item(int id, string codigo) =>
        new(id, codigo, SolicitudMaterialTipo.PorFicha, "F1", SolicitudMaterialEstado.Pendiente, DateTime.UtcNow, "Laura");

    [Fact]
    public async Task Index_BodegueroBodega1_SoloVeSolicitudesDeBodega1()
    {
        _bodega.SetupGet(a => a.BodegaId).Returns(1);
        _solicitudes
            .Setup(s => s.GetListForBodegaAsync(1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Item(1, "SOL-B1")]);

        var controller = CreateController(Principal(5));
        var result = await controller.Index(estado: null, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<BodegaSolicitudesIndexViewModel>(view.Model);
        Assert.Single(vm.Solicitudes);
        Assert.Equal("SOL-B1", vm.Solicitudes[0].Codigo);

        _solicitudes.Verify(s => s.GetListForBodegaAsync(1, true, It.IsAny<CancellationToken>()), Times.Once);
        _solicitudes.Verify(s => s.GetListForBodegaAsync(2, It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _solicitudes.Verify(s => s.GetListForBodegaAsync(null, It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Index_BodegueroSinBodegaAsignada_BloqueaConMensaje()
    {
        _bodega.SetupGet(a => a.BodegaId).Returns(0);

        var controller = CreateController(Principal(5));
        var result = await controller.Index(estado: null, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<BodegaSolicitudesIndexViewModel>(view.Model);
        Assert.Empty(vm.Solicitudes);
        Assert.False(vm.IsSuccess);
        Assert.Contains("bodega asignada", vm.Message, StringComparison.OrdinalIgnoreCase);

        _solicitudes.Verify(
            s => s.GetListForBodegaAsync(It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
