using Microsoft.EntityFrameworkCore; // Include, Where, AnyAsync...
using Sipitex.Application.Interfaces.Repositories; // IUserRepository
using Sipitex.Domain.Entities; // User
using Sipitex.Infrastructure.Persistence; // SipitexDbContext

namespace Sipitex.Infrastructure.Repositories;

// CRUD de usuarios y búsquedas para login
public class UserRepository : IUserRepository
{
    private readonly SipitexDbContext _context;

    public UserRepository(SipitexDbContext context) => _context = context;

    // Todos los usuarios con su ficha asignada, ordenados por nombre
    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Users
            .Include(u => u.FichaAsignada) // Ficha principal del instructor
            .OrderBy(u => u.Nombre)
            .ToListAsync(cancellationToken);

    // Busca usuario por Id (para editar perfil o ver detalle)
    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Users
            .Include(u => u.FichaAsignada)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    // Normalizo el email a minúsculas para que el login sea case-insensitive
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Users
            .Include(u => u.FichaAsignada)
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant(), cancellationToken);

    // excludeUserId sirve al editar: no contar el propio email como duplicado
    public Task<bool> EmailExistsAsync(string email, int? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant(); // Misma normalización que en login
        var query = _context.Users.AsQueryable().Where(u => u.Email == normalized);
        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value); // Ignoro al usuario que estoy editando
        return query.AnyAsync(cancellationToken); // true si ya existe otro con ese email
    }

    // Agrega un usuario nuevo
    public void Add(User user) => _context.Users.Add(user);

    // Actualiza datos del usuario (nombre, rol, foto...)
    public void Update(User user) => _context.Users.Update(user);
}
