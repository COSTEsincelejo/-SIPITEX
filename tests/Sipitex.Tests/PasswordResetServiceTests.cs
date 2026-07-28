using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Moq;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class PasswordResetServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordResetTokenRepository> _tokens = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IEmailSender> _email = new();
    private readonly List<PasswordResetToken> _store = [];
    private string? _lastEmailBody;

    private PasswordResetService CreateSut()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _tokens.Setup(t => t.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()))
            .Callback<PasswordResetToken, CancellationToken>((token, _) =>
            {
                token.Id = _store.Count + 1;
                _store.Add(token);
            })
            .Returns(Task.CompletedTask);

        _tokens.Setup(t => t.GetUnusedByUserAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int userId, CancellationToken _) =>
                _store.Where(t => t.UserId == userId && t.UsedAtUtc is null).ToList());

        _tokens.Setup(t => t.CountCreatedSinceAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int userId, DateTime since, CancellationToken _) =>
                _store.Count(t => t.UserId == userId && t.CreatedAtUtc >= since));

        _tokens.Setup(t => t.FindValidAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int userId, string hash, DateTime now, CancellationToken _) =>
                _store.FirstOrDefault(t =>
                    t.UserId == userId
                    && t.TokenHash == hash
                    && t.UsedAtUtc is null
                    && t.ExpiresAtUtc > now));

        _tokens.Setup(t => t.Update(It.IsAny<PasswordResetToken>()));

        _email.Setup(e => e.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string, CancellationToken>((_, _, _, body, _) => _lastEmailBody = body)
            .Returns(Task.CompletedTask);

        return new PasswordResetService(_users.Object, _tokens.Object, _uow.Object, _email.Object);
    }

    private static User ActiveUser(string email = "user@sipitex.test") => new()
    {
        Id = 7,
        Nombre = "Usuario Demo",
        Email = email,
        PasswordHash = PasswordHasher.Hash("Antigua123!"),
        Rol = UserRoles.Instructor,
        IsActive = true
    };

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private string ExtractPlainTokenFromEmail()
    {
        Assert.False(string.IsNullOrWhiteSpace(_lastEmailBody));
        var match = Regex.Match(_lastEmailBody!, @"[?&]token=([^&\s]+)");
        Assert.True(match.Success);
        return Uri.UnescapeDataString(match.Groups[1].Value);
    }

    [Fact]
    public async Task RequestReset_ExistingEmail_CreatesHashedToken_NotPlaintext()
    {
        var user = ActiveUser();
        _users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var sut = CreateSut();

        await sut.RequestResetAsync(user.Email, "https://sipitex.test");

        var saved = Assert.Single(_store);
        var plain = ExtractPlainTokenFromEmail();
        Assert.Equal(Hash(plain), saved.TokenHash);
        Assert.DoesNotContain(plain, saved.TokenHash);
        Assert.NotEqual(plain, saved.TokenHash);
        Assert.Null(saved.UsedAtUtc);
        Assert.True(saved.ExpiresAtUtc > DateTime.UtcNow);
        _email.Verify(e => e.SendAsync(user.Email, user.Nombre, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestReset_UnknownEmail_DoesNotCreateToken_OrThrow()
    {
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var sut = CreateSut();

        var ex = await Record.ExceptionAsync(() =>
            sut.RequestResetAsync("nadie@sipitex.test", "https://sipitex.test"));

        Assert.Null(ex);
        Assert.Empty(_store);
        _email.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResetPassword_ValidToken_ChangesPassword()
    {
        var user = ActiveUser();
        _users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var sut = CreateSut();
        await sut.RequestResetAsync(user.Email, "https://sipitex.test");
        var plain = ExtractPlainTokenFromEmail();

        var result = await sut.ResetPasswordAsync(user.Email, plain, "NuevaClave99!");

        Assert.True(result.Success);
        Assert.True(PasswordHasher.Verify("NuevaClave99!", user.PasswordHash));
        Assert.NotNull(_store[0].UsedAtUtc);
    }

    [Fact]
    public async Task ResetPassword_UsedToken_Fails()
    {
        var user = ActiveUser();
        _users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var sut = CreateSut();
        await sut.RequestResetAsync(user.Email, "https://sipitex.test");
        var plain = ExtractPlainTokenFromEmail();
        await sut.ResetPasswordAsync(user.Email, plain, "NuevaClave99!");

        var second = await sut.ResetPasswordAsync(user.Email, plain, "OtraClave99!");

        Assert.False(second.Success);
        Assert.Equal("Enlace inválido o expirado.", second.Message);
    }

    [Fact]
    public async Task ResetPassword_ExpiredToken_Fails()
    {
        var user = ActiveUser();
        _users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var sut = CreateSut();
        await sut.RequestResetAsync(user.Email, "https://sipitex.test");
        var plain = ExtractPlainTokenFromEmail();
        _store[0].ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5);

        var result = await sut.ResetPasswordAsync(user.Email, plain, "NuevaClave99!");

        Assert.False(result.Success);
        Assert.Equal("Enlace inválido o expirado.", result.Message);
    }

    [Fact]
    public async Task ResetPassword_TokenForOtherEmail_Fails()
    {
        var owner = ActiveUser("owner@sipitex.test");
        var other = ActiveUser("other@sipitex.test");
        other.Id = 8;
        _users.Setup(r => r.GetByEmailAsync(owner.Email, It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _users.Setup(r => r.GetByEmailAsync(other.Email, It.IsAny<CancellationToken>())).ReturnsAsync(other);
        var sut = CreateSut();
        await sut.RequestResetAsync(owner.Email, "https://sipitex.test");
        var plain = ExtractPlainTokenFromEmail();

        var result = await sut.ResetPasswordAsync(other.Email, plain, "NuevaClave99!");

        Assert.False(result.Success);
        Assert.Equal("Enlace inválido o expirado.", result.Message);
    }

    [Fact]
    public async Task SecondRequest_InvalidatesFirstToken()
    {
        var user = ActiveUser();
        _users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var sut = CreateSut();

        await sut.RequestResetAsync(user.Email, "https://sipitex.test");
        var firstToken = ExtractPlainTokenFromEmail();

        await sut.RequestResetAsync(user.Email, "https://sipitex.test");
        var secondToken = ExtractPlainTokenFromEmail();

        Assert.NotEqual(firstToken, secondToken);
        Assert.NotNull(_store[0].UsedAtUtc);

        var oldResult = await sut.ResetPasswordAsync(user.Email, firstToken, "NuevaClave99!");
        Assert.False(oldResult.Success);

        var newResult = await sut.ResetPasswordAsync(user.Email, secondToken, "NuevaClave99!");
        Assert.True(newResult.Success);
        Assert.True(PasswordHasher.Verify("NuevaClave99!", user.PasswordHash));
    }
}
