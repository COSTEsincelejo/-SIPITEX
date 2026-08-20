using Sipitex.Application.DTOs;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Services;

// Autenticación y administración de usuarios
public interface IUserAccountService
{
    Task<User?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateUserAsync(
        string nombre,
        string email,
        string password,
        string rol,
        int? fichaAsignadaId,
        int? bodegaId,
        IReadOnlyList<string> permisos,
        CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateUserAsync(
        int id,
        string nombre,
        string email,
        string password,
        string rol,
        int? fichaAsignadaId,
        int? bodegaId,
        IReadOnlyList<string> permisos,
        bool isActive,
        CancellationToken cancellationToken = default);
    Task<ServiceResult> ToggleUserStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);

    // Hard delete (gap #1). actorUserId = quien ejecuta la acción (anti auto-borrado).
    Task<ServiceResult> DeleteUserAsync(int id, int actorUserId, CancellationToken cancellationToken = default);

    // removePhoto = quitar foto del perfil sin subir una nueva
    Task<ServiceResult> UpdateProfileAsync(
        int id,
        string nombre,
        string email,
        string? funcionDescripcion,
        string? newPassword,
        string? photoPath,
        bool removePhoto,
        CancellationToken cancellationToken = default);
}
