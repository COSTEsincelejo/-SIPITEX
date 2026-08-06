using Microsoft.EntityFrameworkCore; // EF Core para hablar con SQLite
using Sipitex.Domain.Entities; // Las entidades del dominio que mapeo a tablas

namespace Sipitex.Infrastructure.Persistence;

// El DbContext de EF Core — acá queda todo el mapeo a SQLite
public class SipitexDbContext : DbContext
{
    // El contenedor DI me pasa las opciones (connection string, etc.)
    public SipitexDbContext(DbContextOptions<SipitexDbContext> options) : base(options) { }

    // Cada DbSet = una tabla en la BD
    public DbSet<Material> Materials => Set<Material>(); // Inventario de telas, hilos, etc.
    public DbSet<BomItem> BomItems => Set<BomItem>(); // Lista de materiales por prenda (BOM)
    public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>(); // Órdenes OP-xxx
    public DbSet<MaterialRequest> MaterialRequests => Set<MaterialRequest>(); // Solicitudes de salida de bodega
    public DbSet<Ficha> Fichas => Set<Ficha>(); // Fichas de proceso (trazo, corte, confección...)
    public DbSet<FichaInstructor> FichaInstructors => Set<FichaInstructor>(); // M2M ficha ↔ instructor
    public DbSet<QualityRecord> QualityRecords => Set<QualityRecord>(); // Inspecciones de calidad
    public DbSet<FunctionalRequirement> FunctionalRequirements => Set<FunctionalRequirement>(); // RF del proyecto
    public DbSet<NonFunctionalRequirement> NonFunctionalRequirements => Set<NonFunctionalRequirement>(); // RNF del proyecto
    public DbSet<User> Users => Set<User>(); // Usuarios del sistema con roles
    public DbSet<ProductionSession> ProductionSessions => Set<ProductionSession>(); // Registro diario de producción
    public DbSet<AlertPreference> AlertPreferences => Set<AlertPreference>(); // Qué alertas quiere cada usuario
    public DbSet<AlertDelivery> AlertDeliveries => Set<AlertDelivery>(); // Historial de alertas enviadas
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>(); // Tokens para recuperar contraseña
    public DbSet<SolicitudMaterial> SolicitudesMaterial => Set<SolicitudMaterial>(); // Solicitudes multi-ítem ligadas a Ficha
    public DbSet<DetalleSolicitudMaterial> DetallesSolicitudMaterial => Set<DetalleSolicitudMaterial>(); // Ítems de SolicitudMaterial
    public DbSet<EntregaMaterial> EntregasMaterial => Set<EntregaMaterial>(); // Entrega 1:1 de una solicitud resuelta

    // Acá configuro EF Core para cada entidad (claves, longitudes, relaciones...)
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- Material ---
        modelBuilder.Entity<Material>(e =>
        {
            e.HasKey(m => m.Id); // PK autoincremental
            e.Property(m => m.Name).HasMaxLength(120).IsRequired(); // Nombre del material, obligatorio
            e.Property(m => m.Code).HasMaxLength(40).IsRequired(); // Código interno (mat1, mat2...)
            // Precision 18,2 porque stock puede tener decimales (metros de tela, etc.)
            e.Property(m => m.Stock).HasPrecision(18, 2);
            e.Property(m => m.MinStock).HasPrecision(18, 2); // Umbral para alerta de stock bajo
        });

        // BOM = lista de materiales por prenda (Bill of Materials)
        modelBuilder.Entity<BomItem>(e =>
        {
            e.HasKey(b => b.Id); // PK del ítem BOM
            e.Property(b => b.ProductName).HasMaxLength(80).IsRequired(); // Prenda: Camisa, Pantalón...
            e.Property(b => b.QuantityPerUnit).HasPrecision(18, 2); // Cuánto material gasta una unidad
            // Relación: cada BomItem apunta a un Material
            e.HasOne(b => b.Material).WithMany(m => m.BomItems).HasForeignKey(b => b.MaterialId);
        });

        // --- ProductionOrder ---
        modelBuilder.Entity<ProductionOrder>(e =>
        {
            e.HasKey(o => o.Id); // PK de la orden
            e.Property(o => o.OrderNumber).HasMaxLength(20).IsRequired(); // OP-001, OP-002...
            e.Property(o => o.ProductName).HasMaxLength(80).IsRequired(); // Qué prenda se fabrica
            // Que no se repita el número de orden
            e.HasIndex(o => o.OrderNumber).IsUnique();
        });

