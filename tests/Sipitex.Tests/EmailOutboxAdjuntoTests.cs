using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Infrastructure.Email;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Tests;

public class EmailOutboxAdjuntoTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"sipitex-outbox-{Guid.NewGuid():N}.db");
    private readonly SipitexDbContext _db;

    public EmailOutboxAdjuntoTests()
    {
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        _db = new SipitexDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public async Task Outbox_GuardaElNombreDelAdjunto_YNoLosBytes()
    {
        var sender = new EmailSender(
            Options.Create(new EmailOptions { Provider = "Outbox" }),
            NullLogger<EmailSender>.Instance,
            _db);
        var bytes = new byte[] { 9, 8, 7, 6, 5 };

        await sender.SendAsync(
            "destino-secreto@ejemplo.test",
            "Ana",
            "Acta ACT-0001",
            "Cuerpo sin el archivo",
            [new EmailAttachment("ACT-0001.pdf", "application/pdf", bytes)]);

        var fila = Assert.Single(_db.EmailOutboxMessages.AsNoTracking().ToList());
        Assert.Equal("ACT-0001.pdf", fila.AttachmentNames);
        Assert.Equal("Cuerpo sin el archivo", fila.Body);
        Assert.DoesNotContain(Convert.ToBase64String(bytes), fila.Body, StringComparison.Ordinal);
        Assert.DoesNotContain(Convert.ToBase64String(bytes), fila.AttachmentNames, StringComparison.Ordinal);
    }
}
