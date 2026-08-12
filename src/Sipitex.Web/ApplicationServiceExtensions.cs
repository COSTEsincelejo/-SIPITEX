using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;

namespace Sipitex.Web;

// Método de extensión para registrar servicios sin llenar Program.cs de líneas
public static class ApplicationServiceExtensions
{
    // Registra los servicios de aplicación en el contenedor de DI
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Consumo de materiales en producción
        services.AddScoped<ProductionConsumptionService>();

        // Inventario y solicitudes de material (MaterialRequest legacy)
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IStockMovementService, StockMovementService>();
        services.AddScoped<IProductionOrderService, ProductionOrderService>();
        services.AddScoped<IOrderMaterialService, OrderMaterialService>();
        services.AddScoped<IProductionFlowService, ProductionFlowService>();

        // SolicitudMaterial (flujo Ficha multi-ítem; paralelo a MaterialRequest)
        services.AddScoped<ICodigoGeneradorService, CodigoGeneradorService>();
        services.AddScoped<ISolicitudMaterialApprovalService, SolicitudMaterialApprovalService>();
        services.AddScoped<ISolicitudMaterialService, SolicitudMaterialService>();

        // MRP y fichas de producción
        services.AddScoped<IMrpService, MrpService>();
        services.AddScoped<IBomCatalogService, BomCatalogService>();
        services.AddScoped<IFichaService, FichaService>();

        // Control de calidad y estadísticas del dashboard
        services.AddScoped<IQualityService, QualityService>();
        services.AddScoped<IStatisticsService, StatisticsService>();

        // Cuentas de usuario y recuperar contraseña
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();

        // Alertas por correo y evaluación programada
        services.AddScoped<IAlertService, AlertService>();

        return services;
    }
}