        // --- MaterialRequest ---
        modelBuilder.Entity<MaterialRequest>(e =>
        {
            e.HasKey(r => r.Id); // PK de la solicitud
            e.Property(r => r.Quantity).HasPrecision(18, 2); // Cantidad pedida (puede ser decimal)
            // A qué material y a qué orden pertenece la solicitud
            e.HasOne(r => r.Material).WithMany(m => m.Requests).HasForeignKey(r => r.MaterialId);
            e.HasOne(r => r.ProductionOrder).WithMany(o => o.MaterialRequests).HasForeignKey(r => r.ProductionOrderId);
        });

        // --- Ficha ---
        modelBuilder.Entity<Ficha>(e =>
        {
            e.HasKey(f => f.Id); // PK de la ficha
            e.Property(f => f.FichaCode).HasMaxLength(30).IsRequired(); // FICHA-T1, FICHA-C2...
            e.Property(f => f.Turno).HasMaxLength(20).IsRequired(); // Mañana, Tarde...
            // Cada ficha está ligada a una orden de producción
            e.HasOne(f => f.ProductionOrder).WithMany(o => o.Fichas).HasForeignKey(f => f.ProductionOrderId);
            // Si borran el usuario instructor, dejo la ficha pero sin vínculo
            e.HasOne(f => f.InstructorUser)
                .WithMany()
                .HasForeignKey(f => f.InstructorUserId)
                .OnDelete(DeleteBehavior.SetNull); // No borro la ficha, solo pongo null
        });

        // --- FichaInstructor (M2M) ---
        modelBuilder.Entity<FichaInstructor>(e =>
        {
            e.HasKey(x => new { x.FichaId, x.UserId }); // Un instructor una sola vez por ficha
            e.Property(x => x.Proceso).HasMaxLength(60); // Proceso del instructor en esa ficha
            e.HasOne(x => x.Ficha)
                .WithMany(f => f.Instructors)
                .HasForeignKey(x => x.FichaId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.UserId);
        });

        // --- QualityRecord ---
        modelBuilder.Entity<QualityRecord>(e =>
        {
            e.HasKey(q => q.Id); // PK del registro de calidad
            e.Property(q => q.MotivoReproceso).HasMaxLength(300); // Por qué va a reproceso
            e.Property(q => q.Responsable).HasMaxLength(120); // Quién responde por el reproceso
            // Inspección ligada a una orden
            e.HasOne(q => q.ProductionOrder).WithMany(o => o.QualityRecords).HasForeignKey(q => q.ProductionOrderId);
        });

        // --- ProductionSession ---
        modelBuilder.Entity<ProductionSession>(e =>
        {
            e.HasKey(s => s.Id); // PK de la sesión diaria
            e.Property(s => s.Observations).HasMaxLength(500); // Notas del instructor ese día
            e.HasOne(s => s.Ficha).WithMany().HasForeignKey(s => s.FichaId); // En qué ficha trabajó
            e.HasOne(s => s.ProductionOrder).WithMany().HasForeignKey(s => s.ProductionOrderId); // Orden asociada
            // Quién registró la sesión — si borran al usuario, queda null
            e.HasOne(s => s.RegisteredByUser)
                .WithMany()
                .HasForeignKey(s => s.RegisteredByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Requisitos funcionales del proyecto (RF01, RF02...)
        modelBuilder.Entity<FunctionalRequirement>(e =>
        {
            e.HasKey(r => r.Id); // PK del RF
            e.Property(r => r.Code).HasMaxLength(10).IsRequired(); // RF01, RF02...
            e.HasIndex(r => r.Code).IsUnique(); // No repetir códigos
        });

        // Requisitos no funcionales (RNF01, RNF02...)
        modelBuilder.Entity<NonFunctionalRequirement>(e =>
        {
            e.HasKey(r => r.Id); // PK del RNF
            e.Property(r => r.Code).HasMaxLength(10).IsRequired(); // RNF01, RNF02...
            e.HasIndex(r => r.Code).IsUnique(); // Tampoco repetir acá
        });

        // --- User ---
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id); // PK del usuario
            e.Property(u => u.Nombre).HasMaxLength(120).IsRequired(); // Nombre completo
            e.Property(u => u.Email).HasMaxLength(160).IsRequired(); // Correo de login
            e.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired(); // Hash, nunca la clave en claro
            e.Property(u => u.Rol).HasMaxLength(40).IsRequired(); // Administrador, Instructor, Bodeguero...
            e.Property(u => u.PermisosExtendidos).HasMaxLength(500); // Permisos extra si aplica
            e.Property(u => u.PhotoPath).HasMaxLength(260); // Ruta de la foto de perfil
            e.Property(u => u.FuncionDescripcion).HasMaxLength(800); // Descripción del cargo
            e.HasIndex(u => u.Email).IsUnique(); // Un email = una cuenta
            // Un instructor puede tener una ficha asignada como "principal"
            e.HasOne(u => u.FichaAsignada)
                .WithMany()
                .HasForeignKey(u => u.FichaAsignadaId)
                .OnDelete(DeleteBehavior.SetNull); // Si borran la ficha, el usuario sigue
        });

