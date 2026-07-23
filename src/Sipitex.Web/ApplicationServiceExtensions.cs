using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;

namespace Sipitex.Web;

public static class ApplicationServiceExtensions
{
    // Aquí se registran los servicios de la capa de aplicación para que puedan ser usados por los controladores.
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ProductionConsumptionService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IProductionOrderService, ProductionOrderService>();
        services.AddScoped<IMrpService, MrpService>();
        services.AddScoped<IFichaService, FichaService>();
        services.AddScoped<IQualityService, QualityService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IAlertService, AlertService>();
        return services;
    }
}
