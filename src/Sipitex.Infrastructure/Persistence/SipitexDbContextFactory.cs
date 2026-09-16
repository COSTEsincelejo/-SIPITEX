using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sipitex.Infrastructure.Persistence;

public sealed class SipitexDbContextFactory : IDesignTimeDbContextFactory<SipitexDbContext>
{
    public SipitexDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseNpgsql(PostgresDefaults.LocalConnectionString)
            .Options;
        return new SipitexDbContext(options);
    }
}
