using Moq;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class EmailVerificationServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordResetTokenRepository> _tokens = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IEmailSender> _email = new();
    private readonly List<User> _userStore = [];
    private readonly List<PasswordResetToken> _store = [];
    private string? _lastEmailBody;

    private EmailVerificationService CreateSut()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        AuthCodeTestSupport.BindTokens(_tokens, _store);
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string email, CancellationToken _) =>
                _userStore.FirstOrDefault(u => u.Email == email.Trim().ToLowerInvariant()));
        _users.Setup(r => r.Add(It.IsAny<User>()))
            .Callback<User>(u =>
            {
                u.Id = _userStore.Count + 1;
                _userStore.Add(u);
            });
        _users.Setup(r => r.Update(It.IsAny<User>()));
        _email.Setup(e => e.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string, CancellationToken>((_, _, _, body, _) => _lastEmailBody = body)
            .Returns(Task.CompletedTask);

        var issuer = new AuthCodeIssuer(_tokens.Object, _uow.Object, _email.Object);
        return new EmailVerificationService(
            _users.Object, _tokens.Object, _uow.Object, issuer, new AuthCodeTestSupport.NoopGuard());
    }

    [Fact]
    public async Task Register_CreatesUnverifiedUser_AndEmailsHashedCode()
    {
        var sut = CreateSut();

        var result = await sut.RegisterAsync("Ana Nueva", "ana@sipitex.test", "Clave123!", "127.0.0.1");

        Assert.True(result.Success);
        var user = Assert.Single(_userStore);
        Assert.False(user.EmailConfirmed);
        Assert.Equal(UserRoles.Instructor, user.Rol);
        var code = AuthCodeTestSupport.ExtractCode(_lastEmailBody);
        Assert.Equal(AuthCodeHelper.Hash(code), _store[0].TokenHash);
        Assert.Equal(AuthCodePurposes.EmailVerification, _store[0].Purpose);
    }

    [Fact]
    public async Task Register_InvalidEmailFormat_FailsWithoutCreatingUser()
    {
        var sut = CreateSut();
        var result = await sut.RegisterAsync("Ana", "correo-invalido", "Clave123!", null);
        Assert.False(result.Success);
        Assert.Empty(_userStore);
        _email.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Verify_ValidCode_ConfirmsEmail()
    {
        var sut = CreateSut();
        await sut.RegisterAsync("Ana Nueva", "ana@sipitex.test", "Clave123!", null);
        var code = AuthCodeTestSupport.ExtractCode(_lastEmailBody);

        var result = await sut.VerifyAsync("ana@sipitex.test", code);

        Assert.True(result.Success);
        Assert.True(_userStore[0].EmailConfirmed);
        Assert.NotNull(_store[0].UsedAtUtc);
    }

    [Fact]
    public async Task Verify_ExpiredCode_FailsAndDoesNotConfirm()
    {
        var sut = CreateSut();
        await sut.RegisterAsync("Ana Nueva", "ana@sipitex.test", "Clave123!", null);
        var code = AuthCodeTestSupport.ExtractCode(_lastEmailBody);
        _store[0].ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);

        var result = await sut.VerifyAsync("ana@sipitex.test", code);

        Assert.False(result.Success);
        Assert.False(_userStore[0].EmailConfirmed);
        Assert.Equal(AuthCodeMessages.InvalidOrExpired, result.Message);
    }

    [Fact]
    public async Task Verify_UsedCode_CannotBeReused()
    {
        var sut = CreateSut();
        await sut.RegisterAsync("Ana Nueva", "ana@sipitex.test", "Clave123!", null);
        var code = AuthCodeTestSupport.ExtractCode(_lastEmailBody);
        await sut.VerifyAsync("ana@sipitex.test", code);
        _userStore[0].EmailConfirmed = false;

        var second = await sut.VerifyAsync("ana@sipitex.test", code);

        Assert.False(second.Success);
    }

    [Fact]
    public async Task Resend_InvalidatesPreviousCode()
    {
        var sut = CreateSut();
        await sut.RegisterAsync("Ana Nueva", "ana@sipitex.test", "Clave123!", null);
        var first = AuthCodeTestSupport.ExtractCode(_lastEmailBody);
        _store[0].CreatedAtUtc = DateTime.UtcNow.AddMinutes(-2);

        var resent = await sut.ResendAsync("ana@sipitex.test", null);
        Assert.True(resent.Success);
        var second = AuthCodeTestSupport.ExtractCode(_lastEmailBody);
        Assert.NotEqual(first, second);

        var old = await sut.VerifyAsync("ana@sipitex.test", first);
        Assert.False(old.Success);

        var ok = await sut.VerifyAsync("ana@sipitex.test", second);
        Assert.True(ok.Success);
        Assert.True(_userStore[0].EmailConfirmed);
    }

    [Fact]
    public async Task Resend_WithinCooldown_Fails()
    {
        var sut = CreateSut();
        await sut.RegisterAsync("Ana Nueva", "ana@sipitex.test", "Clave123!", null);

        var resent = await sut.ResendAsync("ana@sipitex.test", null);

        Assert.False(resent.Success);
        Assert.Equal(AuthCodeMessages.Cooldown, resent.Message);
        Assert.Single(_store);
    }
}
