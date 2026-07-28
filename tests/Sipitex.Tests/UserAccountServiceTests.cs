using Moq;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class UserAccountServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IFichaRepository> _fichaRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UserAccountService CreateSut() => new(_userRepository.Object, _fichaRepository.Object, _unitOfWork.Object);

    private static User CreateUser(string email, string password, bool isActive = true) => new()
    {
        Id = 1,
        Nombre = "Usuario Demo",
        Email = email,
        PasswordHash = PasswordHasher.Hash(password),
        Rol = UserRoles.Instructor,
        IsActive = isActive
    };

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsUser()
    {
        var user = CreateUser("instructor@sipitex.test", "Instructor123!");
        _userRepository
            .Setup(r => r.GetByEmailAsync("instructor@sipitex.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await CreateSut().AuthenticateAsync("instructor@sipitex.test", "Instructor123!");

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidPassword_ReturnsNull()
    {
        var user = CreateUser("instructor@sipitex.test", "Instructor123!");
        _userRepository
            .Setup(r => r.GetByEmailAsync("instructor@sipitex.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await CreateSut().AuthenticateAsync("instructor@sipitex.test", "clave-incorrecta");

        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenUserIsInactive_ReturnsNull()
    {
        var user = CreateUser("instructor@sipitex.test", "Instructor123!", isActive: false);
        _userRepository
            .Setup(r => r.GetByEmailAsync("instructor@sipitex.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await CreateSut().AuthenticateAsync("instructor@sipitex.test", "Instructor123!");

        Assert.Null(result);
    }
}
