using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Reporting;
using Sipitex.Web.Controllers;

namespace Sipitex.Tests;

public class VerificarActaTests
{
    private readonly Mock<IActaMovimientoRepository> _actas = new();
    private readonly ActaPdfService _pdf = new();

    private ActaMovimientoService CreateSut() => new(
        _actas.Object,
        Mock.Of<IStockMovementRepository>(),
        Mock.Of<IConsumoMaterialRepository>(),
        Mock.Of<IProductionOrderRepository>(),
        Mock.Of<IUserRepository>(),
        _pdf,
        Mock.Of<IUnitOfWork>());

    [Fact]
    public async Task CodigoCorrecto_DevuelveSoloLosDatosPublicos()
    {
        var acta = ActaDePrueba();
        _actas.Setup(r => r.GetByNumeroAsync("ACT-0007", It.IsAny<CancellationToken>())).ReturnsAsync(acta);
        var codigo = ActaPdfService.CodigoVerificacion(MapaPublico(acta));

        var resultado = await CreateSut().VerificarPublicoAsync("ACT-0007", codigo.ToUpperInvariant());

        Assert.True(resultado.Autentica);
        Assert.Equal("ACT-0007", resultado.Numero);
        Assert.Equal(ActaTipo.Egreso, resultado.Tipo);
        Assert.Equal(ActaEstado.PendienteFirma, resultado.Estado);
        Assert.Equal(1, resultado.CantidadItems);
        Assert.Equal(acta.FechaUtc, resultado.FechaUtc);
    }

    [Fact]
    public async Task CodigoIncorrecto_YActaInexistente_DanElMismoResultado()
    {
        var acta = ActaDePrueba();
        _actas.Setup(r => r.GetByNumeroAsync("ACT-0007", It.IsAny<CancellationToken>())).ReturnsAsync(acta);
        _actas.Setup(r => r.GetByNumeroAsync("ACT-NO", It.IsAny<CancellationToken>())).ReturnsAsync((ActaMovimiento?)null);

        var mal = await CreateSut().VerificarPublicoAsync("ACT-0007", "no-es-el-codigo");
        var ausente = await CreateSut().VerificarPublicoAsync("ACT-NO", "no-es-el-codigo");

        Assert.False(mal.Autentica);
        Assert.False(ausente.Autentica);
        Assert.Null(mal.Numero);
        Assert.Null(ausente.Numero);
        Assert.Null(mal.Tipo);
        Assert.Null(ausente.Tipo);
        Assert.Null(mal.Estado);
        Assert.Null(ausente.Estado);
        Assert.Equal(0, mal.CantidadItems);
        Assert.Equal(0, ausente.CantidadItems);
    }

    [Fact]
    public void PaginaPublica_EsAnonima_SinAntiforgery_YConLimite()
    {
        var clase = typeof(VerificarController);
        Assert.NotNull(clase.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.NotNull(clase.GetCustomAttribute<IgnoreAntiforgeryTokenAttribute>());
        var metodo = clase.GetMethod(nameof(VerificarController.Acta));
        var limite = metodo!.GetCustomAttribute<EnableRateLimitingAttribute>();
        Assert.Equal(VerificarController.PoliticaLimite, limite!.PolicyName);
    }

    private static ActaMovimiento ActaDePrueba() => new()
    {
        Id = 7,
        Numero = "ACT-0007",
        Tipo = ActaTipo.Egreso,
        Origen = ActaOrigen.Manual,
        FechaUtc = new DateTime(2026, 11, 9, 18, 0, 0, DateTimeKind.Utc),
        Estado = ActaEstado.PendienteFirma,
        Observaciones = "No debe salir en la verificación",
        EntregaNombre = "Ana Secreta",
        EntregaCargo = "Administrador",
        RecibeNombre = "Pedro Secreto",
        RecibeCargo = "Bodega",
        CreadoPorUserId = 1,
        Detalles =
        [
            new ActaMovimientoDetalle
            {
                Id = 1,
                ItemTipo = ActaItemTipo.Material,
                Descripcion = "Tela secreta",
                Cantidad = 4,
                Unidad = "metro"
            }
        ]
    };

    private static Application.DTOs.ActaMovimientoDto MapaPublico(ActaMovimiento a) => new(
        a.Id,
        a.Numero,
        a.Tipo,
        a.Origen,
        a.FechaUtc,
        a.Observaciones,
        a.ProductionOrderId,
        null,
        a.EstadoProductoOrigen,
        a.EstadoProductoDestino,
        a.EntregaNombre,
        a.EntregaCargo,
        a.EntregaConformidadUtc,
        a.EntregaFirmaPng,
        a.RecibeNombre,
        a.RecibeCargo,
        a.RecibeConformidadUtc,
        a.RecibeFirmaPng,
        a.CreadoPorUserId,
        "#1",
        a.Detalles.Select(d => new Application.DTOs.ActaDetalleDto(
            d.Id, d.ItemTipo, d.Descripcion, d.Cantidad, d.Unidad,
            d.MaterialId, d.ConsumoMaterialId, d.StockMovementId, d.ProductionOrderId)).ToList(),
        Estado: a.Estado);
}
