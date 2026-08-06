using Moq;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class AlertNotifyUsersTests
{
    private readonly Mock<IAlertRepository> _alerts = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<IMaterialRequestRepository> _requests = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IQualityRepository> _quality = new();
    private readonly Mock<IEmailSender> _email = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private AlertService CreateSut() => new(
        _alerts.Object,
        _users.Object,
        _materials.Object,
        _requests.Object,
        _orders.Object,
        _quality.Object,
        _email.Object,
        _uow.Object);

    [Fact]
    public async Task NotifyUsersAsync_PorRol_SoloEnviaSiPreferenciaEnabled()
    {
        var bodegueroOn = new User
        {
            Id = 1,
            Nombre = "Bodega On",
            Email = "on@test.com",
            Rol = UserRoles.Bodeguero,
            IsActive = true
        };
        var bodegueroOff = new User
        {
            Id = 2,
            Nombre = "Bodega Off",
            Email = "off@test.com",
            Rol = UserRoles.Bodeguero,
            IsActive = true
        };

        _users.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([bodegueroOn, bodegueroOff]);
        _alerts.Setup(r => r.EnsureDefaultPreferencesAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _alerts.Setup(r => r.GetEnabledPreferencesAsync(AlertType.SolicitudMaterialNueva, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new AlertPreference
                {
                    UserId = 1,
                    User = bodegueroOn,
                    AlertType = AlertType.SolicitudMaterialNueva,
                    Enabled = true
                }
            ]);
        _email.SetupGet(e => e.IsSmtpConfigured).Returns(false);
        _alerts.Setup(r => r.AddDeliveryAsync(It.IsAny<AlertDelivery>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sent = await CreateSut().NotifyUsersAsync(
            AlertType.SolicitudMaterialNueva,
            "Asunto",
            "Cuerpo",
            userIds: null,
            role: UserRoles.Bodeguero);

        Assert.Equal(1, sent);
        _email.Verify(e => e.SendAsync("on@test.com", "Bodega On", "Asunto", "Cuerpo", It.IsAny<CancellationToken>()), Times.Once);
        _email.Verify(e => e.SendAsync("off@test.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _alerts.Verify(r => r.AddDeliveryAsync(
            It.Is<AlertDelivery>(d => d.UserId == 1 && d.AlertType == AlertType.SolicitudMaterialNueva && d.Channel == "Outbox"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyUsersAsync_PorUserIds_RespetaPreferenciaDeshabilitada()
    {
        var solicitante = new User
        {
            Id = 10,
            Nombre = "Laura",
            Email = "laura@test.com",
            Rol = UserRoles.Instructor,
            IsActive = true
        };

        _users.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(solicitante);
        _alerts.Setup(r => r.EnsureDefaultPreferencesAsync(solicitante, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        // Preferencia deshabilitada → GetEnabled no lo incluye
        _alerts.Setup(r => r.GetEnabledPreferencesAsync(AlertType.SolicitudMaterialResuelta, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sent = await CreateSut().NotifyUsersAsync(
            AlertType.SolicitudMaterialResuelta,
            "Resuelta",
            "Ok",
            userIds: [10]);

        Assert.Equal(0, sent);
        _email.Verify(
            e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