        // Preferencias de alertas por usuario (qué tipo de notificación quiere recibir)
        modelBuilder.Entity<AlertPreference>(e =>
        {
            e.HasKey(a => a.Id); // PK de la preferencia
            // Un usuario no puede tener dos prefs del mismo tipo
            e.HasIndex(a => new { a.UserId, a.AlertType }).IsUnique();
            // Si borran al usuario, se van sus preferencias también
            e.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Historial de alertas que ya se mandaron
        modelBuilder.Entity<AlertDelivery>(e =>
        {
            e.HasKey(a => a.Id); // PK del envío
            e.Property(a => a.Subject).HasMaxLength(200).IsRequired(); // Asunto del correo
            e.Property(a => a.Body).HasMaxLength(4000).IsRequired(); // Cuerpo del mensaje
            e.Property(a => a.Channel).HasMaxLength(40).IsRequired(); // Email, etc.
            // Borrar usuario = borrar su historial de alertas
            e.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        });


        // --- SolicitudMaterial (flujo Ficha multi-ítem; paralelo a MaterialRequest) ---
        modelBuilder.Entity<SolicitudMaterial>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Codigo).HasMaxLength(20).IsRequired();
            e.HasIndex(s => s.Codigo).IsUnique();
            e.Property(s => s.Observaciones).HasMaxLength(500);
            e.Property(s => s.Estado).HasConversion<string>().HasMaxLength(30);
            e.HasOne(s => s.Ficha)
                .WithMany()
                .HasForeignKey(s => s.FichaId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Solicitante)
                .WithMany()
                .HasForeignKey(s => s.SolicitanteId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.ResueltoPor)
                .WithMany()
                .HasForeignKey(s => s.ResueltoPorId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(s => s.FichaId);
            e.HasIndex(s => s.SolicitanteId);
            e.HasIndex(s => s.Estado);
        });

        // --- DetalleSolicitudMaterial ---
        modelBuilder.Entity<DetalleSolicitudMaterial>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.CantidadSolicitada).HasPrecision(18, 2);
            e.Property(d => d.CantidadAprobada).HasPrecision(18, 2);
            e.Property(d => d.EstadoItem).HasConversion<string>().HasMaxLength(30);
            e.HasOne(d => d.SolicitudMaterial)
                .WithMany(s => s.Detalles)
                .HasForeignKey(d => d.SolicitudMaterialId)
                .OnDelete(DeleteBehavior.Cascade);
            // Sin colección inversa en Material (no se modifica esa entidad)
            e.HasOne(d => d.Material)
                .WithMany()
                .HasForeignKey(d => d.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(d => d.MaterialId);
        });

        // --- EntregaMaterial (1:1 con SolicitudMaterial) ---
        modelBuilder.Entity<EntregaMaterial>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Codigo).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Codigo).IsUnique();
            e.Property(x => x.Observaciones).HasMaxLength(500);
            e.HasOne(x => x.SolicitudMaterial)
                .WithOne(s => s.Entrega)
                .HasForeignKey<EntregaMaterial>(x => x.SolicitudMaterialId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.SolicitudMaterialId).IsUnique();
            e.HasOne(x => x.Bodeguero)
                .WithMany()
                .HasForeignKey(x => x.BodegueroId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Tokens para recuperar contraseña (guardamos el hash, no el token en claro)
        modelBuilder.Entity<PasswordResetToken>(e =>
        {
            e.HasKey(t => t.Id); // PK del token
            e.Property(t => t.TokenHash).HasMaxLength(128).IsRequired(); // Hash SHA del token
            e.HasIndex(t => t.TokenHash); // Para buscar rápido al validar
            e.HasIndex(t => new { t.UserId, t.CreatedAtUtc }); // Para rate limiting por usuario
            // Si borran al usuario, sus tokens también se van
            e.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
