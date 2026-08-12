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

    public void Remove(User user) => _context.Users.Remove(user);

    public Task<int> CountActiveAdministratorsAsync(CancellationToken cancellationToken = default) =>
        _context.Users.CountAsync(
            u => u.IsActive && u.Rol == UserRoles.Administrador,
            cancellationToken);

    public async Task<IReadOnlyList<string>> GetDeletionBlockersAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var blockers = new List<string>();

        if (await _context.StockMovements.AnyAsync(m => m.UsuarioId == userId, cancellationToken))
            blockers.Add("movimientos de inventario (StockMovement)");
        if (await _context.OrderChangeLogs.AnyAsync(c => c.UsuarioId == userId, cancellationToken))
            blockers.Add("historial de edición de órdenes (OrderChangeLog)");
        if (await _context.FinishedGoodMovements.AnyAsync(m => m.ActorUserId == userId, cancellationToken))
            blockers.Add("movimientos de producto terminado");
        if (await _context.ProductionOrderStageMovements.AnyAsync(
                m => m.ActorUserId == userId || m.AuthorizedByUserId == userId, cancellationToken))
            blockers.Add("movimientos de etapas MES");
        if (await _context.ProductionOrderHistoryEntries.AnyAsync(h => h.ActorUserId == userId, cancellationToken))
            blockers.Add("historial MES de órdenes");
        if (await _context.SolicitudesMaterial.AnyAsync(
                s => s.SolicitanteId == userId || s.ResueltoPorId == userId, cancellationToken))
            blockers.Add("solicitudes de material (solicitante o resolución)");
        if (await _context.MaterialRequests.AnyAsync(r => r.SolicitanteId == userId, cancellationToken))
            blockers.Add("solicitudes legacy de inventario (MaterialRequest)");
        if (await _context.EntregasMaterial.AnyAsync(e => e.BodegueroId == userId, cancellationToken))
            blockers.Add("entregas de material registradas");
        if (await _context.Fichas.AnyAsync(f => f.InstructorUserId == userId, cancellationToken))
            blockers.Add("fichas con instructor principal asignado");
        if (await _context.FichaInstructors.AnyAsync(fi => fi.UserId == userId, cancellationToken))
            blockers.Add("asignaciones instructor–ficha");
        if (await _context.BomProductInstructors.AnyAsync(bi => bi.UserId == userId, cancellationToken))
            blockers.Add("asignaciones instructor–ficha técnica (BOM)");
        if (await _context.ProductionOrderStages.AnyAsync(s => s.InstructorUserId == userId, cancellationToken))
            blockers.Add("etapas MES con instructor asignado");
        if (await _context.ProductionSessions.AnyAsync(s => s.RegisteredByUserId == userId, cancellationToken))
            blockers.Add("sesiones de producción registradas");
        if (await _context.InstructorStagePermissions.AnyAsync(p => p.UserId == userId, cancellationToken))
            blockers.Add("permisos de etapa por instructor");

        return blockers;
    }
}
