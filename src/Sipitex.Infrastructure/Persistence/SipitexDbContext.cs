using Microsoft.EntityFrameworkCore;
using Sipitex.Domain.Entities;

namespace Sipitex.Infrastructure.Persistence;

// El DbContext de EF Core — acá queda todo el mapeo a SQLite
public class SipitexDbContext : DbContext
{
    public SipitexDbContext(DbContextOptions<SipitexDbContext> options) : base(options) { }

    // Cada DbSet = una tabla en la BD
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<BomItem> BomItems => Set<BomItem>();
    public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>();
    public DbSet<MaterialRequest> MaterialRequests => Set<MaterialRequest>();
    public DbSet<Ficha> Fichas => Set<Ficha>();
    public DbSet<QualityRecord> QualityRecords => Set<QualityRecord>();
    public DbSet<FunctionalRequirement> FunctionalRequirements => Set<FunctionalRequirement>();
    public DbSet<NonFunctionalRequirement> NonFunctionalRequirements => Set<NonFunctionalRequirement>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ProductionSession> ProductionSessions => Set<ProductionSession>();
    public DbSet<AlertPreference> AlertPreferences => Set<AlertPreference>();
    public DbSet<AlertDelivery> AlertDeliveries => Set<AlertDelivery>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    // Acá configuro EF Core para cada entidad (claves, longitudes, relaciones...)
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Material>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Name).HasMaxLength(120).IsRequired();
            e.Property(m => m.Code).HasMaxLength(40).IsRequired();
            // Precision 18,2 porque stock puede tener decimales (metros de tela, etc.)
            e.Property(m => m.Stock).HasPrecision(18, 2);
            e.Property(m => m.MinStock).HasPrecision(18, 2);
        });

        // BOM = lista de materiales por prenda (Bill of Materials)
        modelBuilder.Entity<BomItem>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.ProductName).HasMaxLength(80).IsRequired();
            e.Property(b => b.QuantityPerUnit).HasPrecision(18, 2);
            e.HasOne(b => b.Material).WithMany(m => m.BomItems).HasForeignKey(b => b.MaterialId);
        });

        modelBuilder.Entity<ProductionOrder>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.OrderNumber).HasMaxLength(20).IsRequired();
            e.Property(o => o.ProductName).HasMaxLength(80).IsRequired();
            // Que no se repita el número de orden
            e.HasIndex(o => o.OrderNumber).IsUnique();
        });

        modelBuilder.Entity<MaterialRequest>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Quantity).HasPrecision(18, 2);
            e.HasOne(r => r.Material).WithMany(m => m.Requests).HasForeignKey(r => r.MaterialId);
            e.HasOne(r => r.ProductionOrder).WithMany(o => o.MaterialRequests).HasForeignKey(r => r.ProductionOrderId);
        });

        modelBuilder.Entity<Ficha>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.FichaCode).HasMaxLength(30).IsRequired();
            e.Property(f => f.Turno).HasMaxLength(20).IsRequired();
            e.HasOne(f => f.ProductionOrder).WithMany(o => o.Fichas).HasForeignKey(f => f.ProductionOrderId);
            // Si borran el usuario instructor, dejo la ficha pero sin vínculo
            e.HasOne(f => f.InstructorUser)
                .WithMany()
                .HasForeignKey(f => f.InstructorUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<QualityRecord>(e =>
        {
            e.HasKey(q => q.Id);
            e.Property(q => q.MotivoReproceso).HasMaxLength(300);
            e.Property(q => q.Responsable).HasMaxLength(120);
            e.HasOne(q => q.ProductionOrder).WithMany(o => o.QualityRecords).HasForeignKey(q => q.ProductionOrderId);
        });

        modelBuilder.Entity<ProductionSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Observations).HasMaxLength(500);
            e.HasOne(s => s.Ficha).WithMany().HasForeignKey(s => s.FichaId);
            e.HasOne(s => s.ProductionOrder).WithMany().HasForeignKey(s => s.ProductionOrderId);
            e.HasOne(s => s.RegisteredByUser)
                .WithMany()
                .HasForeignKey(s => s.RegisteredByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Requisitos funcionales del proyecto (RF01, RF02...)
        modelBuilder.Entity<FunctionalRequirement>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Code).HasMaxLength(10).IsRequired();
            e.HasIndex(r => r.Code).IsUnique();
        });

        modelBuilder.Entity<NonFunctionalRequirement>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Code).HasMaxLength(10).IsRequired();
            e.HasIndex(r => r.Code).IsUnique();
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Nombre).HasMaxLength(120).IsRequired();
            e.Property(u => u.Email).HasMaxLength(160).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(u => u.Rol).HasMaxLength(40).IsRequired();
            e.Property(u => u.PermisosExtendidos).HasMaxLength(500);
            e.Property(u => u.PhotoPath).HasMaxLength(260);
            e.Property(u => u.FuncionDescripcion).HasMaxLength(800);
            e.HasIndex(u => u.Email).IsUnique();
            // Un instructor puede tener una ficha asignada como "principal"
            e.HasOne(u => u.FichaAsignada)
                .WithMany()
                .HasForeignKey(u => u.FichaAsignadaId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Preferencias de alertas por usuario (qué tipo de notificación quiere recibir)
        modelBuilder.Entity<AlertPreference>(e =>
        {
            e.HasKey(a => a.Id);
            // Un usuario no puede tener dos prefs del mismo tipo
            e.HasIndex(a => new { a.UserId, a.AlertType }).IsUnique();
            e.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AlertDelivery>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Subject).HasMaxLength(200).IsRequired();
            e.Property(a => a.Body).HasMaxLength(4000).IsRequired();
            e.Property(a => a.Channel).HasMaxLength(40).IsRequired();
            e.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Tokens para recuperar contraseña (guardamos el hash, no el token en claro)
        modelBuilder.Entity<PasswordResetToken>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
            e.HasIndex(t => t.TokenHash);
            e.HasIndex(t => new { t.UserId, t.CreatedAtUtc });
            e.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
