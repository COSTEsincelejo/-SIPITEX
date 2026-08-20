using Microsoft.EntityFrameworkCore; // Para registrar el DbContext
using Microsoft.Extensions.Configuration; // Leo appsettings (connection string, email...)
using Microsoft.Extensions.DependencyInjection; // Contenedor DI de ASP.NET
using Sipitex.Application.Interfaces; // IUnitOfWork
using Sipitex.Application.Interfaces.Repositories; // Interfaces de los repos
using Sipitex.Application.Interfaces.Services; // IEmailSender, IReportService
using Sipitex.Infrastructure.Email; // Implementación del correo
using Sipitex.Infrastructure.Persistence; // SipitexDbContext
using Sipitex.Infrastructure.Reporting; // ReportService
using Sipitex.Infrastructure.Repositories; // Todos los repositorios
using Sipitex.Infrastructure.Search; // BusquedaService
using Sipitex.Infrastructure.Services; // ActivityLogService (auditoría global)

namespace Sipitex.Infrastructure;

// Registro de servicios de la capa Infrastructure en el contenedor DI de ASP.NET
public static class DependencyInjection
{
    // Método de extensión que llama Program.cs para cablear todo
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Leo la cadena de conexión del appsettings, si no hay uso el default
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=sipitex.db";

        // EF Core con SQLite — el archivo sipitex.db queda en la raíz del proyecto
        services.AddDbContext<SipitexDbContext>(options =>
            options.UseSqlite(connectionString));

        // Opciones de correo desde la sección "Email" del appsettings
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddScoped<IEmailSender, EmailSender>(); // Un EmailSender por request
        services.AddScoped<IReportService, ReportService>(); // Reportes Excel/PDF
        services.AddScoped<IFuncionalidadesReportService, FuncionalidadesReportService>(); // Catálogo Word
        services.AddScoped<IBusquedaService, BusquedaService>(); // Búsqueda global del header

        // Repositorios scoped = una instancia por request HTTP
        services.AddScoped<IUnitOfWork, UnitOfWork>(); // Guarda cambios al final del request
        services.AddScoped<IMaterialRepository, MaterialRepository>();
        services.AddScoped<IProductionOrderRepository, ProductionOrderRepository>();
        services.AddScoped<IBomRepository, BomRepository>();
        services.AddScoped<IProductionOrderBomSnapshotRepository, ProductionOrderBomSnapshotRepository>();
        services.AddScoped<IMaterialRequestRepository, MaterialRequestRepository>();
        services.AddScoped<IFichaRepository, FichaRepository>();
        services.AddScoped<IBodegaRepository, BodegaRepository>(); // Catálogo Bodega 1 / Bodega 2
        services.AddScoped<IQualityRepository, QualityRepository>();
        services.AddScoped<IRequirementRepository, RequirementRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IProductionSessionRepository, ProductionSessionRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<ISolicitudMaterialRepository, SolicitudMaterialRepository>(); // Flujo SolicitudMaterial (Ficha)
        services.AddScoped<IOrderMaterialRequirementRepository, OrderMaterialRequirementRepository>(); // Materiales por orden
        services.AddScoped<IProductionFlowRepository, ProductionFlowRepository>(); // Flujo MES
        services.AddScoped<IStockMovementRepository, StockMovementRepository>(); // Historial de stock
        services.AddScoped<IOrderChangeLogRepository, OrderChangeLogRepository>(); // Auditoría ediciones de orden
        services.AddScoped<IActivityLogService, ActivityLogService>(); // Auditoría global transversal

        return services; // Devuelvo la colección ya configurada
    }
}
