using Moq;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class AlertSendTestTests
{
    [Fact]
    public async Task SendTestAsync_UsuarioActivo_RegistraEntrega()
    {
        var alerts = new Mock<IAlertRepository>();
        var users = new Mock<IUserRepository>();
        var materials = new Mock<IMaterialRepository>();
        var requests = new Mock<IMaterialRequestRepository>();
        var orders = new Mock<IProductionOrderRepository>();
        var quality = new Mock<IQualityRepository>();
        var email = new Mock<IEmailSender>();
        var uow = new Mock<IUnitOfWork>();

        users.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 3, Nombre = "Ana", Email = "ana@sipitex.test", IsActive = true });
        email.SetupGet(e => e.IsSmtpConfigured).Returns(false);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new AlertService(
            alerts.Object, users.Object, materials.Object, requests.Object,
            orders.Object, quality.Object, email.Object, uow.Object);

        var result = await sut.SendTestAsync(3);

        Assert.True(result.Success);
        Assert.Contains("Outbox", result.Message);
        email.Verify(e => e.SendAsync("ana@sipitex.test", "Ana", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        alerts.Verify(r => r.AddDeliveryAsync(It.Is<AlertDelivery>(d => d.UserId == 3 && d.Channel == "Outbox"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
