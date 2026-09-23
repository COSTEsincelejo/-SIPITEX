using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;
using Sipitex.Infrastructure.Repositories;
using Sipitex.Web.Controllers;
using Sipitex.Web.Models;

namespace Sipitex.Tests;

public class EmailConfirmationFlowTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"sipitex-email-{Guid.NewGuid():N}.db");

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        foreach (var suffix in new[] { "-shm", "-wal" })
        {
            var side = _dbPath + suffix;
            if (File.Exists(side)) File.Delete(side);
        }
    }

    private SipitexDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        var db = new SipitexDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task AltaSinConfirmar_NoEntraHastaElCodigo_YElCodigoNoQuedaEnClaro()
    {
        await using var db = CreateDb();
        var pending = new User
        {
            Nombre = "Nuevo Instructor",
            Email = "nuevo@sipitex.test",
            PasswordHash = PasswordHasher.Hash("Clave123!"),
            Rol = UserRoles.Instructor,
            PermisosExtendidos = string.Empty,
            IsActive = true,
            EmailConfirmed = false
        };
        var legacy = new User
        {
            Nombre = "Cuenta previa",
            Email = "previa@sipitex.test",
            PasswordHash = PasswordHasher.Hash("Clave123!"),
            Rol = UserRoles.Instructor,
            PermisosExtendidos = string.Empty,
            IsActive = true
        };
        db.Users.AddRange(pending, legacy);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var storedPending = await db.Users.SingleAsync(u => u.Email == pending.Email);
        var storedLegacy = await db.Users.SingleAsync(u => u.Email == legacy.Email);
        Assert.False(storedPending.EmailConfirmed);
        Assert.True(storedLegacy.EmailConfirmed);

        string? body = null;
        var email = new Mock<IEmailSender>();
        email.Setup(e => e.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string, CancellationToken>((_, _, _, text, _) => body = text)
            .Returns(Task.CompletedTask);

        var users = new UserRepository(db);
        var reset = new PasswordResetService(users, new PasswordResetTokenRepository(db), new UnitOfWork(db), email.Object);
        var accounts = new UserAccountService(
            users,
            Mock.Of<IFichaRepository>(),
            Mock.Of<IPlantaInventarioRepository>(),
            new UnitOfWork(db),
            reset,
            NullLogger<UserAccountService>.Instance);

        var sent = await reset.SendEmailConfirmationAsync(storedPending);
        Assert.True(sent.Success);
        var code = Regex.Match(body ?? string.Empty, @"(?m)^\s*(\d{6})\s*$").Groups[1].Value;
        Assert.Matches(@"^\d{6}$", code);
        Assert.DoesNotContain(code, sent.Message ?? string.Empty);

        var blocked = await accounts.AuthenticateAsync(pending.Email, "Clave123!");
        Assert.NotNull(blocked);
        Assert.False(blocked!.EmailConfirmed);

        var wrong = await reset.ConfirmEmailAsync(pending.Email, code == "000000" ? "111111" : "000000");
        Assert.False(wrong.Success);
        Assert.Equal(PasswordResetService.InvalidCodeMessage, wrong.Message);

        var confirmed = await reset.ConfirmEmailAsync(pending.Email, code);
        Assert.True(confirmed.Success);
        Assert.DoesNotContain(code, confirmed.Message);

        db.ChangeTracker.Clear();
        var allowed = await accounts.AuthenticateAsync(pending.Email, "Clave123!");
        Assert.NotNull(allowed);
        Assert.True(allowed!.EmailConfirmed);

        var reused = await reset.ConfirmEmailAsync(pending.Email, code);
        Assert.True(reused.Success);
        Assert.Contains("ya está confirmado", reused.Message, StringComparison.OrdinalIgnoreCase);

        var hashes = await db.PasswordResetTokens.Select(t => t.TokenHash).ToListAsync();
        Assert.NotEmpty(hashes);
        Assert.All(hashes, hash =>
        {
            Assert.NotEqual(code, hash);
            Assert.Equal(64, hash.Length);
        });
        var logs = await db.ActivityLogs.ToListAsync();
        Assert.DoesNotContain(logs, log =>
            (log.Details ?? string.Empty).Contains(code, StringComparison.Ordinal)
            || log.Action.Contains(code, StringComparison.Ordinal)
            || (log.EntityId ?? string.Empty).Contains(code, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Login_ConCorreoSinConfirmar_NoRegistraFallo_YNoAbreSesion()
    {
        var accounts = new Mock<IUserAccountService>();
        var guard = new Mock<ILoginAttemptGuard>();
        var user = new User
        {
            Id = 4,
            Nombre = "Nuevo",
            Email = "nuevo@sipitex.test",
            Rol = UserRoles.Instructor,
            IsActive = true,
            EmailConfirmed = false
        };
        accounts.Setup(s => s.AuthenticateAsync(user.Email, "Clave123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        guard.Setup(g => g.IsLockedOut(It.IsAny<string>(), It.IsAny<string?>())).Returns(false);

        var identity = new ClaimsIdentity();
        var controller = new AccountController(
            accounts.Object,
            Mock.Of<IPasswordResetService>(),
            Mock.Of<IFuncionalidadesReportService>(),
            Mock.Of<IActivityLogService>(),
            Mock.Of<IPlantaInventarioService>(),
            Mock.Of<IWebHostEnvironment>(),
            guard.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

        var result = await controller.Login(
            new LoginViewModel { Email = user.Email, Password = "Clave123!" },
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AccountController.ConfirmEmail), redirect.ActionName);
        Assert.Equal(user.Email, redirect.RouteValues?["email"]);
        guard.Verify(g => g.RecordFailure(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        guard.Verify(g => g.Reset(user.Email, It.IsAny<string?>()), Times.Once);
    }
}
