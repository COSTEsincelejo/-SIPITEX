using Sipitex.Application.DTOs;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Interfaces.Services;

// Preferencias de alerta, evaluación y envío de notificaciones
public interface IAlertService
{
    // Traer preferencias del usuario (crea defaults si es primera vez)
    Task<IReadOnlyList<AlertPreferenceDto>> GetPreferencesForUserAsync(int userId, CancellationToken cancellationToken = default);
    // Guardar qué alertas tiene activas
    Task SavePreferencesAsync(int userId, IReadOnlyDictionary<AlertType, bool> preferences, CancellationToken cancellationToken = default);
    // Evaluar condiciones y mandar correos
    Task<AlertEvaluationResultDto> EvaluateAndSendAsync(CancellationToken cancellationToken = default);
    // Historial de envíos recientes
    Task<IReadOnlyList<AlertDeliveryDto>> GetRecentDeliveriesAsync(int take = 30, CancellationToken cancellationToken = default);
}
