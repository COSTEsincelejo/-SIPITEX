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
    private readonly Mock<IBodegaRepository> _bodegaRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UserAccountService CreateSut()
    {
        _bodegaRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Bodega { Id = 1, Nombre = "Bodega 1" });
        _bodegaRepository
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Bodega { Id = 2, Nombre = "Bodega 2" });

        return new(_userRepository.Object, _fichaRepository.Object, _bodegaRepository.Object, _unitOfWork.Object);
    }

    private static User CreateUser(string email, string password, bool isActive = true, string rol = UserRoles.Instructor) => new()
    {
        Id = 1,
        Nombre = "Usuario Demo",
        Email = email,
        PasswordHash = PasswordHasher.Hash(password),
        Rol = rol,
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
    public async Task CreateUserAsync_WhenRoleIsAdministrador_Succeeds()
    {
        _userRepository
            .Setup(r => r.EmailExistsAsync("nuevo-admin@sipitex.test", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateSut().CreateUserAsync(
            "Nuevo Admin",
            "nuevo-admin@sipitex.test",
            "Clave123!",
            UserRoles.Administrador,
            null,
            null,
            []);

        Assert.True(result.Success);
        _userRepository.Verify(r => r.Add(It.Is<User>(u =>
            u.Email == "nuevo-admin@sipitex.test" && u.Rol == UserRoles.Administrador)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
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
            null,
            []);

        Assert.True(result.Success);
        _userRepository.Verify(r => r.Add(It.Is<User>(u =>
            u.Email == "nuevo@sipitex.test" && u.Rol == UserRoles.Instructor)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task DeleteUserAsync_WhenHasDependencies_BlocksAndSuggestsDeactivate()
    {
        var target = CreateUser("bodega@sipitex.test", "Clave123!", rol: UserRoles.Bodeguero);
        target.Id = 5;
        _userRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(target);
        _userRepository
            .Setup(r => r.GetDeletionBlockersAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["movimientos de inventario (StockMovement)"]);

        var result = await CreateSut().DeleteUserAsync(5, actorUserId: 1);

        Assert.False(result.Success);
        Assert.Contains("Desactive", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("StockMovement", result.Message);
        _userRepository.Verify(r => r.Remove(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUserAsync_WhenNoDependencies_Removes()
    {
        var target = CreateUser("temp@sipitex.test", "Clave123!", rol: UserRoles.Instructor);
        target.Id = 8;
        _userRepository.Setup(r => r.GetByIdAsync(8, It.IsAny<CancellationToken>())).ReturnsAsync(target);
        _userRepository
            .Setup(r => r.GetDeletionBlockersAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().DeleteUserAsync(8, actorUserId: 1);

        Assert.True(result.Success);
        _userRepository.Verify(r => r.Remove(target), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_SelfDelete_Fails()
    {
        var result = await CreateSut().DeleteUserAsync(id: 3, actorUserId: 3);

        Assert.False(result.Success);
        Assert.Contains("propia cuenta", result.Message, StringComparison.OrdinalIgnoreCase);
        _userRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUserAsync_LastActiveAdmin_Fails()
    {
        var admin = CreateUser("admin@sipitex.test", "Clave123!", rol: UserRoles.Administrador);
        admin.Id = 2;
        admin.IsActive = true;
        _userRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        _userRepository.Setup(r => r.CountActiveAdministratorsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().DeleteUserAsync(2, actorUserId: 9);

        Assert.False(result.Success);
        Assert.Contains("último administrador", result.Message, StringComparison.OrdinalIgnoreCase);
        _userRepository.Verify(r => r.Remove(It.IsAny<User>()), Times.Never);
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
            "Registro de producción y seguimiento de fichas en turno mañana.",
            "NuevaClave1!",
            "/uploads/profiles/1.jpg",
            removePhoto: false);

        Assert.True(result.Success);
        Assert.Equal("Instructor Actualizado", user.Nombre);
        Assert.Equal("Registro de producción y seguimiento de fichas en turno mañana.", user.FuncionDescripcion);
        Assert.Equal("/uploads/profiles/1.jpg", user.PhotoPath);
        Assert.True(PasswordHasher.Verify("NuevaClave1!", user.PasswordHash));
        _userRepository.Verify(r => r.Update(user), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_BodegueroConBodegaValida_AsignaBodegaId()
    {
        _userRepository
            .Setup(r => r.EmailExistsAsync("bodega3@sipitex.test", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        User? saved = null;
        _userRepository
            .Setup(r => r.Add(It.IsAny<User>()))
            .Callback<User>(u => saved = u);

        var result = await CreateSut().CreateUserAsync(
            "Bodeguero Tres",
            "bodega3@sipitex.test",
            "Clave123!",
            UserRoles.Bodeguero,
            fichaAsignadaId: null,
            bodegaId: 2,
            []);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(saved);
        Assert.Equal(2, saved!.BodegaId);
        Assert.Equal(UserRoles.Bodeguero, saved.Rol);
    }

    [Fact]
    public async Task CreateUserAsync_InstructorConBodegaId_IgnoraYDejaNull()
    {
        _userRepository
            .Setup(r => r.EmailExistsAsync("inst@sipitex.test", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        User? saved = null;
        _userRepository
            .Setup(r => r.Add(It.IsAny<User>()))
            .Callback<User>(u => saved = u);

        var result = await CreateSut().CreateUserAsync(
            "Instructor",
            "inst@sipitex.test",
            "Clave123!",
            UserRoles.Instructor,
            fichaAsignadaId: null,
            bodegaId: 2,
            []);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(saved);
        Assert.Null(saved!.BodegaId);
    }

    [Fact]
    public async Task CreateUserAsync_AdministradorConBodegaId_IgnoraYDejaNull()
    {
        _userRepository
            .Setup(r => r.EmailExistsAsync("admin2@sipitex.test", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        User? saved = null;
        _userRepository
            .Setup(r => r.Add(It.IsAny<User>()))
            .Callback<User>(u => saved = u);

        var result = await CreateSut().CreateUserAsync(
            "Admin Dos",
            "admin2@sipitex.test",
            "Clave123!",
            UserRoles.Administrador,
            fichaAsignadaId: null,
            bodegaId: 1,
            []);

        Assert.True(result.Success, result.Message);
        Assert.Null(saved!.BodegaId);
    }

    [Fact]
    public async Task CreateUserAsync_BodegueroSinBodega_Falla()
    {
        var result = await CreateSut().CreateUserAsync(
            "Bodeguero Huérfano",
            "huerfano@sipitex.test",
            "Clave123!",
            UserRoles.Bodeguero,
            fichaAsignadaId: null,
            bodegaId: null,
            []);

        Assert.False(result.Success);
        Assert.Contains("asignar una bodega", result.Message, StringComparison.OrdinalIgnoreCase);
        _userRepository.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_AsignaBodegaABodegueroSinBodega()
    {
        var user = CreateUser("legado@sipitex.test", "Clave123!", rol: UserRoles.Bodeguero);
        user.Id = 12;
        user.BodegaId = null;
        _userRepository.Setup(r => r.GetByIdAsync(12, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userRepository
            .Setup(r => r.EmailExistsAsync("legado@sipitex.test", 12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().UpdateUserAsync(
            12,
            "Bodeguero Legado",
            "legado@sipitex.test",
            password: "",
            UserRoles.Bodeguero,
            fichaAsignadaId: null,
            bodegaId: 1,
            [],
            isActive: true);

        Assert.True(result.Success, result.Message);
        Assert.Equal(1, user.BodegaId);
        _userRepository.Verify(r => r.Update(user), Times.Once);
    }
}
