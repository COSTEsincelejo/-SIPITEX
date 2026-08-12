using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Acá va todo lo de alertas: preferencias, revisar condiciones y mandar correos
public class AlertService : IAlertService
{
    // Repo para guardar preferencias y entregas de alertas
    private readonly IAlertRepository _alertRepository;
    // Repo de usuarios para validar que existan
    private readonly IUserRepository _userRepository;
    // Repo de materiales para detectar stock bajo
    private readonly IMaterialRepository _materialRepository;
    // Repo de solicitudes pendientes de bodega
    private readonly IMaterialRequestRepository _requestRepository;
    // Repo de órdenes de producción (vencimiento, atrasos)
    private readonly IProductionOrderRepository _orderRepository;
    // Repo de calidad para reprocesos recientes
    private readonly IQualityRepository _qualityRepository;
    // Servicio que manda el correo (SMTP o outbox)
    private readonly IEmailSender _emailSender;
    // Para persistir cambios en BD de una vez
    private readonly IUnitOfWork _unitOfWork;

    // Inyecto todos los repos y servicios que necesito
    public AlertService(
        IAlertRepository alertRepository,
        IUserRepository userRepository,
        IMaterialRepository materialRepository,
        IMaterialRequestRepository requestRepository,
        IProductionOrderRepository orderRepository,
        IQualityRepository qualityRepository,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork)
    {
        _alertRepository = alertRepository;
        _userRepository = userRepository;
        _materialRepository = materialRepository;
        _requestRepository = requestRepository;
        _orderRepository = orderRepository;
        _qualityRepository = qualityRepository;
        _emailSender = emailSender;
        _unitOfWork = unitOfWork;
    }

    // Con esto traigo las preferencias de alerta de un usuario
    public async Task<IReadOnlyList<AlertPreferenceDto>> GetPreferencesForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Acá reviso si el usuario existe, si no tiro excepción
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        // Si es la primera vez, le creo las preferencias por defecto
        await _alertRepository.EnsureDefaultPreferencesAsync(user, cancellationToken);
        // Guardo en BD lo que acabo de crear
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Traigo lo que tiene guardado en preferencias
        var prefs = await _alertRepository.GetPreferencesByUserAsync(userId, cancellationToken);
        // Lo paso a diccionario para buscar rápido por tipo
        var map = prefs.ToDictionary(p => p.AlertType, p => p.Enabled);

