using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;

namespace Sipitex.Web;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ProductionConsumptionService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IProductionOrderService, ProductionOrderService>();
        services.AddScoped<IMrpService, MrpService>();
        services.AddScoped<IFichaService, FichaService>();
        services.AddScoped<IQualityService, QualityService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IRequirementService, RequirementService>();
        return services;
    }
}
