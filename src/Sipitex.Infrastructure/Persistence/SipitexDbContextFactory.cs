using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sipitex.Infrastructure.Persistence;

public sealed class SipitexDbContextFactory : IDesignTimeDbContextFactory<SipitexDbContext>
{
    public SipitexDbContext CreateDbContext(string[] args)
    {
        var fromEnv = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DATABASE_URL");
        var connection = string.IsNullOrWhiteSpace(fromEnv)
            ? PostgresDefaults.LocalConnectionString
            : PostgresConnection.Normalize(fromEnv);
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseNpgsql(connection)
            .Options;
        return new SipitexDbContext(options);
    }
}