        // Cruzo el catálogo fijo con lo que el usuario tiene activado
        return AlertCatalog.All.Select(item => new AlertPreferenceDto(
            item.Type,
            item.Title,
            item.Description,
            // Si está en el mapa y enabled=true, queda activo
            map.TryGetValue(item.Type, out var enabled) && enabled,
            item.Roles)).ToList();
    }

    // Guarda qué alertas quiere recibir cada usuario
    public async Task SavePreferencesAsync(int userId, IReadOnlyDictionary<AlertType, bool> preferences, CancellationToken cancellationToken = default)
    {
        // Actualizo o inserto las preferencias en BD
        await _alertRepository.UpsertPreferencesAsync(userId, preferences, cancellationToken);
        // Persisto los cambios
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // Historial de correos/alertas enviados (para mostrar en la vista)
    public async Task<IReadOnlyList<AlertDeliveryDto>> GetRecentDeliveriesAsync(int take = 30, CancellationToken cancellationToken = default)
    {
        // Traigo los últimos N envíos desde el repo
        var items = await _alertRepository.GetRecentDeliveriesAsync(take, cancellationToken);
        // Los mapeo a DTO para la capa web
        return items.Select(i => new AlertDeliveryDto(i.AlertType, i.Subject, i.SentAt, i.Channel)).ToList();
    }

    // Disparo inmediato a userIds y/o rol, respetando preferencias
    public async Task<int> NotifyUsersAsync(
        AlertType type,
        string subject,
        string body,
        IReadOnlyList<int>? userIds = null,
        string? role = null,
        CancellationToken cancellationToken = default)
    {
        var candidates = new List<User>();

        if (userIds is { Count: > 0 })
        {
            foreach (var id in userIds.Distinct())
            {
                var user = await _userRepository.GetByIdAsync(id, cancellationToken);
                if (user is { IsActive: true })
                {
                    await _alertRepository.EnsureDefaultPreferencesAsync(user, cancellationToken);
                    candidates.Add(user);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var all = await _userRepository.GetAllAsync(cancellationToken);
            foreach (var user in all.Where(u =>
                         u.IsActive
                         && string.Equals(u.Rol, role, StringComparison.OrdinalIgnoreCase)))
            {
                if (candidates.Any(c => c.Id == user.Id)) continue;
                await _alertRepository.EnsureDefaultPreferencesAsync(user, cancellationToken);
                candidates.Add(user);
            }
        }

        // Persistimos prefs nuevas antes de consultar cuáles están enabled
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (candidates.Count == 0)
            return 0;

        var enabled = await _alertRepository.GetEnabledPreferencesAsync(type, cancellationToken);
        var enabledIds = enabled.Select(p => p.UserId).ToHashSet();
        var channel = _emailSender.IsSmtpConfigured ? "SMTP" : "Outbox";
        var sent = 0;

        foreach (var user in candidates.Where(u => enabledIds.Contains(u.Id)))
        {
            await _emailSender.SendAsync(user.Email, user.Nombre, subject, body, cancellationToken);
            await _alertRepository.AddDeliveryAsync(new AlertDelivery
            {
                UserId = user.Id,
                AlertType = type,
                Subject = subject,
                Body = body,
                SentAt = DateTime.Now,
                Channel = channel
            }, cancellationToken);
            sent++;
        }

        if (sent > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return sent;
    }

    // Revisa todas las condiciones del sistema y manda correos a quien tenga esa alerta activa
    public async Task<AlertEvaluationResultDto> EvaluateAndSendAsync(CancellationToken cancellationToken = default)
    {
        // Lista para ir anotando qué pasó en cada alerta
        var details = new List<string>();
        // Cuántas condiciones de alerta encontré
        var found = 0;
        // Cuántos correos mandé en total
        var sent = 0;

        // Armo los eventos según inventario, órdenes, calidad, etc.
        var events = await BuildAlertEventsAsync(cancellationToken);
        found = events.Count;

        // Recorro cada evento que salió de la evaluación
        foreach (var evt in events)
        {
            // Busco quién tiene activa esta alerta
            var recipients = await _alertRepository.GetEnabledPreferencesAsync(evt.Type, cancellationToken);
            // Si nadie la tiene prendida, sigo con la siguiente
            if (recipients.Count == 0)
            {
                details.Add($"{evt.Type}: sin suscriptores activos.");
                continue;
            }

            // Por cada usuario suscrito mando el correo
            foreach (var pref in recipients)
            {
                var user = pref.User;
                // Usuarios inactivos no reciben nada
                if (!user.IsActive) continue;

                // Si no hay SMTP configurado igual guardo el envío como Outbox
                var channel = _emailSender.IsSmtpConfigured ? "SMTP" : "Outbox";
                // Mando el correo con asunto y cuerpo del evento
                await _emailSender.SendAsync(user.Email, user.Nombre, evt.Subject, evt.Body, cancellationToken);
                // Registro el envío en historial
                await _alertRepository.AddDeliveryAsync(new AlertDelivery
                {
                    UserId = user.Id,
                    AlertType = evt.Type,
                    Subject = evt.Subject,
                    Body = evt.Body,
                    SentAt = DateTime.Now,
                    Channel = channel
                }, cancellationToken);
                sent++;
                details.Add($"{evt.Type} → {user.Email} ({channel})");
            }
        }

        // Guardo entregas y cualquier cambio pendiente
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        // Si no hubo alertas, lo dejo claro en el detalle
        if (found == 0) details.Add("No hay condiciones de alerta activas en este momento.");

        // Devuelvo resumen para mostrar en pantalla
        return new AlertEvaluationResultDto(found, sent, details);
    }

    // Arma la lista de "eventos" según lo que encuentre en inventario, órdenes, calidad, etc.
    private async Task<List<AlertEvent>> BuildAlertEventsAsync(CancellationToken cancellationToken)
    {
        var events = new List<AlertEvent>();
        // Fecha de hoy para comparar plazos
        var today = DateOnly.FromDateTime(DateTime.Today);

        // --- Stock bajo mínimo ---
        var materials = await _materialRepository.GetAllAsync(cancellationToken);
        // Filtro los que están por debajo del mínimo
        var low = materials.Where(m => m.Stock < m.MinStock).ToList();
        if (low.Count > 0)
        {
            // Armo líneas de texto para el cuerpo del correo
            var lines = string.Join("\n", low.Select(m => $"- {m.Name}: {m.Stock:0.##}/{m.MinStock:0.##}"));
            events.Add(new AlertEvent(
                AlertType.StockBajo,
                $"SIPITEX · {low.Count} material(es) bajo mínimo",
                $"Se detectó stock bajo:\n{lines}\n\nRevise Inventario."));
        }

        // --- Solicitudes de material sin aprobar ---
        var requests = await _requestRepository.GetAllAsync(cancellationToken);
        var pending = requests.Where(r => r.Status == RequestStatus.Pendiente).ToList();
        if (pending.Count > 0)
        {
            var lines = string.Join("\n", pending.Select(r => $"- {r.Material.Name} ({r.Quantity:0.##}) · {r.ProductionOrder.OrderNumber}"));
            events.Add(new AlertEvent(
                AlertType.SolicitudPendiente,
                $"SIPITEX · {pending.Count} solicitud(es) pendiente(s)",
                $"Solicitudes pendientes de bodega:\n{lines}\n\nApruebe o rechace en Inventario."));
        }

        // Traigo todas las órdenes para revisar plazos
        var orders = await _orderRepository.GetAllAsync(cancellationToken);
        // Vencimiento/atraso solo desde EnProceso (excluye Pendiente de aprobación)
        var active = orders.Where(o => o.Status == OrderStatus.EnProceso).ToList();

        // Órdenes que vencen en 7 días o menos
        var dueSoon = active.Where(o => o.Deadline <= today.AddDays(7)).ToList();
        if (dueSoon.Count > 0)
        {
            var lines = string.Join("\n", dueSoon.Select(o => $"- {o.OrderNumber} ({o.ProductName}) vence {o.Deadline:yyyy-MM-dd} · avance {o.ProducedQuantity}/{o.TotalQuantity}"));
            events.Add(new AlertEvent(
                AlertType.OrdenPorVencer,
                $"SIPITEX · {dueSoon.Count} orden(es) por vencer",
                $"Órdenes con plazo ≤ 7 días:\n{lines}"));
        }

        // Atrasadas: menos del 50% de avance y plazo en 14 días
        var delayed = active.Where(o =>
            o.TotalQuantity > 0 &&
            (o.ProducedQuantity * 100.0 / o.TotalQuantity) < 50 &&
            o.Deadline <= today.AddDays(14)).ToList();
        if (delayed.Count > 0)
        {
            var lines = string.Join("\n", delayed.Select(o => $"- {o.OrderNumber}: {o.ProducedQuantity}/{o.TotalQuantity} ({o.ProducedQuantity * 100 / o.TotalQuantity}%)"));
            events.Add(new AlertEvent(
                AlertType.OrdenAtrasada,
                $"SIPITEX · {delayed.Count} orden(es) atrasada(s)",
                $"Órdenes con avance < 50% y plazo próximo:\n{lines}"));
        }

        // Reprocesos de calidad de la última semana
        var quality = await _qualityRepository.GetAllAsync(cancellationToken);
        var reprocesos = quality
            .Where(q => q.Result == QualityResult.Reproceso && q.InspectionDate >= today.AddDays(-7))
            .ToList();
        if (reprocesos.Count > 0)
        {
            var lines = string.Join("\n", reprocesos.Select(q =>
                $"- {q.ProductionOrder.OrderNumber}: {q.UnitsInspected} uds · {q.MotivoReproceso ?? "Sin motivo"} · {q.Responsable ?? "N/D"}"));
            events.Add(new AlertEvent(
                AlertType.ReprocesoCalidad,
                $"SIPITEX · {reprocesos.Count} reproceso(s) reciente(s)",
                $"Reprocesos de los últimos 7 días:\n{lines}"));
        }

        return events;
    }

    // DTO interno para pasar tipo + asunto + cuerpo del correo
    private sealed record AlertEvent(AlertType Type, string Subject, string Body);
}
