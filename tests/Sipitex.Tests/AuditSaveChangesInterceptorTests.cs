using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Tests;

public class AuditSaveChangesInterceptorTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"sipitex-audit-ix-{Guid.NewGuid():N}.db");

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        foreach (var suffix in new[] { "-shm", "-wal" })
        {
            var side = _dbPath + suffix;
            if (File.Exists(side)) File.Delete(side);
        }
    }

    private SipitexDbContext CreateDb(IAuditActorAccessor actor)
    {
        var interceptor = new AuditSaveChangesInterceptor(actor);
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .AddInterceptors(interceptor)
            .Options;
        var db = new SipitexDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task SavingChanges_ConActor_RegistraCreateDeMaterial()
    {
        var actor = new StubAuditActor { UserId = 7, UserName = "Ana Admin" };
        await using var db = CreateDb(actor);

        db.Materials.Add(new Material
        {
            Code = "mat-audit",
            Name = "Tela interceptor",
            PlantaInventarioId = 1,
            CostoAdquisicion = 3.5m
        });
        await db.SaveChangesAsync();

        var row = Assert.Single(db.ActivityLogs.ToList());
        Assert.Equal(7, row.UserId);
        Assert.Equal("Ana Admin", row.UserName);
        Assert.Equal(ActivityLogActions.Create, row.Action);
        Assert.Equal(ActivityLogEntities.Material, row.Entity);
        Assert.Contains("Tela interceptor", row.Details, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SavingChanges_SinActor_NoEscribeActivityLog()
    {
        var actor = new StubAuditActor { UserId = null, UserName = null };
        await using var db = CreateDb(actor);

        db.Materials.Add(new Material
        {
            Code = "mat-silent",
            Name = "Sin actor",
            PlantaInventarioId = 1
        });
        await db.SaveChangesAsync();

        Assert.Empty(db.ActivityLogs.ToList());
    }

    [Fact]
    public async Task SavingChanges_Update_IncluyeBeforeAfterJson()
    {
        var actor = new StubAuditActor { UserId = 3, UserName = "Laura" };
        await using var db = CreateDb(actor);

        var material = new Material
        {
            Code = "mat-upd",
            Name = "Antes",
            PlantaInventarioId = 1
        };
        db.Materials.Add(material);
        await db.SaveChangesAsync();
        db.ActivityLogs.RemoveRange(db.ActivityLogs);
        await db.SaveChangesAsync();

        material.Name = "Después";
        await db.SaveChangesAsync();

        var row = Assert.Single(db.ActivityLogs.ToList());
        Assert.Equal(ActivityLogActions.Update, row.Action);
        Assert.Contains("Antes", row.Details, StringComparison.Ordinal);
        Assert.Contains("Después", row.Details, StringComparison.Ordinal);
    }

    private sealed class StubAuditActor : IAuditActorAccessor
    {
        public int? UserId { get; set; }
        public string? UserName { get; set; }
    }
}
