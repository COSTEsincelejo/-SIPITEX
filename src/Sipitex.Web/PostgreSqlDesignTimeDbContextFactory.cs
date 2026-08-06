using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Sipitex.Infrastructure;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Web;

/// <summary>
/// Factory de diseño para generar migraciones EF Core contra PostgreSQL.
/// </summary>
public sealed class PostgreSqlDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SipitexDbContext>
{
    public SipitexDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("SIPITEX_POSTGRES")
            ?? "Host=localhost;Port=5432;Database=sipitex;Username=sipitex;Password=sipitex";

        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(DependencyInjection.PostgreSqlMigrationsAssembly))
            .Options;

        return new SipitexDbContext(options);
    }
}
