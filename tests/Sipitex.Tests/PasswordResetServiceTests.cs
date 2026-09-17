using Moq;
using Sipitex.Application.DTOs;
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
        AuthCodeTestSupport.BindTokens(_tokens, _store);
        _email.Setup(e => e.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string, CancellationToken>((_, _, _, body, _) => _lastEmailBody = body)
            .Returns(Task.CompletedTask);

        var issuer = new AuthCodeIssuer(_tokens.Object, _uow.Object, _email.Object);
        return new PasswordResetService(_users.Object, _uow.Object, issuer, new AuthCodeTestSupport.NoopGuard());
    }

    private static User ActiveUser(string email = "user@sipitex.test") => new()
    {
        Id = 7,
        Nombre = "Usuario Demo",
        Email = email,
        PasswordHash = PasswordHasher.Hash("Antigua123!"),
        Rol = UserRoles.Instructor,
        IsActive = true,
        EmailConfirmed = true
    };

    [Fact]
    public async Task RequestReset_ExistingEmail_CreatesHashedSixDigitCode_NotPlaintext()
    {
        var user = ActiveUser();
        _users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var sut = CreateSut();

        await sut.RequestResetAsync(user.Email);

        var saved = Assert.Single(_store);
        var plain = AuthCodeTestSupport.ExtractCode(_lastEmailBody);
        Assert.Equal(AuthCodeHelper.Hash(plain), saved.TokenHash);
        Assert.DoesNotContain(plain, saved.TokenHash);
        Assert.Equal(AuthCodePurposes.PasswordReset, saved.Purpose);
        Assert.Null(saved.UsedAtUtc);
        Assert.True(saved.ExpiresAtUtc > DateTime.UtcNow);
        Assert.DoesNotContain("/Account/ResetPassword?token=", _lastEmailBody, StringComparison.OrdinalIgnoreCase);
        _email.Verify(e => e.SendAsync(user.Email, user.Nombre, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestReset_UnknownEmail_DoesNotCreateToken_OrThrow()
    {
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var sut = CreateSut();

        var ex = await Record.ExceptionAsync(() => sut.RequestResetAsync("nadie@sipitex.test"));

        Assert.Null(ex);
        Assert.Empty(_store);
        _email.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResetPassword_ValidCode_ChangesPassword()
    {
        var user = ActiveUser();
        _users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var sut = CreateSut();
        await sut.RequestResetAsync(user.Email);
        var code = AuthCodeTestSupport.ExtractCode(_lastEmailBody);

        var result = await sut.ResetPasswordAsync(user.Email, code, "NuevaClave99!");

        Assert.True(result.Success);
        Assert.True(PasswordHasher.Verify("NuevaClave99!", user.PasswordHash));
        Assert.NotNull(_store[0].UsedAtUtc);
    }

    [Fact]
    public async Task ResetPassword_UsedCode_Fails()
    {
        var user = ActiveUser();
        _users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var sut = CreateSut();
        await sut.RequestResetAsync(user.Email);
        var code = AuthCodeTestSupport.ExtractCode(_lastEmailBody);
        await sut.ResetPasswordAsync(user.Email, code, "NuevaClave99!");

        var second = await sut.ResetPasswordAsync(user.Email, code, "OtraClave99!");

        Assert.False(second.Success);
        Assert.Equal(AuthCodeMessages.InvalidOrExpired, second.Message);
    }

    [Fact]
    public async Task ResetPassword_ExpiredCode_Fails()
    {
        var user = ActiveUser();
        _users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var sut = CreateSut();
        await sut.RequestResetAsync(user.Email);
        var code = AuthCodeTestSupport.ExtractCode(_lastEmailBody);
        _store[0].ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5);

        var result = await sut.ResetPasswordAsync(user.Email, code, "NuevaClave99!");

        Assert.False(result.Success);
        Assert.Equal(AuthCodeMessages.InvalidOrExpired, result.Message);
    }

    [Fact]
    public async Task ResetPassword_WrongCode_ThenLocksAfterMaxAttempts()
    {
        var user = ActiveUser();
        _users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var sut = CreateSut();
        await sut.RequestResetAsync(user.Email);

        ServiceResult? last = null;
        for (var i = 0; i < AuthCodeHelper.MaxFailedAttempts; i++)
            last = await sut.ResetPasswordAsync(user.Email, "000000", "NuevaClave99!");

        Assert.False(last!.Success);
        Assert.Equal(AuthCodeMessages.TooManyAttempts, last.Message);
        Assert.NotNull(_store[0].UsedAtUtc);
    }

    [Fact]
    public async Task ResetPassword_CodeForOtherEmail_Fails()
    {
        var owner = ActiveUser("owner@sipitex.test");
        var other = ActiveUser("other@sipitex.test");
        other.Id = 8;
        _users.Setup(r => r.GetByEmailAsync(owner.Email, It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _users.Setup(r => r.GetByEmailAsync(other.Email, It.IsAny<CancellationToken>())).ReturnsAsync(other);
        var sut = CreateSut();
        await sut.RequestResetAsync(owner.Email);
        var code = AuthCodeTestSupport.ExtractCode(_lastEmailBody);

        var result = await sut.ResetPasswordAsync(other.Email, code, "NuevaClave99!");

        Assert.False(result.Success);
        Assert.Equal(AuthCodeMessages.InvalidOrExpired, result.Message);
    }

    [Fact]
    public async Task SecondRequest_InvalidatesFirstCode()
    {
        var user = ActiveUser();
        _users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var sut = CreateSut();

        await sut.RequestResetAsync(user.Email);
        var first = AuthCodeTestSupport.ExtractCode(_lastEmailBody);

        await sut.RequestResetAsync(user.Email);
        var second = AuthCodeTestSupport.ExtractCode(_lastEmailBody);

        Assert.NotEqual(first, second);
        Assert.NotNull(_store[0].UsedAtUtc);

        var oldResult = await sut.ResetPasswordAsync(user.Email, first, "NuevaClave99!");
        Assert.False(oldResult.Success);

        var newResult = await sut.ResetPasswordAsync(user.Email, second, "NuevaClave99!");
        Assert.True(newResult.Success);
        Assert.True(PasswordHasher.Verify("NuevaClave99!", user.PasswordHash));
    }
}
