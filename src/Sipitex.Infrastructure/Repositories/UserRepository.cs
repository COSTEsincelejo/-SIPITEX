using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly SipitexDbContext _context;

    public UserRepository(SipitexDbContext context) => _context = context;

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Users
            .Include(u => u.FichaAsignada)
            .OrderBy(u => u.Nombre)
            .ToListAsync(cancellationToken);

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
        var normalized = email.Trim().ToLowerInvariant();
        var query = _context.Users.AsQueryable().Where(u => u.Email == normalized);
        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public void Add(User user) => _context.Users.Add(user);

    public void Update(User user) => _context.Users.Update(user);
}
