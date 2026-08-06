using Microsoft.EntityFrameworkCore;
using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests.Integration;

/// <summary>
/// Tests de integración punta a punta del flujo SolicitudMaterial.
/// Cada test usa su propio SQLite en %TEMP% vía <see cref="SolicitudMaterialFlowFixture"/>.
///
/// Cómo correr solo esta suite:
///   dotnet test --filter FullyQualifiedName~Integration
/// </summary>
public class SolicitudMaterialFlowTests
{
    [Fact]
    public async Task FlujoFelizCompleto_ApruebaTotal_EntregaYNotificaciones()
    {
        await using var fx = await SolicitudMaterialFlowFixture.CreateAsync();

        var create = await fx.SolicitudService.CreateAsync(
            new CreateSolicitudMaterialDto(
                fx.FichaAsignadaId,
                [
                    new CreateDetalleSolicitudDto(fx.MaterialAmplioId, 40),
                    new CreateDetalleSolicitudDto(fx.MaterialJustoId, 3)
                ]),
            fx.InstructorId,
            UserRoles.Instructor,
            "Instructor Test");

        Assert.True(create.Success, create.Message);

        var solicitud = await fx.Context.SolicitudesMaterial
            .Include(s => s.Detalles)
            .AsNoTracking()
            .SingleAsync();

        Assert.StartsWith("SOL-", solicitud.Codigo);
        Assert.Equal(SolicitudMaterialEstado.Pendiente, solicitud.Estado);
        Assert.Equal(2, solicitud.Detalles.Count);

        // Notificación a Bodeguero: email mock + AlertDelivery persistido
        fx.EmailMock.Verify(e => e.SendAsync(
            "bodega.int@test.local",
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains(solicitud.Codigo)),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        var alertNueva = await fx.Context.AlertDeliveries.AsNoTracking()
            .Where(a => a.AlertType == AlertType.SolicitudMaterialNueva)
            .ToListAsync();
        Assert.Single(alertNueva);
        Assert.Equal(fx.BodegueroId, alertNueva[0].UserId);

        var items = solicitud.Detalles
            .Select(d => new ResolveDetalleDto(d.Id, d.CantidadSolicitada))
            .ToList();

        var resolve = await fx.ApprovalService.ResolveSolicitudAsync(
            solicitud.Id, items, fx.BodegueroId);

        Assert.True(resolve.Success, resolve.Message);

        // Relectura fresca desde disco (sin tracker) para asserts finales
        fx.Context.ChangeTracker.Clear();

        var after = await fx.Context.SolicitudesMaterial
            .Include(s => s.Detalles)
            .Include(s => s.Entrega)
            .AsNoTracking()
            .SingleAsync(s => s.Id == solicitud.Id);

        Assert.Equal(SolicitudMaterialEstado.AprobadaTotal, after.Estado);
        Assert.NotNull(after.Entrega);
        Assert.StartsWith("ENT-", after.Entrega!.Codigo);
        Assert.Equal(fx.BodegueroId, after.Entrega.BodegueroId);

        Assert.Equal(
            fx.StockAmplioInicial - 40,
            await fx.GetMaterialStockAsync(fx.MaterialAmplioId));
        Assert.Equal(
            fx.StockJustoInicial - 3,
            await fx.GetMaterialStockAsync(fx.MaterialJustoId));

        fx.EmailMock.Verify(e => e.SendAsync(
            "instructor.int@test.local",
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains(solicitud.Codigo)),
            It.Is<string>(b => b.Contains(after.Entrega.Codigo, StringComparison.Ordinal)
                               || b.Contains("Aprobada", StringComparison.OrdinalIgnoreCase)),
            It.IsAny<CancellationToken>()), Times.Once);

        var alertResuelta = await fx.Context.AlertDeliveries.AsNoTracking()
            .Where(a => a.AlertType == AlertType.SolicitudMaterialResuelta)
            .ToListAsync();
        Assert.Single(alertResuelta);
        Assert.Equal(fx.InstructorId, alertResuelta[0].UserId);
    }

