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

    [Fact]
    public async Task CreateUserAsync_WhenRoleIsAdministrador_Fails()
    {
        var result = await CreateSut().CreateUserAsync(
            "Nuevo Admin",
            "nuevo@sipitex.test",
            "Clave123!",
            UserRoles.Administrador,
            null,
            []);

        Assert.False(result.Success);
        Assert.Contains("Instructor o Bodeguero", result.Message);
        _userRepository.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_WhenRoleIsInstructor_Succeeds()
    {
        _userRepository
            .Setup(r => r.EmailExistsAsync("nuevo@sipitex.test", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateSut().CreateUserAsync(
            "Nuevo Instructor",
            "nuevo@sipitex.test",
            "Clave123!",
            UserRoles.Instructor,
            null,
            []);

        Assert.True(result.Success);
        _userRepository.Verify(r => r.Add(It.Is<User>(u =>
            u.Email == "nuevo@sipitex.test" && u.Rol == UserRoles.Instructor)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesPhotoAndPassword()
    {
        var user = CreateUser("instructor@sipitex.test", "Instructor123!");
        _userRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepository
            .Setup(r => r.EmailExistsAsync("instructor@sipitex.test", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateSut().UpdateProfileAsync(
            1,
            "Instructor Actualizado",
            "instructor@sipitex.test",
            "NuevaClave1!",
            "/uploads/profiles/1.jpg",
            removePhoto: false);

        Assert.True(result.Success);
        Assert.Equal("Instructor Actualizado", user.Nombre);
        Assert.Equal("/uploads/profiles/1.jpg", user.PhotoPath);
        Assert.True(PasswordHasher.Verify("NuevaClave1!", user.PasswordHash));
        _userRepository.Verify(r => r.Update(user), Times.Once);
    }
}
