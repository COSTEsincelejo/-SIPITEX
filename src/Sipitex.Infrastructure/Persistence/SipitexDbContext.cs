using Microsoft.EntityFrameworkCore;
using Sipitex.Domain.Entities;

namespace Sipitex.Infrastructure.Persistence;

public class SipitexDbContext : DbContext
{
    public SipitexDbContext(DbContextOptions<SipitexDbContext> options) : base(options) { }

    public DbSet<Material> Materials => Set<Material>();
    public DbSet<BomItem> BomItems => Set<BomItem>();
    public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>();
    public DbSet<MaterialRequest> MaterialRequests => Set<MaterialRequest>();
    public DbSet<Ficha> Fichas => Set<Ficha>();
    public DbSet<QualityRecord> QualityRecords => Set<QualityRecord>();
    public DbSet<FunctionalRequirement> FunctionalRequirements => Set<FunctionalRequirement>();
    public DbSet<NonFunctionalRequirement> NonFunctionalRequirements => Set<NonFunctionalRequirement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Material>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Name).HasMaxLength(120).IsRequired();
            e.Property(m => m.Code).HasMaxLength(40).IsRequired();
            e.Property(m => m.Stock).HasPrecision(18, 2);
            e.Property(m => m.MinStock).HasPrecision(18, 2);
        });

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
            e.HasOne(f => f.ProductionOrder).WithMany(o => o.Fichas).HasForeignKey(f => f.ProductionOrderId);
        });

        modelBuilder.Entity<QualityRecord>(e =>
        {
            e.HasKey(q => q.Id);
            e.HasOne(q => q.ProductionOrder).WithMany(o => o.QualityRecords).HasForeignKey(q => q.ProductionOrderId);
        });

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
    }
}