    [Fact]
    public async Task AprobacionParcial_PorStockInsuficiente_GeneraEntrega()
    {
        await using var fx = await SolicitudMaterialFlowFixture.CreateAsync();

        // Solicita 40 del amplio (ok) y 20 del justo (stock 5 → aprobará 5)
        var create = await fx.SolicitudService.CreateAsync(
            new CreateSolicitudMaterialDto(
                fx.FichaAsignadaId,
                [
                    new CreateDetalleSolicitudDto(fx.MaterialAmplioId, 40),
                    new CreateDetalleSolicitudDto(fx.MaterialJustoId, 20)
                ]),
            fx.InstructorId,
            UserRoles.Instructor,
            "Instructor Test");
        Assert.True(create.Success, create.Message);

        var solicitud = await fx.Context.SolicitudesMaterial
            .Include(s => s.Detalles)
            .AsNoTracking()
            .SingleAsync();

        var dAmplio = solicitud.Detalles.Single(d => d.MaterialId == fx.MaterialAmplioId);
        var dJusto = solicitud.Detalles.Single(d => d.MaterialId == fx.MaterialJustoId);

        var resolve = await fx.ApprovalService.ResolveSolicitudAsync(
            solicitud.Id,
            [
                new ResolveDetalleDto(dAmplio.Id, 40),
                new ResolveDetalleDto(dJusto.Id, 5) // parcial: stock justo
            ],
            fx.BodegueroId);

        Assert.True(resolve.Success, resolve.Message);

        fx.Context.ChangeTracker.Clear();
        var after = await fx.Context.SolicitudesMaterial
            .Include(s => s.Detalles)
            .Include(s => s.Entrega)
            .AsNoTracking()
            .SingleAsync(s => s.Id == solicitud.Id);

        Assert.Equal(SolicitudMaterialEstado.AprobadaParcial, after.Estado);
        Assert.NotNull(after.Entrega);
        Assert.Equal(
            DetalleSolicitudEstado.Aprobado,
            after.Detalles.Single(d => d.Id == dAmplio.Id).EstadoItem);
        Assert.Equal(
            DetalleSolicitudEstado.AprobadoParcial,
            after.Detalles.Single(d => d.Id == dJusto.Id).EstadoItem);
        Assert.Equal(fx.StockAmplioInicial - 40, await fx.GetMaterialStockAsync(fx.MaterialAmplioId));
        Assert.Equal(0m, await fx.GetMaterialStockAsync(fx.MaterialJustoId));
    }

