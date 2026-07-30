using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;

namespace Sipitex.Web;

// Método de extensión para registrar servicios sin llenar Program.cs de líneas
public static class ApplicationServiceExtensions
{
    // Aquí se registran los servicios de la capa de aplicación para que puedan ser usados por los controladores.
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Consumo de materiales en producción
        services.AddScoped<ProductionConsumptionService>();

        // Inventario y solicitudes de material
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IProductionOrderService, ProductionOrderService>();

        // MRP y fichas de producción
        services.AddScoped<IMrpService, MrpService>();
        services.AddScoped<IFichaService, FichaService>();

        services.AddScoped<IQualityService, QualityService>();
        services.AddScoped<IStatisticsService, StatisticsService>();

        // Cuentas de usuario y recuperar contraseña
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();

        services.AddScoped<IAlertService, AlertService>();

        return services;
    }
}
