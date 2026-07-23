using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

public class AlertService : IAlertService
{
    private readonly IAlertRepository _alertRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMaterialRepository _materialRepository;
    private readonly IMaterialRequestRepository _requestRepository;
    private readonly IProductionOrderRepository _orderRepository;
    private readonly IQualityRepository _qualityRepository;
    private readonly IEmailSender _emailSender;
    private readonly IUnitOfWork _unitOfWork;

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

    public async Task<IReadOnlyList<AlertPreferenceDto>> GetPreferencesForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        await _alertRepository.EnsureDefaultPreferencesAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var prefs = await _alertRepository.GetPreferencesByUserAsync(userId, cancellationToken);
        var map = prefs.ToDictionary(p => p.AlertType, p => p.Enabled);

        return AlertCatalog.All.Select(item => new AlertPreferenceDto(
            item.Type,
            item.Title,
            item.Description,
            map.TryGetValue(item.Type, out var enabled) && enabled,
            item.Roles)).ToList();
    }

    public async Task SavePreferencesAsync(int userId, IReadOnlyDictionary<AlertType, bool> preferences, CancellationToken cancellationToken = default)
    {
        await _alertRepository.UpsertPreferencesAsync(userId, preferences, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AlertDeliveryDto>> GetRecentDeliveriesAsync(int take = 30, CancellationToken cancellationToken = default)
    {
        var items = await _alertRepository.GetRecentDeliveriesAsync(take, cancellationToken);
        return items.Select(i => new AlertDeliveryDto(i.AlertType, i.Subject, i.SentAt, i.Channel)).ToList();
    }

    public async Task<AlertEvaluationResultDto> EvaluateAndSendAsync(CancellationToken cancellationToken = default)
    {
        var details = new List<string>();
        var found = 0;
        var sent = 0;

        var events = await BuildAlertEventsAsync(cancellationToken);
        found = events.Count;

        foreach (var evt in events)
        {
            var recipients = await _alertRepository.GetEnabledPreferencesAsync(evt.Type, cancellationToken);
            if (recipients.Count == 0)
            {
                details.Add($"{evt.Type}: sin suscriptores activos.");
                continue;
            }

            foreach (var pref in recipients)
            {
                var user = pref.User;
                if (!user.IsActive) continue;

                var channel = _emailSender.IsSmtpConfigured ? "SMTP" : "Outbox";
                await _emailSender.SendAsync(user.Email, user.Nombre, evt.Subject, evt.Body, cancellationToken);
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

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (found == 0) details.Add("No hay condiciones de alerta activas en este momento.");

        return new AlertEvaluationResultDto(found, sent, details);
    }

    private async Task<List<AlertEvent>> BuildAlertEventsAsync(CancellationToken cancellationToken)
    {
        var events = new List<AlertEvent>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var materials = await _materialRepository.GetAllAsync(cancellationToken);
        var low = materials.Where(m => m.Stock < m.MinStock).ToList();
        if (low.Count > 0)
        {
            var lines = string.Join("\n", low.Select(m => $"- {m.Name}: {m.Stock:0.##}/{m.MinStock:0.##}"));
            events.Add(new AlertEvent(
                AlertType.StockBajo,
                $"SIPITEX · {low.Count} material(es) bajo mínimo",
                $"Se detectó stock bajo:\n{lines}\n\nRevise Inventario."));
        }

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

        var orders = await _orderRepository.GetAllAsync(cancellationToken);
        var active = orders.Where(o => o.Status is not OrderStatus.Finalizada and not OrderStatus.Cancelada).ToList();
        var dueSoon = active.Where(o => o.Deadline <= today.AddDays(7)).ToList();
        if (dueSoon.Count > 0)
        {
            var lines = string.Join("\n", dueSoon.Select(o => $"- {o.OrderNumber} ({o.ProductName}) vence {o.Deadline:yyyy-MM-dd} · avance {o.ProducedQuantity}/{o.TotalQuantity}"));
            events.Add(new AlertEvent(
                AlertType.OrdenPorVencer,
                $"SIPITEX · {dueSoon.Count} orden(es) por vencer",
                $"Órdenes con plazo ≤ 7 días:\n{lines}"));
        }

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

    private sealed record AlertEvent(AlertType Type, string Subject, string Body);
}
