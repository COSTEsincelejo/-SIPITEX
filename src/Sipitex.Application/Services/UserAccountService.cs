using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

// Login, CRUD de usuarios y perfil
public class UserAccountService : IUserAccountService
{
    private readonly IUserRepository _userRepository;
    private readonly IFichaRepository _fichaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserAccountService(IUserRepository userRepository, IFichaRepository fichaRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _fichaRepository = fichaRepository;
        _unitOfWork = unitOfWork;
    }

    // Login: busca por email y compara hash de contraseña
    public async Task<User?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return null;

        var user = await _userRepository.GetByEmailAsync(email.Trim(), cancellationToken);
        if (user is null || !user.IsActive) return null;
        if (!PasswordHasher.Verify(password, user.PasswordHash)) return null;
        return user;
    }

    // Lista completa (la usa el admin en la pantalla de usuarios)
    public Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken = default) =>
        _userRepository.GetAllAsync(cancellationToken);

    public Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _userRepository.GetByIdAsync(id, cancellationToken);

    // Alta de usuario (solo roles que el admin puede crear)
    public async Task<ServiceResult> CreateUserAsync(
        string nombre,
        string email,
        string password,
        string rol,
        int? fichaAsignadaId,
        IReadOnlyList<string> permisos,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(nombre, email, password, rol, requirePassword: true, creatableOnly: true);
        if (validation is not null) return validation;

        if (await _userRepository.EmailExistsAsync(email.Trim(), null, cancellationToken))
            return ServiceResult.Fail("Ya existe un usuario con ese correo.");

        var user = new User
        {
            Nombre = nombre.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = PasswordHasher.Hash(password),
            Rol = rol,
            FichaAsignadaId = fichaAsignadaId,
            PermisosExtendidos = ExtendedPermissions.Serialize(permisos),
            IsActive = true
        };

        _userRepository.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await SyncFichaOwnershipAsync(user.Id, user.Nombre, user.Rol, fichaAsignadaId, cancellationToken);
        return ServiceResult.Ok("Usuario creado correctamente.");
    }

    // Edición desde el admin: datos, rol, permisos y activo/inactivo
    public async Task<ServiceResult> UpdateUserAsync(
        int id,
        string nombre,
        string email,
        string password,
        string rol,
        int? fichaAsignadaId,
        IReadOnlyList<string> permisos,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(nombre, email, password, rol, requirePassword: false, creatableOnly: false);
        if (validation is not null) return validation;

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null) return ServiceResult.Fail("Usuario no encontrado.");

        // El admin principal no se le puede bajar el rol
        var isExistingAdmin = string.Equals(user.Rol, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase);
        if (isExistingAdmin)
        {
            if (!string.Equals(rol, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase))
                return ServiceResult.Fail("No se puede cambiar el rol del administrador.");
        }
        else if (!UserRoles.CreatableByAdmin.Contains(rol))
        {
            return ServiceResult.Fail("Solo se pueden asignar roles de Instructor o Bodeguero.");
        }

        if (await _userRepository.EmailExistsAsync(email.Trim(), id, cancellationToken))
            return ServiceResult.Fail("Ya existe un usuario con ese correo.");

        user.Nombre = nombre.Trim();
        user.Email = email.Trim().ToLowerInvariant();
        user.Rol = rol;
        user.FichaAsignadaId = fichaAsignadaId;
        user.PermisosExtendidos = ExtendedPermissions.Serialize(permisos);
        user.IsActive = isActive;

        if (!string.IsNullOrWhiteSpace(password))
            user.PasswordHash = PasswordHasher.Hash(password);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await SyncFichaOwnershipAsync(user.Id, user.Nombre, user.Rol, fichaAsignadaId, cancellationToken);
        return ServiceResult.Ok("Usuario actualizado correctamente.");
    }

    // Activar / desactivar sin borrar el usuario (así no pierde el historial)
    public async Task<ServiceResult> ToggleUserStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null) return ServiceResult.Fail("Usuario no encontrado.");

        user.IsActive = isActive;
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok(isActive ? "Usuario activado." : "Usuario desactivado.");
    }

    // El usuario edita su propio perfil (nombre, correo, foto, contraseña opcional)
    public async Task<ServiceResult> UpdateProfileAsync(
        int id,
        string nombre,
        string email,
        string? funcionDescripcion,
        string? newPassword,
        string? photoPath,
        bool removePhoto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(email))
            return ServiceResult.Fail("Nombre y correo son obligatorios.");

        if (funcionDescripcion is { Length: > 800 })
            return ServiceResult.Fail("La descripción de funciones no puede superar 800 caracteres.");

        var passwordError = PasswordRules.Validate(newPassword, required: false);
        if (passwordError is not null)
            return ServiceResult.Fail(passwordError);

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null) return ServiceResult.Fail("Usuario no encontrado.");

        if (await _userRepository.EmailExistsAsync(email.Trim(), id, cancellationToken))
            return ServiceResult.Fail("Ya existe un usuario con ese correo.");

        user.Nombre = nombre.Trim();
        user.Email = email.Trim().ToLowerInvariant();
        user.FuncionDescripcion = string.IsNullOrWhiteSpace(funcionDescripcion)
            ? null
            : funcionDescripcion.Trim();

        if (!string.IsNullOrWhiteSpace(newPassword))
            user.PasswordHash = PasswordHasher.Hash(newPassword);

        if (removePhoto)
            user.PhotoPath = null;
        else if (!string.IsNullOrWhiteSpace(photoPath))
            user.PhotoPath = photoPath;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Perfil actualizado correctamente.");
    }

    // Validaciones comunes entre crear y editar usuario
    private static ServiceResult? Validate(
        string nombre,
        string email,
        string password,
        string rol,
        bool requirePassword,
        bool creatableOnly)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(email))
            return ServiceResult.Fail("Nombre y correo son obligatorios.");

        var passwordError = PasswordRules.Validate(password, required: requirePassword);
        if (passwordError is not null)
            return ServiceResult.Fail(passwordError);

        if (creatableOnly)
        {
            if (!UserRoles.CreatableByAdmin.Contains(rol))
                return ServiceResult.Fail("Solo el administrador puede crear cuentas de Instructor o Bodeguero.");
        }
        else if (!UserRoles.All.Contains(rol))
        {
            return ServiceResult.Fail("Rol no válido.");
        }

        return null;
    }

    // Si es instructor con ficha asignada, actualizo la ficha para que apunte a ese usuario
    private async Task SyncFichaOwnershipAsync(
        int userId,
        string nombre,
        string rol,
        int? fichaAsignadaId,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(rol, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase)
            || fichaAsignadaId is not int fichaId)
            return;

        var ficha = await _fichaRepository.GetByIdAsync(fichaId, cancellationToken);
        if (ficha is null) return;

        ficha.InstructorUserId = userId;
        ficha.InstructorName = nombre;
        _fichaRepository.Update(ficha);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
