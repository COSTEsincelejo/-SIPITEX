using Moq;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class AlertStockYSolicitudesTests
{
    private readonly Mock<IAlertRepository> _alerts = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<ISolicitudMaterialRepository> _solicitudes = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IQualityRepository> _quality = new();
    private readonly Mock<IEmailSender> _email = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private string? _body;

    public AlertStockYSolicitudesTests()
    {
        _orders.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _quality.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _solicitudes.Setup(r => r.GetAllWithFichaAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _email.Setup(e => e.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string, CancellationToken>((_, _, _, body, _) => _body = body)
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task StockCeroYMinimoCero_NoEsCritico_YNoDisparaCorreo()
    {
        _materials.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([Material("Guata", stock: 0, min: 0)]);
        Suscribir(AlertType.StockBajo);

        var result = await CreateSut().EvaluateAndSendAsync();

        Assert.Equal(0, result.AlertsFound);
        Assert.Equal(0, result.EmailsSent);
        _email.Verify(
            e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task MinimoDefinidoYStockBajo_DisparaCorreo_SinIncluirSinMinimo()
    {
        _materials.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Material("Hilo", stock: 2, min: 5),
                Material("Guata", stock: 0, min: 0)
            ]);
        Suscribir(AlertType.StockBajo);

        var result = await CreateSut().EvaluateAndSendAsync();

        Assert.Equal(1, result.AlertsFound);
        Assert.Equal(1, result.EmailsSent);
        Assert.Contains("Hilo", _body);
        Assert.Contains("Bajo", _body);
        Assert.DoesNotContain("Guata", _body);
    }

    [Fact]
    public async Task MinimoDefinidoYStockCero_DisparaCorreoCritico()
    {
        _materials.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([Material("Botón", stock: 0, min: 4)]);
        Suscribir(AlertType.StockBajo);

        var result = await CreateSut().EvaluateAndSendAsync();

        Assert.Equal(1, result.EmailsSent);
        Assert.Contains("Botón", _body);
        Assert.Contains("Crítico", _body);
    }

    [Fact]
    public async Task SolicitudPendiente_CuentaSolicitudMaterial_NoElEstadoResuelto()
    {
        _materials.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _solicitudes.Setup(r => r.GetAllWithFichaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new SolicitudMaterial
                {
                    Id = 1,
                    Codigo = "SOL-0007",
                    Estado = SolicitudMaterialEstado.Pendiente,
                    Ficha = new Ficha { NumeroGrupo = "FICHA-T1" }
                },
                new SolicitudMaterial
                {
                    Id = 2,
                    Codigo = "SOL-0008",
                    Estado = SolicitudMaterialEstado.AprobadaTotal
                }
            ]);
        Suscribir(AlertType.SolicitudPendiente);

        var result = await CreateSut().EvaluateAndSendAsync();

        Assert.Equal(1, result.AlertsFound);
        Assert.Equal(1, result.EmailsSent);
        Assert.Contains("SOL-0007", _body);
        Assert.Contains("FICHA-T1", _body);
        Assert.DoesNotContain("SOL-0008", _body);
    }

    private void Suscribir(AlertType type)
    {
        _alerts.Setup(r => r.GetEnabledPreferencesAsync(type, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new AlertPreference
                {
                    UserId = 1,
                    AlertType = type,
                    Enabled = true,
                    User = new User
                    {
                        Id = 1,
                        Nombre = "Bodega",
                        Email = "bodega@sipitex.test",
                        IsActive = true
                    }
                }
            ]);
    }

    private static Material Material(string name, decimal stock, decimal min) => new()
    {
        Name = name,
        Stock = stock,
        MinStock = min,
        Unit = MaterialUnit.Unidades
    };

    private AlertService CreateSut() => new(
        _alerts.Object,
        _users.Object,
        _materials.Object,
        _solicitudes.Object,
        _orders.Object,
        _quality.Object,
        _email.Object,
        _uow.Object);
}
