using Microsoft.Extensions.Options;
using Moq;
using Sipitex.Application;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class CostingSettingsServiceTests
{
    private readonly Mock<IAppSettingRepository> _settings = new();
    private readonly Mock<IActivityLogService> _log = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private CostingSettingsService CreateSut(decimal fallback) => new(
        _settings.Object,
        _log.Object,
        _uow.Object,
        Options.Create(new CostingOptions { LaborHourRate = fallback }));

    [Fact]
    public async Task GetLaborHourRate_UsaAppSettingsSiNoHayValorPersistido()
    {
        _settings.Setup(r => r.GetAsync(CostingSettingsService.LaborHourRateKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSetting?)null);

        var rate = await CreateSut(6500).GetLaborHourRateAsync();

        Assert.Equal(6500, rate);
    }

    [Fact]
    public async Task GetLaborHourRate_PrefiereValorPersistido()
    {
        _settings.Setup(r => r.GetAsync(CostingSettingsService.LaborHourRateKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSetting { Key = CostingSettingsService.LaborHourRateKey, Value = "8000.5" });

        var rate = await CreateSut(6500).GetLaborHourRateAsync();

        Assert.Equal(8000.5m, rate);
    }

    [Fact]
    public async Task UpdateLaborHourRate_Cero_Falla()
    {
        var result = await CreateSut(6500).UpdateLaborHourRateAsync(0, 1);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateLaborHourRate_GuardaYAudita()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut(6500).UpdateLaborHourRateAsync(7200, 9);

        Assert.True(result.Success);
        Assert.Equal(7200, result.Value);
        _settings.Verify(r => r.UpsertAsync(
            CostingSettingsService.LaborHourRateKey,
            "7200",
            It.IsAny<CancellationToken>()), Times.Once);
        _log.Verify(l => l.LogAsync(
            9,
            ActivityLogActions.UpdateLaborHourRate,
            ActivityLogEntities.AppSetting,
            CostingSettingsService.LaborHourRateKey,
            "7200",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