    [Fact]
    public async Task RechazoTotal_SinEntregaNiDescuentoDeStock()
    {
        await using var fx = await SolicitudMaterialFlowFixture.CreateAsync();

        var create = await fx.SolicitudService.CreateAsync(
            new CreateSolicitudMaterialDto(
                fx.FichaAsignadaId,
                [
                    new CreateDetalleSolicitudDto(fx.MaterialAmplioId, 10),
                    new CreateDetalleSolicitudDto(fx.MaterialJustoId, 2)
                ]),
            fx.InstructorId,
            UserRoles.Instructor,
            "Instructor Test");
        Assert.True(create.Success, create.Message);

        var solicitud = await fx.Context.SolicitudesMaterial
            .Include(s => s.Detalles)
            .AsNoTracking()
            .SingleAsync();

        var resolve = await fx.ApprovalService.ResolveSolicitudAsync(
            solicitud.Id,
            solicitud.Detalles.Select(d => new ResolveDetalleDto(d.Id, 0)).ToList(),
            fx.BodegueroId);

        Assert.True(resolve.Success, resolve.Message);

        fx.Context.ChangeTracker.Clear();
        var after = await fx.Context.SolicitudesMaterial
            .Include(s => s.Entrega)
            .AsNoTracking()
            .SingleAsync(s => s.Id == solicitud.Id);

        Assert.Equal(SolicitudMaterialEstado.Rechazada, after.Estado);
        Assert.Null(after.Entrega);
        Assert.Equal(0, await fx.Context.EntregasMaterial.CountAsync());
        Assert.Equal(fx.StockAmplioInicial, await fx.GetMaterialStockAsync(fx.MaterialAmplioId));
        Assert.Equal(fx.StockJustoInicial, await fx.GetMaterialStockAsync(fx.MaterialJustoId));

        var alert = await fx.Context.AlertDeliveries.AsNoTracking()
            .SingleAsync(a => a.AlertType == AlertType.SolicitudMaterialResuelta);
        Assert.Contains("Rechazada", alert.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(fx.InstructorId, alert.UserId);
    }

    [Fact]
    public async Task SobreAprobacion_FallaSinCambios()
    {
        await using var fx = await SolicitudMaterialFlowFixture.CreateAsync();

        var create = await fx.SolicitudService.CreateAsync(
            new CreateSolicitudMaterialDto(
                fx.FichaAsignadaId,
                [new CreateDetalleSolicitudDto(fx.MaterialJustoId, 20)]),
            fx.InstructorId,
            UserRoles.Instructor,
            "Instructor Test");
        Assert.True(create.Success, create.Message);

        var solicitud = await fx.Context.SolicitudesMaterial
            .Include(s => s.Detalles)
            .AsNoTracking()
            .SingleAsync();
        var detalleId = solicitud.Detalles.Single().Id;

        var resolve = await fx.ApprovalService.ResolveSolicitudAsync(
            solicitud.Id,
            [new ResolveDetalleDto(detalleId, 10)], // stock solo 5
            fx.BodegueroId);

        Assert.False(resolve.Success);

        fx.Context.ChangeTracker.Clear();
        var after = await fx.Context.SolicitudesMaterial.AsNoTracking()
            .SingleAsync(s => s.Id == solicitud.Id);
        Assert.Equal(SolicitudMaterialEstado.Pendiente, after.Estado);
        Assert.Equal(0, await fx.Context.EntregasMaterial.CountAsync());
        Assert.Equal(fx.StockJustoInicial, await fx.GetMaterialStockAsync(fx.MaterialJustoId));
        Assert.Equal(
            0,
            await fx.Context.AlertDeliveries.CountAsync(a => a.AlertType == AlertType.SolicitudMaterialResuelta));
    }

    [Fact]
    public async Task DobleResolucion_SegundoIntentoFallaSinAlterar()
    {
        await using var fx = await SolicitudMaterialFlowFixture.CreateAsync();

        var create = await fx.SolicitudService.CreateAsync(
            new CreateSolicitudMaterialDto(
                fx.FichaAsignadaId,
                [new CreateDetalleSolicitudDto(fx.MaterialAmplioId, 10)]),
            fx.InstructorId,
            UserRoles.Instructor,
            "Instructor Test");
        Assert.True(create.Success, create.Message);

        var solicitud = await fx.Context.SolicitudesMaterial
            .Include(s => s.Detalles)
            .AsNoTracking()
            .SingleAsync();
        var detalleId = solicitud.Detalles.Single().Id;

        var first = await fx.ApprovalService.ResolveSolicitudAsync(
            solicitud.Id,
            [new ResolveDetalleDto(detalleId, 10)],
            fx.BodegueroId);
        Assert.True(first.Success, first.Message);

        fx.Context.ChangeTracker.Clear();
        var snapshot = await fx.Context.SolicitudesMaterial
            .Include(s => s.Entrega)
            .AsNoTracking()
            .SingleAsync(s => s.Id == solicitud.Id);
        var stockAfterFirst = await fx.GetMaterialStockAsync(fx.MaterialAmplioId);
        var entregaCodigo = snapshot.Entrega!.Codigo;

        var second = await fx.ApprovalService.ResolveSolicitudAsync(
            solicitud.Id,
            [new ResolveDetalleDto(detalleId, 10)],
            fx.BodegueroId);
        Assert.False(second.Success);

        fx.Context.ChangeTracker.Clear();
        var after = await fx.Context.SolicitudesMaterial
            .Include(s => s.Entrega)
            .AsNoTracking()
            .SingleAsync(s => s.Id == solicitud.Id);

        Assert.Equal(SolicitudMaterialEstado.AprobadaTotal, after.Estado);
        Assert.Equal(entregaCodigo, after.Entrega!.Codigo);
        Assert.Equal(stockAfterFirst, await fx.GetMaterialStockAsync(fx.MaterialAmplioId));
        Assert.Equal(1, await fx.Context.EntregasMaterial.CountAsync());
    }

    [Fact]
    public async Task PermisosCruzados_InstructorEnFichaAjena_NoCreaRegistro()
    {
        await using var fx = await SolicitudMaterialFlowFixture.CreateAsync();

        var create = await fx.SolicitudService.CreateAsync(
            new CreateSolicitudMaterialDto(
                fx.FichaAjenaId,
                [new CreateDetalleSolicitudDto(fx.MaterialAmplioId, 5)]),
            fx.InstructorId,
            UserRoles.Instructor,
            "Instructor Test");

        Assert.False(create.Success);
        Assert.Equal(0, await fx.Context.SolicitudesMaterial.CountAsync());
        Assert.Equal(0, await fx.Context.DetallesSolicitudMaterial.CountAsync());
        Assert.Equal(
            0,
            await fx.Context.AlertDeliveries.CountAsync(a => a.AlertType == AlertType.SolicitudMaterialNueva));
    }

    [Fact]
    public async Task UnicidadDeCodigos_SecuenciaRapida_SinColisiones()
    {
        await using var fx = await SolicitudMaterialFlowFixture.CreateAsync();

        var codigos = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            var result = await fx.SolicitudService.CreateAsync(
                new CreateSolicitudMaterialDto(
                    fx.FichaAsignadaId,
                    [new CreateDetalleSolicitudDto(fx.MaterialAmplioId, 1)]),
                fx.InstructorId,
                UserRoles.Instructor,
                "Instructor Test");
            Assert.True(result.Success, result.Message);
        }

        fx.Context.ChangeTracker.Clear();
        codigos = await fx.Context.SolicitudesMaterial.AsNoTracking()
            .Select(s => s.Codigo)
            .ToListAsync();

        Assert.Equal(5, codigos.Count);
        Assert.Equal(codigos.Distinct(StringComparer.Ordinal).Count(), codigos.Count);
        Assert.All(codigos, c => Assert.StartsWith("SOL-", c));
        Assert.Contains("SOL-0001", codigos);
        Assert.Contains("SOL-0005", codigos);
    }
}
