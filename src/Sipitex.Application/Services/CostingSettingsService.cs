using Microsoft.Extensions.Options;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Application.Services;

public class CostingSettingsService : ICostingSettingsService
{
    public const string LaborHourRateKey = "Costing.LaborHourRate";

    private readonly IAppSettingRepository _settings;
    private readonly IActivityLogService _activityLog;
    private readonly IUnitOfWork _uow;
    private readonly CostingOptions _options;

    public CostingSettingsService(
        IAppSettingRepository settings,
        IActivityLogService activityLog,
        IUnitOfWork uow,
        IOptions<CostingOptions> options)
    {
        _settings = settings;
        _activityLog = activityLog;
        _uow = uow;
        _options = options.Value;
    }

    public async Task<decimal> GetLaborHourRateAsync(CancellationToken cancellationToken = default)
    {
        var stored = await _settings.GetAsync(LaborHourRateKey, cancellationToken);
        if (stored is not null
            && decimal.TryParse(
                stored.Value,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var rate)
            && rate > 0)
        {
            return decimal.Round(rate, 4, MidpointRounding.AwayFromZero);
        }

        return _options.LaborHourRate;
    }

    public async Task<ServiceResult<decimal>> UpdateLaborHourRateAsync(
        decimal rate,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (rate <= 0)
            return ServiceResult<decimal>.Fail("La tarifa de hora de mano de obra debe ser mayor que cero.");

        var rounded = decimal.Round(rate, 4, MidpointRounding.AwayFromZero);
        var value = rounded.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await _settings.UpsertAsync(LaborHourRateKey, value, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        if (actorUserId > 0)
        {
            await _activityLog.LogAsync(
                actorUserId,
                ActivityLogActions.UpdateLaborHourRate,
                ActivityLogEntities.AppSetting,
                entityId: LaborHourRateKey,
                details: value,
                cancellationToken);
        }

        return ServiceResult<decimal>.Ok(rounded, $"Tarifa de mano de obra actualizada a {rounded.ToString("0.####")} / h.");
    }
}
