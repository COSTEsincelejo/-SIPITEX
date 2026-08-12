using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;
using Sipitex.Infrastructure.Services;
using Sipitex.Web.Controllers;
using Sipitex.Web.Models;

namespace Sipitex.Tests;

/// <summary>
/// ActivityLog global (PoC): LogAsync + instrumentación CreateUser/DeleteUser/ToggleUserStatus.
/// </summary>
public class ActivityLogServiceTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"sipitex-activity-{Guid.NewGuid():N}.db");
    private readonly SipitexDbContext _db;
    private readonly Mock<IUserRepository> _users = new();

    public ActivityLogServiceTests()
    {
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        _db = new SipitexDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    private ActivityLogService CreateSut() => new(_db, _users.Object);

    [Fact]
    public async Task LogAsync_PersistsRowWithUserNameSnapshot()
    {
        _users.Setup(u => u.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 7,
                Nombre = "Admin Demo",
                Email = "admin@sipitex.test",
                PasswordHash = "x",
                Rol = UserRoles.Administrador
            });

        await CreateSut().LogAsync(
            userId: 7,
            action: "CreateUser",
            entity: "User",
            entityId: "nuevo@sipitex.test",
            details: "Rol=Instructor");

        var row = Assert.Single(_db.ActivityLogs.AsNoTracking().ToList());
        Assert.Equal(7, row.UserId);
        Assert.Equal("Admin Demo", row.UserName);
        Assert.Equal("CreateUser", row.Action);
        Assert.Equal("User", row.Entity);
        Assert.Equal("nuevo@sipitex.test", row.EntityId);
        Assert.Equal("Rol=Instructor", row.Details);
        Assert.True(row.Timestamp <= DateTime.UtcNow.AddMinutes(1));
        Assert.True(row.Timestamp >= DateTime.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public async Task LogAsync_WhenUserMissing_UsesHashFallbackName()
    {
        _users.Setup(u => u.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await CreateSut().LogAsync(99, "DeleteUser", "User", "5");

        var row = Assert.Single(_db.ActivityLogs.AsNoTracking().ToList());
        Assert.Equal("#99", row.UserName);
    }

    [Fact]
    public async Task LogAsync_InvalidUserIdOrAction_DoesNotPersist()
    {
        await CreateSut().LogAsync(0, "CreateUser", "User", "1");
        await CreateSut().LogAsync(1, "", "User", "1");
        await CreateSut().LogAsync(1, "CreateUser", "  ", "1");

        Assert.Empty(_db.ActivityLogs.AsNoTracking().ToList());
        _users.Verify(u => u.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class AccountActivityLogInstrumentationTests
{
    private readonly Mock<IUserAccountService> _accounts = new();
    private readonly Mock<IPasswordResetService> _passwordReset = new();
    private readonly Mock<IFuncionalidadesReportService> _funcionalidades = new();
    private readonly Mock<IActivityLogService> _activity = new();
    private readonly Mock<IWebHostEnvironment> _env = new();

    private AccountController CreateController(int actorId = 1, string actorName = "Admin")
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, actorId.ToString()),
            new Claim(ClaimTypes.Name, actorName),
            new Claim(ClaimTypes.Role, UserRoles.Administrador)
        ], "Test");

        var controller = new AccountController(
            _accounts.Object,
            _passwordReset.Object,
            _funcionalidades.Object,
            _activity.Object,
            _env.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
        controller.TempData = new TempDataDictionary(
            controller.HttpContext,
            Mock.Of<ITempDataProvider>());
        return controller;
    }

    [Fact]
    public async Task CreateUser_OnSuccess_LogsActivity()
    {
        _accounts.Setup(s => s.CreateUserAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Ok("Usuario creado correctamente."));
        _accounts.Setup(s => s.GetUserByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // GetFichasAsync uses IFichaService from RequestServices — stub empty provider
        var services = new Mock<IServiceProvider>();
        services.Setup(s => s.GetService(typeof(IFichaService))).Returns(null!);
        var controller = CreateController();
        controller.ControllerContext.HttpContext.RequestServices = services.Object;

        // Avoid null FichaService: mock GetService to return a ficha service
        var fichas = new Mock<IFichaService>();
        fichas.Setup(f => f.GetFichasAsync(
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        services.Setup(s => s.GetService(typeof(IFichaService))).Returns(fichas.Object);

        var model = new UserEditViewModel
        {
            Nombre = "Laura",
            Email = "laura@sipitex.test",
            Password = "Clave123!",
            Rol = UserRoles.Instructor
        };

        // RedirectToAction necesita IUrlHelperFactory; solo nos importa el LogAsync
        try
        {
            await controller.CreateUser(model, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // esperado en unit test sin pipeline MVC completo
        }

        _activity.Verify(a => a.LogAsync(
            1,
            "CreateUser",
            "User",
            "laura@sipitex.test",
            It.Is<string?>(d => d != null && d.Contains("Instructor", StringComparison.Ordinal)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUser_OnFailure_DoesNotLog()
    {
        _accounts.Setup(s => s.CreateUserAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Fail("Ya existe un usuario con ese correo."));

        var services = new Mock<IServiceProvider>();
        var fichas = new Mock<IFichaService>();
        fichas.Setup(f => f.GetFichasAsync(
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        services.Setup(s => s.GetService(typeof(IFichaService))).Returns(fichas.Object);

        var controller = CreateController();
        controller.ControllerContext.HttpContext.RequestServices = services.Object;

        var model = new UserEditViewModel
        {
            Nombre = "Laura",
            Email = "laura@sipitex.test",
            Password = "Clave123!",
            Rol = UserRoles.Instructor
        };

        await controller.CreateUser(model, CancellationToken.None);

        _activity.Verify(a => a.LogAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ToggleUserStatus_OnSuccess_LogsActivity()
    {
        _accounts.Setup(s => s.ToggleUserStatusAsync(5, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Ok("Usuario desactivado."));

        var controller = CreateController();
        await controller.ToggleUserStatus(5, isActive: false, CancellationToken.None);

        _activity.Verify(a => a.LogAsync(
            1,
            "ToggleUserStatus",
            "User",
            "5",
            "IsActive=false",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_OnSuccess_LogsActivityWithTargetSnapshot()
    {
        _accounts.Setup(s => s.GetUserByIdAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 8,
                Nombre = "Temp",
                Email = "temp@sipitex.test",
                PasswordHash = "x",
                Rol = UserRoles.Instructor
            });
        _accounts.Setup(s => s.DeleteUserAsync(8, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Ok("Usuario «Temp» eliminado."));

        var controller = CreateController();
        await controller.DeleteUser(8, CancellationToken.None);

        _activity.Verify(a => a.LogAsync(
            1,
            "DeleteUser",
            "User",
            "8",
            It.Is<string?>(d => d != null
                && d.Contains("temp@sipitex.test", StringComparison.Ordinal)
                && d.Contains("Instructor", StringComparison.Ordinal)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_OnFailure_DoesNotLog()
    {
        _accounts.Setup(s => s.GetUserByIdAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 8,
                Nombre = "Temp",
                Email = "temp@sipitex.test",
                PasswordHash = "x",
                Rol = UserRoles.Instructor
            });
        _accounts.Setup(s => s.DeleteUserAsync(8, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Fail("No se puede eliminar: dependencias."));

        var controller = CreateController();
        await controller.DeleteUser(8, CancellationToken.None);

        _activity.Verify(a => a.LogAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
