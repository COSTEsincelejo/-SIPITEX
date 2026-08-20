using Microsoft.EntityFrameworkCore;
using Moq;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Persistence;
using Sipitex.Infrastructure.Repositories;

namespace Sipitex.Tests.Integration;

/// <summary>
/// Fixture de integración: SQLite temporal en %TEMP% por instancia (un test = un archivo).
/// No usa sipitex.db ni rutas del proyecto. Cleanup borra .db + -wal/-shm.
/// </summary>
public sealed class SolicitudMaterialFlowFixture : IAsyncDisposable
{
    private readonly string _dbPath;
    private bool _disposed;

    private SolicitudMaterialFlowFixture(string dbPath, SipitexDbContext context)
    {
        _dbPath = dbPath;
        Context = context;
    }

    public string DbPath => _dbPath;

    public SipitexDbContext Context { get; }

    // --- Seed IDs (rellenados en SeedAsync) ---
    public int AdminId { get; private set; }
    public int InstructorId { get; private set; }
    public int BodegueroId { get; private set; }
    public int FichaAsignadaId { get; private set; }
    public int FichaAjenaId { get; private set; }
    public int MaterialAmplioId { get; private set; }
    public int MaterialJustoId { get; private set; }
    public int MaterialCeroId { get; private set; }

    public decimal StockAmplioInicial { get; private set; } = 100m;
    public decimal StockJustoInicial { get; private set; } = 5m;
    public decimal StockCeroInicial { get; private set; } = 0m;

    // --- Servicios reales + email mock ---
    public Mock<IEmailSender> EmailMock { get; private set; } = null!;
    public ISolicitudMaterialService SolicitudService { get; private set; } = null!;
    public ISolicitudMaterialApprovalService ApprovalService { get; private set; } = null!;
    public ICodigoGeneradorService CodigoGenerador { get; private set; } = null!;
    public IAlertService AlertService { get; private set; } = null!;

    /// <summary>
    /// Crea BD temp → MigrateAsync → Seed → cablea servicios. Un archivo por llamada.
    /// </summary>
    public static async Task<SolicitudMaterialFlowFixture> CreateAsync(
        CancellationToken cancellationToken = default)
    {
        var dbPath = Path.Combine(
            Path.GetTempPath(),
            $"sipitex-solicitud-int-{Guid.NewGuid():N}.db");

        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        var context = new SipitexDbContext(options);
        await context.Database.MigrateAsync(cancellationToken);

        var fixture = new SolicitudMaterialFlowFixture(dbPath, context);
        await fixture.SeedAsync(cancellationToken);
        fixture.WireServices();
        return fixture;
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        var admin = new User
        {
            Nombre = "Admin Test",
            Email = "admin.int@test.local",
            PasswordHash = PasswordHasher.Hash("Admin123!"),
            Rol = UserRoles.Administrador,
            IsActive = true
        };
        var instructor = new User
        {
            Nombre = "Instructor Test",
            Email = "instructor.int@test.local",
            PasswordHash = PasswordHasher.Hash("Instr123!"),
            Rol = UserRoles.Instructor,
            IsActive = true
        };
        var bodeguero = new User
        {
            Nombre = "Bodeguero Test",
            Email = "bodega.int@test.local",
            PasswordHash = PasswordHasher.Hash("Bodega123!"),
            Rol = UserRoles.Bodeguero,
            BodegaId = 1,
            IsActive = true
        };

        Context.Users.AddRange(admin, instructor, bodeguero);
        await Context.SaveChangesAsync(cancellationToken);

        AdminId = admin.Id;
        InstructorId = instructor.Id;
        BodegueroId = bodeguero.Id;

        var matAmplio = new Material
        {
            Code = "mat-amplio",
            Name = "Tela amplio",
            Unit = MaterialUnit.Metros,
            Stock = StockAmplioInicial,
            MinStock = 10,
            Status = MaterialStatus.Bueno,
            LastEntryDate = DateOnly.FromDateTime(DateTime.Today),
            BodegaId = 1
        };
        var matJusto = new Material
        {
            Code = "mat-justo",
            Name = "Hilo justo",
            Unit = MaterialUnit.Unidades,
            Stock = StockJustoInicial,
            MinStock = 1,
            Status = MaterialStatus.Bueno,
            LastEntryDate = DateOnly.FromDateTime(DateTime.Today),
            BodegaId = 1
        };
        var matCero = new Material
        {
            Code = "mat-cero",
            Name = "Botones cero",
            Unit = MaterialUnit.Unidades,
            Stock = StockCeroInicial,
            MinStock = 1,
            Status = MaterialStatus.Bueno,
            LastEntryDate = DateOnly.FromDateTime(DateTime.Today),
            BodegaId = 1
        };

        Context.Materials.AddRange(matAmplio, matJusto, matCero);
        await Context.SaveChangesAsync(cancellationToken);

        MaterialAmplioId = matAmplio.Id;
        MaterialJustoId = matJusto.Id;
        MaterialCeroId = matCero.Id;

        // Ficha donde SÍ está el instructor (con Proceso)
        var ficha = new Ficha
        {
            FichaCode = "FICHA-INT-1",
            ProcessName = "Confección",
            InstructorName = instructor.Nombre,
            InstructorUserId = instructor.Id,
            Turno = "Mañana"
        };
        ficha.Instructors.Add(new FichaInstructor
        {
            UserId = instructor.Id,
            AssignedAtUtc = DateTime.UtcNow,
            Proceso = "Corte"
        });

        // Ficha ajena (otro instructor ficticio / sin el instructor del seed)
        var fichaAjena = new Ficha
        {
            FichaCode = "FICHA-INT-AJENA",
            ProcessName = "Trazo",
            InstructorName = "Otro Instructor",
            InstructorUserId = null,
            Turno = "Tarde"
        };

        Context.Fichas.AddRange(ficha, fichaAjena);
        await Context.SaveChangesAsync(cancellationToken);

        FichaAsignadaId = ficha.Id;
        FichaAjenaId = fichaAjena.Id;
        instructor.FichaAsignadaId = ficha.Id;
        await Context.SaveChangesAsync(cancellationToken);
    }

