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

        _tokens.Setup(t => t.GetUnusedByUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int userId, string purpose, CancellationToken _) =>
                _store.Where(t => t.UserId == userId && t.Purpose == purpose && t.UsedAtUtc is null).ToList());

        _tokens.Setup(t => t.GetLatestUnusedAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int userId, string purpose, CancellationToken _) =>
                _store.Where(t => t.UserId == userId && t.Purpose == purpose && t.UsedAtUtc is null)
                    .OrderByDescending(t => t.Id)
                    .FirstOrDefault());

        _tokens.Setup(t => t.CountCreatedSinceAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int userId, string purpose, DateTime since, CancellationToken _) =>
                _store.Count(t => t.UserId == userId && t.Purpose == purpose && t.CreatedAtUtc >= since));

        _tokens.Setup(t => t.FindByHashAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int userId, string purpose, string hash, CancellationToken _) =>
                _store.Where(t => t.UserId == userId && t.Purpose == purpose && t.TokenHash == hash)
                    .OrderByDescending(t => t.Id)
                    .FirstOrDefault());

        _tokens.Setup(t => t.Update(It.IsAny<PasswordResetToken>()));

        _email.Setup(e => e.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string, CancellationToken>((_, _, _, body, _) => _lastEmailBody = body)
            .Returns(Task.CompletedTask);

        return new PasswordResetService(_users.Object, _tokens.Object, _uow.Object, _email.Object);
    }

    private static User ActiveUser(string email = "user@sipitex.test", bool emailConfirmed = true) => new()
    {
        Id = 7,
        Nombre = "Usuario Demo",
        Email = email,
        PasswordHash = PasswordHasher.Hash("Antigua123!"),
        Rol = UserRoles.Instructor,
        IsActive = true,
        EmailConfirmed = emailConfirmed
    };

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private string ExtractCodeFromEmail()
    {
        Assert.False(string.IsNullOrWhiteSpace(_lastEmailBody));
        var match = Regex.Match(_lastEmailBody!, @"(?m)^\s*(\d{6})\s*$");
        Assert.True(match.Success, _lastEmailBody);
        return match.Groups[1].Value;
    }

    private static string DifferentCode(string code) => code == "000000" ? "111111" : "000000";

    private void StubUser(User user) =>
        _users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);

    [Fact]
    public async Task RequestReset_ExistingEmail_SendsSixDigitCode_StoresOnlyHash()
    {
        var user = ActiveUser();
        StubUser(user);
        var sut = CreateSut();

        await sut.RequestResetAsync(user.Email);

        var saved = Assert.Single(_store);
        var plain = ExtractCodeFromEmail();
        Assert.Matches(@"^\d{6}$", plain);
        Assert.Equal(Hash(plain), saved.TokenHash);
        Assert.Equal(64, saved.TokenHash.Length);
        Assert.NotEqual(plain, saved.TokenHash);
        Assert.Equal(VerificationCodePurpose.PasswordReset, saved.Purpose);
        Assert.Null(saved.UsedAtUtc);
        Assert.Equal(PasswordResetService.CodeLifetime, saved.ExpiresAtUtc - saved.CreatedAtUtc);
        Assert.Equal(TimeSpan.FromMinutes(15), PasswordResetService.CodeLifetime);
        Assert.DoesNotContain("http", _lastEmailBody!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token=", _lastEmailBody!, StringComparison.OrdinalIgnoreCase);
        _email.Verify(e => e.SendAsync(
            user.Email, user.Nombre, It.Is<string>(s => s.Contains("contraseña")), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestReset_UnknownEmail_DoesNotCreateCode_OrThrow()
    {
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var sut = CreateSut();

        var ex = await Record.ExceptionAsync(() => sut.RequestResetAsync("nadie@sipitex.test"));

        Assert.Null(ex);
        Assert.Empty(_store);
        _email.Verify(e => e.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestReset_InactiveUser_DoesNotSendCode()
    {
        var user = ActiveUser();
        user.IsActive = false;
        StubUser(user);
        var sut = CreateSut();

        await sut.RequestResetAsync(user.Email);

        Assert.Empty(_store);
        _email.Verify(e => e.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResetPassword_ValidCode_ChangesPassword_AndMessageDoesNotEchoCode()
    {
        var user = ActiveUser();
        StubUser(user);
        var sut = CreateSut();
        await sut.RequestResetAsync(user.Email);
        var plain = ExtractCodeFromEmail();

        var result = await sut.ResetPasswordAsync(user.Email, plain.Insert(3, " "), "NuevaClave99!");

        Assert.True(result.Success);
        Assert.DoesNotContain(plain, result.Message);
        Assert.True(PasswordHasher.Verify("NuevaClave99!", user.PasswordHash));
        Assert.NotNull(_store[0].UsedAtUtc);
    }

    [Fact]
    public async Task ResetPassword_UsedCode_Fails()
    {
        var user = ActiveUser();
        StubUser(user);
        var sut = CreateSut();
        await sut.RequestResetAsync(user.Email);
        var plain = ExtractCodeFromEmail();
        await sut.ResetPasswordAsync(user.Email, plain, "NuevaClave99!");

        var second = await sut.ResetPasswordAsync(user.Email, plain, "OtraClave99!");

        Assert.False(second.Success);
        Assert.Equal(PasswordResetService.UsedCodeMessage, second.Message);
        Assert.DoesNotContain(plain, second.Message);
    }

    [Fact]
    public async Task ResetPassword_ExpiredCode_FailsWithExpiredMessage()
    {
        var user = ActiveUser();
        StubUser(user);
        var sut = CreateSut();
        await sut.RequestResetAsync(user.Email);
        var plain = ExtractCodeFromEmail();
        _store[0].ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);

        var result = await sut.ResetPasswordAsync(user.Email, plain, "NuevaClave99!");

        Assert.False(result.Success);
        Assert.Equal(PasswordResetService.ExpiredCodeMessage, result.Message);
    }

    [Fact]
    public async Task ResetPassword_WrongCode_FailsWithoutRevealingWhetherEmailExists()
    {
        var user = ActiveUser();
        StubUser(user);
        var sut = CreateSut();
        await sut.RequestResetAsync(user.Email);
        var plain = ExtractCodeFromEmail();

        var result = await sut.ResetPasswordAsync(user.Email, DifferentCode(plain), "NuevaClave99!");

        Assert.False(result.Success);
        Assert.Equal(PasswordResetService.InvalidCodeMessage, result.Message);
        Assert.Null(_store[0].UsedAtUtc);
        Assert.Equal(1, _store[0].FailedAttempts);
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
        var plain = ExtractCodeFromEmail();

        var result = await sut.ResetPasswordAsync(other.Email, plain, "NuevaClave99!");

        Assert.False(result.Success);
        Assert.Equal(PasswordResetService.InvalidCodeMessage, result.Message);
    }

    [Fact]
    public async Task SecondRequest_InvalidatesFirstCode()
    {
        var user = ActiveUser();
        StubUser(user);
        var sut = CreateSut();

        await sut.RequestResetAsync(user.Email);
        var firstCode = ExtractCodeFromEmail();

        await sut.RequestResetAsync(user.Email);
        var secondCode = ExtractCodeFromEmail();

        Assert.NotEqual(firstCode, secondCode);
        Assert.NotNull(_store[0].UsedAtUtc);

        var oldResult = await sut.ResetPasswordAsync(user.Email, firstCode, "NuevaClave99!");
        Assert.False(oldResult.Success);
        Assert.Equal(PasswordResetService.UsedCodeMessage, oldResult.Message);

        var newResult = await sut.ResetPasswordAsync(user.Email, secondCode, "NuevaClave99!");
        Assert.True(newResult.Success);
        Assert.True(PasswordHasher.Verify("NuevaClave99!", user.PasswordHash));
    }

    [Fact]
    public async Task FourthRequestWithinWindow_DoesNotSendAnotherCode()
    {
        var user = ActiveUser();
        StubUser(user);
        var sut = CreateSut();

        await sut.RequestResetAsync(user.Email);
        await sut.RequestResetAsync(user.Email);
        await sut.RequestResetAsync(user.Email);
        var validCode = ExtractCodeFromEmail();

        await sut.RequestResetAsync(user.Email);

        Assert.Equal(3, _store.Count);
        _email.Verify(e => e.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        var stillWorks = await sut.ResetPasswordAsync(user.Email, validCode, "NuevaClave99!");
        Assert.True(stillWorks.Success);
    }

    [Fact]
    public async Task ResetPassword_FifthWrongAttempt_InvalidatesCode()
    {
        var user = ActiveUser();
        StubUser(user);
        var sut = CreateSut();
        await sut.RequestResetAsync(user.Email);
        var plain = ExtractCodeFromEmail();
        var wrong = DifferentCode(plain);

        for (var i = 0; i < PasswordResetService.MaxFailedAttempts - 1; i++)
        {
            var attempt = await sut.ResetPasswordAsync(user.Email, wrong, "NuevaClave99!");
            Assert.Equal(PasswordResetService.InvalidCodeMessage, attempt.Message);
        }

        var locked = await sut.ResetPasswordAsync(user.Email, wrong, "NuevaClave99!");
        Assert.Equal(PasswordResetService.TooManyAttemptsMessage, locked.Message);
        Assert.DoesNotContain(plain, locked.Message);

        var after = await sut.ResetPasswordAsync(user.Email, plain, "NuevaClave99!");
        Assert.False(after.Success);
        Assert.False(PasswordHasher.Verify("NuevaClave99!", user.PasswordHash));
    }

    [Fact]
    public async Task SendEmailConfirmation_StoresHashedCode_AndConfirmMarksEmailConfirmed()
    {
        var user = ActiveUser(emailConfirmed: false);
        StubUser(user);
        var sut = CreateSut();

        var sent = await sut.SendEmailConfirmationAsync(user);

        Assert.True(sent.Success);
        var saved = Assert.Single(_store);
        var plain = ExtractCodeFromEmail();
        Assert.Equal(VerificationCodePurpose.EmailConfirmation, saved.Purpose);
        Assert.Equal(Hash(plain), saved.TokenHash);
        Assert.Equal(TimeSpan.FromMinutes(15), saved.ExpiresAtUtc - saved.CreatedAtUtc);
        Assert.DoesNotContain("http", _lastEmailBody!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(plain, sent.Message ?? string.Empty);

        var result = await sut.ConfirmEmailAsync(user.Email, plain);

        Assert.True(result.Success);
        Assert.True(user.EmailConfirmed);
        Assert.NotNull(saved.UsedAtUtc);
        Assert.DoesNotContain(plain, result.Message);
    }

    [Fact]
    public async Task ConfirmEmail_ExpiredCode_Fails()
    {
        var user = ActiveUser(emailConfirmed: false);
        StubUser(user);
        var sut = CreateSut();
        await sut.SendEmailConfirmationAsync(user);
        var plain = ExtractCodeFromEmail();
        _store[0].ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);

        var result = await sut.ConfirmEmailAsync(user.Email, plain);

        Assert.False(result.Success);
        Assert.Equal(PasswordResetService.ExpiredCodeMessage, result.Message);
        Assert.False(user.EmailConfirmed);
    }

    [Fact]
    public async Task ConfirmEmail_WrongCode_Fails()
    {
        var user = ActiveUser(emailConfirmed: false);
        StubUser(user);
        var sut = CreateSut();
        await sut.SendEmailConfirmationAsync(user);
        var plain = ExtractCodeFromEmail();

        var result = await sut.ConfirmEmailAsync(user.Email, DifferentCode(plain));

        Assert.False(result.Success);
        Assert.Equal(PasswordResetService.InvalidCodeMessage, result.Message);
        Assert.False(user.EmailConfirmed);
    }

    [Fact]
    public async Task ConfirmEmail_UsedCode_Fails()
    {
        var user = ActiveUser(emailConfirmed: false);
        StubUser(user);
        var sut = CreateSut();
        await sut.SendEmailConfirmationAsync(user);
        var plain = ExtractCodeFromEmail();
        Assert.True((await sut.ConfirmEmailAsync(user.Email, plain)).Success);
        user.EmailConfirmed = false;

        var second = await sut.ConfirmEmailAsync(user.Email, plain);

        Assert.False(second.Success);
        Assert.Equal(PasswordResetService.UsedCodeMessage, second.Message);
        Assert.False(user.EmailConfirmed);
    }

    [Fact]
    public async Task ResendEmailConfirmation_WithinCooldown_KeepsPreviousCode()
    {
        var user = ActiveUser(emailConfirmed: false);
        StubUser(user);
        var sut = CreateSut();
        await sut.SendEmailConfirmationAsync(user);
        var first = ExtractCodeFromEmail();

        var resend = await sut.ResendEmailConfirmationAsync(user.Email);

        Assert.False(resend.Success);
        Assert.Equal(PasswordResetService.ResendCooldownMessage, resend.Message);
        Assert.Single(_store);
        Assert.Null(_store[0].UsedAtUtc);
        _email.Verify(e => e.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

        var stillValid = await sut.ConfirmEmailAsync(user.Email, first);
        Assert.True(stillValid.Success);
    }

    [Fact]
    public async Task ResendEmailConfirmation_AfterCooldown_InvalidatesPreviousCode()
    {
        var user = ActiveUser(emailConfirmed: false);
        StubUser(user);
        var sut = CreateSut();
        await sut.SendEmailConfirmationAsync(user);
        var first = ExtractCodeFromEmail();
        _store[0].CreatedAtUtc = DateTime.UtcNow.AddMinutes(-2);

        var resend = await sut.ResendEmailConfirmationAsync(user.Email);
        var second = ExtractCodeFromEmail();

        Assert.True(resend.Success);
        Assert.DoesNotContain(second, resend.Message ?? string.Empty);
        Assert.NotEqual(first, second);
        Assert.NotNull(_store[0].UsedAtUtc);
        Assert.Equal(2, _store.Count);

        var oldResult = await sut.ConfirmEmailAsync(user.Email, first);
        Assert.False(oldResult.Success);
        Assert.False(user.EmailConfirmed);

        var newResult = await sut.ConfirmEmailAsync(user.Email, second);
        Assert.True(newResult.Success);
        Assert.True(user.EmailConfirmed);
    }

    [Fact]
    public async Task ResendEmailConfirmation_UnknownEmail_DoesNotSend_AndStaysGeneric()
    {
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var sut = CreateSut();

        var result = await sut.ResendEmailConfirmationAsync("nadie@sipitex.test");

        Assert.True(result.Success);
        Assert.Equal(PasswordResetService.ConfirmationPendingMessage, result.Message);
        Assert.Empty(_store);
    }

    [Fact]
    public async Task SendEmailConfirmation_WhenAlreadyConfirmed_DoesNotSend()
    {
        var user = ActiveUser(emailConfirmed: true);
        var sut = CreateSut();

        var result = await sut.SendEmailConfirmationAsync(user);

        Assert.True(result.Success);
        Assert.Empty(_store);
        _email.Verify(e => e.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PasswordResetCode_DoesNotConfirmEmail_AndConfirmationCodeDoesNotResetPassword()
    {
        var user = ActiveUser(emailConfirmed: false);
        StubUser(user);
        var sut = CreateSut();
        await sut.SendEmailConfirmationAsync(user);
        var confirmCode = ExtractCodeFromEmail();
        await sut.RequestResetAsync(user.Email);
        var resetCode = ExtractCodeFromEmail();

        if (confirmCode != resetCode)
        {
            var resetWithConfirm = await sut.ResetPasswordAsync(user.Email, confirmCode, "NuevaClave99!");
            Assert.False(resetWithConfirm.Success);
            Assert.False(user.EmailConfirmed);

            var confirmWithReset = await sut.ConfirmEmailAsync(user.Email, resetCode);
            Assert.False(confirmWithReset.Success);
            Assert.False(PasswordHasher.Verify("NuevaClave99!", user.PasswordHash));
        }

        var confirmed = await sut.ConfirmEmailAsync(user.Email, confirmCode);
        Assert.True(confirmed.Success);
        Assert.True(user.EmailConfirmed);
    }
}
