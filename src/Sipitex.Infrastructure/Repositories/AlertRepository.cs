using Microsoft.EntityFrameworkCore; // Include, Where, ToListAsync...
using Sipitex.Application.DTOs; // AlertCatalog con los tipos de alerta
using Sipitex.Application.Interfaces.Repositories; // IAlertRepository
using Sipitex.Domain.Entities; // AlertPreference, AlertDelivery, User
using Sipitex.Domain.Enums; // AlertType
using Sipitex.Infrastructure.Persistence; // SipitexDbContext

namespace Sipitex.Infrastructure.Repositories;

// Repositorio de preferencias y envíos de alertas
public class AlertRepository : IAlertRepository
{
    private readonly SipitexDbContext _context; // Contexto EF inyectado

    public AlertRepository(SipitexDbContext context) => _context = context;

    // Trae todas las preferencias de un usuario (para la pantalla de configuración)
    public async Task<IReadOnlyList<AlertPreference>> GetPreferencesByUserAsync(int userId, CancellationToken cancellationToken = default) =>
        await _context.AlertPreferences.Where(p => p.UserId == userId).ToListAsync(cancellationToken);

    // Solo usuarios activos con esa alerta prendida
    public async Task<IReadOnlyList<AlertPreference>> GetEnabledPreferencesAsync(AlertType type, CancellationToken cancellationToken = default) =>
        await _context.AlertPreferences
            .Include(p => p.User) // Necesito el email del usuario para mandar correo
            .Where(p => p.AlertType == type && p.Enabled && p.User.IsActive)
            .ToListAsync(cancellationToken);

    // Actualiza o crea preferencias según el diccionario que manda la UI
    public async Task UpsertPreferencesAsync(int userId, IReadOnlyDictionary<AlertType, bool> preferences, CancellationToken cancellationToken = default)
    {
        var existing = await _context.AlertPreferences.Where(p => p.UserId == userId).ToListAsync(cancellationToken);
        foreach (var (type, enabled) in preferences) // Recorro lo que mandó el formulario
        {
            var pref = existing.FirstOrDefault(p => p.AlertType == type); // ¿Ya existe esa alerta?
            if (pref is null)
            {
                // No existía → la creo
                _context.AlertPreferences.Add(new AlertPreference
                {
                    UserId = userId,
                    AlertType = type,
                    Enabled = enabled
                });
            }
            else
            {
                pref.Enabled = enabled; // Solo cambio el switch on/off
            }
        }
    }

    // Registra que se envió una alerta (para el historial)
    public async Task AddDeliveryAsync(AlertDelivery delivery, CancellationToken cancellationToken = default) =>
        await _context.AlertDeliveries.AddAsync(delivery, cancellationToken);

    // Historial de alertas enviadas (para el panel de admin)
    public async Task<IReadOnlyList<AlertDelivery>> GetRecentDeliveriesAsync(int take, CancellationToken cancellationToken = default) =>
        await _context.AlertDeliveries
            .Include(d => d.User) // Muestro a quién se le mandó
            .OrderByDescending(d => d.SentAt) // Las más nuevas primero
            .Take(take) // Limito cuántas traigo
            .ToListAsync(cancellationToken);

    // Al crear un usuario nuevo, le pongo las prefs por defecto según su rol
    public async Task EnsureDefaultPreferencesAsync(User user, CancellationToken cancellationToken = default)
    {
        var existing = await _context.AlertPreferences.Where(p => p.UserId == user.Id).Select(p => p.AlertType).ToListAsync(cancellationToken);
        foreach (var item in AlertCatalog.All) // Catálogo fijo de tipos de alerta
        {
            if (existing.Contains(item.Type)) continue; // Ya la tiene, salto
            var enabledByRole = item.Roles.Contains(user.Rol); // Prendida si su rol aplica
            _context.AlertPreferences.Add(new AlertPreference
            {
                UserId = user.Id,
                AlertType = item.Type,
                Enabled = enabledByRole
            });
        }
    }
}