    private void WireServices()
    {
        var uow = new UnitOfWork(Context);
        var solicitudRepo = new SolicitudMaterialRepository(Context);
        var fichaRepo = new FichaRepository(Context);
        var materialRepo = new MaterialRepository(Context);
        var userRepo = new UserRepository(Context);
        var alertRepo = new AlertRepository(Context);
        // Repos que AlertService exige pero NotifyUsersAsync no usa en evaluación
        var materialRequestRepo = new MaterialRequestRepository(Context);
        var orderRepo = new ProductionOrderRepository(Context);
        var qualityRepo = new QualityRepository(Context);

        EmailMock = new Mock<IEmailSender>();
        EmailMock.SetupGet(e => e.IsSmtpConfigured).Returns(false);
        EmailMock
            .Setup(e => e.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        CodigoGenerador = new CodigoGeneradorService(solicitudRepo);
        AlertService = new AlertService(
            alertRepo,
            userRepo,
            materialRepo,
            materialRequestRepo,
            orderRepo,
            qualityRepo,
            EmailMock.Object,
            uow);

        SolicitudService = new SolicitudMaterialService(
            solicitudRepo,
            fichaRepo,
            orderRepo,
            materialRepo,
            CodigoGenerador,
            AlertService,
            uow);

        ApprovalService = new SolicitudMaterialApprovalService(
            solicitudRepo,
            materialRepo,
            new StockMovementRepository(Context),
            CodigoGenerador,
            AlertService,
            uow);
    }

    /// <summary>
    /// Recarga entidades trackeadas / lee stock actual desde BD temp.
    /// </summary>
    public async Task<decimal> GetMaterialStockAsync(int materialId, CancellationToken cancellationToken = default)
    {
        // Evita valores stale del change tracker
        await Context.Entry(
                await Context.Materials.FirstAsync(m => m.Id == materialId, cancellationToken))
            .ReloadAsync(cancellationToken);
        return (await Context.Materials.AsNoTracking()
            .FirstAsync(m => m.Id == materialId, cancellationToken)).Stock;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await Context.DisposeAsync();

        try
        {
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
            foreach (var suffix in new[] { "-shm", "-wal" })
            {
                var side = _dbPath + suffix;
                if (File.Exists(side)) File.Delete(side);
            }
        }
        catch
        {
            // best-effort cleanup (mismo criterio que HealthCheckTests)
        }

        GC.SuppressFinalize(this);
    }
}
