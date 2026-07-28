using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

public class UserAccountService : IUserAccountService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserAccountService(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<User?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return null;

        var user = await _userRepository.GetByEmailAsync(email.Trim(), cancellationToken);
        if (user is null || !user.IsActive) return null;
        if (!PasswordHasher.Verify(password, user.PasswordHash)) return null;
        return user;
    }

    public Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken = default) =>
        _userRepository.GetAllAsync(cancellationToken);

    public Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _userRepository.GetByIdAsync(id, cancellationToken);

    public async Task<ServiceResult> CreateUserAsync(
        string nombre,
        string email,
        string password,
        string rol,
        int? fichaAsignadaId,
        IReadOnlyList<string> permisos,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(nombre, email, password, rol, requirePassword: true);
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
        return ServiceResult.Ok("Usuario creado correctamente.");
    }

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
        var validation = Validate(nombre, email, password, rol, requirePassword: false);
        if (validation is not null) return validation;

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null) return ServiceResult.Fail("Usuario no encontrado.");

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
        return ServiceResult.Ok("Usuario actualizado correctamente.");
    }

    public async Task<ServiceResult> ToggleUserStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null) return ServiceResult.Fail("Usuario no encontrado.");

        user.IsActive = isActive;
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok(isActive ? "Usuario activado." : "Usuario desactivado.");
    }

    private static ServiceResult? Validate(string nombre, string email, string password, string rol, bool requirePassword)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(email))
            return ServiceResult.Fail("Nombre y correo son obligatorios.");

        var passwordError = PasswordRules.Validate(password, required: requirePassword);
        if (passwordError is not null)
            return ServiceResult.Fail(passwordError);

        if (!UserRoles.All.Contains(rol))
            return ServiceResult.Fail("Rol no válido.");

        return null;
    }
}
