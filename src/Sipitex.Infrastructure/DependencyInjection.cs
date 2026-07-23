using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Infrastructure.Email;
using Sipitex.Infrastructure.Persistence;
using Sipitex.Infrastructure.Reporting;
using Sipitex.Infrastructure.Repositories;

namespace Sipitex.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=sipitex.db";

        services.AddDbContext<SipitexDbContext>(options =>
            options.UseSqlite(connectionString));

        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IReportService, ReportService>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IMaterialRepository, MaterialRepository>();
        services.AddScoped<IProductionOrderRepository, ProductionOrderRepository>();
        services.AddScoped<IBomRepository, BomRepository>();
        services.AddScoped<IMaterialRequestRepository, MaterialRequestRepository>();
        services.AddScoped<IFichaRepository, FichaRepository>();
        services.AddScoped<IQualityRepository, QualityRepository>();
        services.AddScoped<IRequirementRepository, RequirementRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProductionSessionRepository, ProductionSessionRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();

        return services;
    }
}
