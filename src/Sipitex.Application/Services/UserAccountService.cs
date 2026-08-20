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
    private readonly IBodegaRepository _bodegaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserAccountService(
        IUserRepository userRepository,
        IFichaRepository fichaRepository,
        IBodegaRepository bodegaRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _fichaRepository = fichaRepository;
        _bodegaRepository = bodegaRepository;
        _unitOfWork = unitOfWork;
    }

    // Login: busca por email y compara hash de contraseña
    public async Task<User?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        // Acá reviso que vengan datos
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return null;

        // Busco el usuario por correo
        var user = await _userRepository.GetByEmailAsync(email.Trim(), cancellationToken);
        // Usuario inexistente o cuenta desactivada
        if (user is null || !user.IsActive) return null;
        // Verifico contraseña con PBKDF2
        if (!PasswordHasher.Verify(password, user.PasswordHash)) return null;
        return user;
    }

    // Lista completa (la usa el admin en la pantalla de usuarios)
    public Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken = default) =>
        _userRepository.GetAllAsync(cancellationToken);

    // Trae un usuario por id
    public Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _userRepository.GetByIdAsync(id, cancellationToken);

    // Alta de usuario (solo roles que el admin puede crear)
    public async Task<ServiceResult> CreateUserAsync(
        string nombre,
        string email,
        string password,
        string rol,
        int? fichaAsignadaId,
        IReadOnlyList<int>? bodegaIds,
        IReadOnlyList<string> permisos,
        CancellationToken cancellationToken = default)
    {
        // Validaciones comunes de nombre, correo, contraseña y rol
        var validation = Validate(nombre, email, password, rol, requirePassword: true, creatableOnly: true);
        if (validation is not null) return validation;

        var resolvedBodega = await ResolveBodegaIdsAsync(rol, bodegaIds, cancellationToken);
        if (!resolvedBodega.Success)
            return ServiceResult.Fail(resolvedBodega.Message ?? "Bodega no válida.");

        // No puede repetirse el correo
        if (await _userRepository.EmailExistsAsync(email.Trim(), null, cancellationToken))
            return ServiceResult.Fail("Ya existe un usuario con ese correo.");

        // Armo la entidad nueva
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
        ReplaceUserBodegas(user, resolvedBodega.BodegaIds);

        // INSERT en Users
        _userRepository.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        // Si es instructor con ficha, sincronizo ownership
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
        IReadOnlyList<int>? bodegaIds,
        IReadOnlyList<string> permisos,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        // Validación de campos; contraseña opcional en edición
        var validation = Validate(nombre, email, password, rol, requirePassword: false, creatableOnly: false);
        if (validation is not null) return validation;

        var resolvedBodega = await ResolveBodegaIdsAsync(rol, bodegaIds, cancellationToken);
        if (!resolvedBodega.Success)
            return ServiceResult.Fail(resolvedBodega.Message ?? "Bodega no válida.");

        // Busco el usuario que vamos a modificar
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null) return ServiceResult.Fail("Usuario no encontrado.");

        // El admin existente no se le puede bajar el rol
        var isExistingAdmin = string.Equals(user.Rol, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase);
        if (isExistingAdmin)
        {
            if (!string.Equals(rol, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase))
                return ServiceResult.Fail("No se puede cambiar el rol del administrador.");
        }
        else if (!UserRoles.CreatableByAdmin.Contains(rol))
        {
            return ServiceResult.Fail("Rol no válido para asignación desde administración.");
        }

        // Correo único excluyendo al propio usuario
        if (await _userRepository.EmailExistsAsync(email.Trim(), id, cancellationToken))
            return ServiceResult.Fail("Ya existe un usuario con ese correo.");

        // Actualizo campos del usuario
        user.Nombre = nombre.Trim();
        user.Email = email.Trim().ToLowerInvariant();
        user.Rol = rol;
        user.FichaAsignadaId = fichaAsignadaId;
        ReplaceUserBodegas(user, resolvedBodega.BodegaIds);
        user.PermisosExtendidos = ExtendedPermissions.Serialize(permisos);
        user.IsActive = isActive;

        // Contraseña opcional en edición
        if (!string.IsNullOrWhiteSpace(password))
            user.PasswordHash = PasswordHasher.Hash(password);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        // Si cambió ficha de instructor, actualizo la ficha en BD
        await SyncFichaOwnershipAsync(user.Id, user.Nombre, user.Rol, fichaAsignadaId, cancellationToken);
        return ServiceResult.Ok("Usuario actualizado correctamente.");
    }

    // Activar / desactivar sin borrar el usuario (así no pierde el historial)
    public async Task<ServiceResult> ToggleUserStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null) return ServiceResult.Fail("Usuario no encontrado.");

        // Solo cambio el flag; no borro la fila
        user.IsActive = isActive;
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok(isActive ? "Usuario activado." : "Usuario desactivado.");
    }

    // Gap #1: hard delete con protecciones (self, último admin activo, dependencias de auditoría)
    public async Task<ServiceResult> DeleteUserAsync(
        int id,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId <= 0)
            return ServiceResult.Fail("Usuario responsable no válido.");

        if (id == actorUserId)
            return ServiceResult.Fail("No puede eliminar su propia cuenta.");

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null) return ServiceResult.Fail("Usuario no encontrado.");

        var isAdmin = string.Equals(user.Rol, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase);
        if (isAdmin && user.IsActive)
        {
            var activeAdmins = await _userRepository.CountActiveAdministratorsAsync(cancellationToken);
            if (activeAdmins <= 1)
                return ServiceResult.Fail(
                    "No se puede eliminar al último administrador activo del sistema.");
        }

        var blockers = await _userRepository.GetDeletionBlockersAsync(id, cancellationToken);
        if (blockers.Count > 0)
        {
            return ServiceResult.Fail(
                "No se puede eliminar: el usuario tiene registros dependientes (" +
                string.Join("; ", blockers) +
                "). Desactive la cuenta en su lugar para preservar el historial.");
        }

        _userRepository.Remove(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Usuario «{user.Nombre}» eliminado.");
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
        // Nombre y correo son obligatorios siempre
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(email))
            return ServiceResult.Fail("Nombre y correo son obligatorios.");

        // Límite de texto en la descripción de funciones
        if (funcionDescripcion is { Length: > 800 })
            return ServiceResult.Fail("La descripción de funciones no puede superar 800 caracteres.");

        // Reglas de complejidad si mandaron clave nueva
        var passwordError = PasswordRules.Validate(newPassword, required: false);
        if (passwordError is not null)
            return ServiceResult.Fail(passwordError);

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null) return ServiceResult.Fail("Usuario no encontrado.");

        if (await _userRepository.EmailExistsAsync(email.Trim(), id, cancellationToken))
            return ServiceResult.Fail("Ya existe un usuario con ese correo.");

        // Datos básicos del perfil
        user.Nombre = nombre.Trim();
        user.Email = email.Trim().ToLowerInvariant();
        user.FuncionDescripcion = string.IsNullOrWhiteSpace(funcionDescripcion)
            ? null
            : funcionDescripcion.Trim();

        // Solo hasheo si escribieron contraseña nueva
        if (!string.IsNullOrWhiteSpace(newPassword))
            user.PasswordHash = PasswordHasher.Hash(newPassword);

        // Foto: quitar, actualizar o dejar como está
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

        // En creación solo roles que el admin puede asignar
        if (creatableOnly)
        {
            if (!UserRoles.CreatableByAdmin.Contains(rol))
                return ServiceResult.Fail("Rol no válido. Elija Administrador, Instructor o Bodeguero.");
        }
        else if (!UserRoles.All.Contains(rol))
        {
            return ServiceResult.Fail("Rol no válido.");
        }

        return null;
    }

    // Bodeguero exige ≥1 bodega existente; otros roles ignoran el valor y quedan sin asignaciones.
    private async Task<(bool Success, string? Message, IReadOnlyList<int> BodegaIds)> ResolveBodegaIdsAsync(
        string rol,
        IReadOnlyList<int>? bodegaIds,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(rol, UserRoles.Bodeguero, StringComparison.OrdinalIgnoreCase))
            return (true, null, []);

        var requested = (bodegaIds ?? [])
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        if (requested.Count == 0)
            return (false, "Debe asignar al menos una bodega al bodeguero.", []);

        var catalog = await _bodegaRepository.GetAllAsync(cancellationToken);
        var validIds = catalog.Select(b => b.Id).ToHashSet();
        if (requested.Any(id => !validIds.Contains(id)))
            return (false, "Bodega no válida.", []);

        return (true, null, requested);
    }

    private static void ReplaceUserBodegas(User user, IReadOnlyList<int> bodegaIds)
    {
        var desired = bodegaIds.Where(id => id > 0).Distinct().ToHashSet();
        var toRemove = user.UserBodegas.Where(ub => !desired.Contains(ub.BodegaId)).ToList();
        foreach (var row in toRemove)
            user.UserBodegas.Remove(row);

        var existing = user.UserBodegas.Select(ub => ub.BodegaId).ToHashSet();
        foreach (var id in desired.Where(id => !existing.Contains(id)))
        {
            var row = new UserBodega { BodegaId = id };
            if (user.Id > 0)
                row.UserId = user.Id;
            user.UserBodegas.Add(row);
        }
    }

    // Si es instructor con ficha asignada, actualizo la ficha para que apunte a ese usuario
    private async Task SyncFichaOwnershipAsync(
        int userId,
        string nombre,
        string rol,
        int? fichaAsignadaId,
        CancellationToken cancellationToken)
    {
        // Solo aplica para instructores con ficha
        if (!string.Equals(rol, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase)
            || fichaAsignadaId is not int fichaId)
            return;

        var ficha = await _fichaRepository.GetByIdAsync(fichaId, cancellationToken);
        if (ficha is null) return;

        // También refleja la asignación en la relación muchos-a-muchos
        if (!ficha.Instructors.Any(i => i.UserId == userId))
        {
            ficha.Instructors.Add(new FichaInstructor
            {
                FichaId = ficha.Id,
                UserId = userId,
                AssignedAtUtc = DateTime.UtcNow
            });
        }

        ficha.InstructorUserId = userId;
        var names = ficha.Instructors
            .Select(i => i.UserId == userId ? nombre : i.User?.Nombre)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList();
        if (names.Count == 0) names.Add(nombre);
        ficha.InstructorName = string.Join(", ", names!);

        _fichaRepository.Update(ficha);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
