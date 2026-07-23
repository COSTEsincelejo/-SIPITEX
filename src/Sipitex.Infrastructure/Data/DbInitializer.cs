using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Helpers;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(SipitexDbContext context)
    {
        await context.Database.EnsureCreatedAsync();
        await EnsureSchemaAsync(context);

        if (!await context.Materials.AnyAsync())
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var mat1 = new Material { Code = "mat1", Name = "Tela Jersey", Unit = MaterialUnit.Metros, Stock = 280, MinStock = 80, Status = MaterialStatus.Bueno, LastEntryDate = today };
            var mat2 = new Material { Code = "mat2", Name = "Hilo Poliéster", Unit = MaterialUnit.Metros, Stock = 3200, MinStock = 500, Status = MaterialStatus.Bueno, LastEntryDate = today };
            var mat3 = new Material { Code = "mat3", Name = "Cremallera invisible", Unit = MaterialUnit.Unidades, Stock = 95, MinStock = 40, Status = MaterialStatus.Bueno, LastEntryDate = today };
            var mat4 = new Material { Code = "mat4", Name = "Forro Satín", Unit = MaterialUnit.Metros, Stock = 120, MinStock = 50, Status = MaterialStatus.Regular, LastEntryDate = today };

            context.Materials.AddRange(mat1, mat2, mat3, mat4);
            await context.SaveChangesAsync();

            context.BomItems.AddRange(
                new BomItem { ProductName = "Camisa", MaterialId = mat1.Id, QuantityPerUnit = 1.6m, Unit = MaterialUnit.Metros },
                new BomItem { ProductName = "Camisa", MaterialId = mat2.Id, QuantityPerUnit = 18m, Unit = MaterialUnit.Metros },
                new BomItem { ProductName = "Camisa", MaterialId = mat3.Id, QuantityPerUnit = 1m, Unit = MaterialUnit.Unidades },
                new BomItem { ProductName = "Pantalón", MaterialId = mat1.Id, QuantityPerUnit = 2.2m, Unit = MaterialUnit.Metros },
                new BomItem { ProductName = "Pantalón", MaterialId = mat2.Id, QuantityPerUnit = 22m, Unit = MaterialUnit.Metros },
                new BomItem { ProductName = "Pantalón", MaterialId = mat4.Id, QuantityPerUnit = 0.8m, Unit = MaterialUnit.Metros });

            var op1 = new ProductionOrder
            {
                OrderNumber = "OP-001",
                ProductName = "Camisa",
                TotalQuantity = 120,
                ProducedQuantity = 45,
                Status = OrderStatus.EnProceso,
                Deadline = new DateOnly(2025, 4, 15)
            };
            var op2 = new ProductionOrder
            {
                OrderNumber = "OP-002",
                ProductName = "Pantalón",
                TotalQuantity = 80,
                ProducedQuantity = 20,
                Status = OrderStatus.EnProceso,
                Deadline = new DateOnly(2025, 4, 20)
            };
            context.ProductionOrders.AddRange(op1, op2);
            await context.SaveChangesAsync();

            context.Fichas.AddRange(
                new Ficha { FichaCode = "FICHA-T1", ProcessName = "Trazo", InstructorName = "Laura Gómez", ProductionOrderId = op1.Id },
                new Ficha { FichaCode = "FICHA-C2", ProcessName = "Corte", InstructorName = "Carlos Méndez", ProductionOrderId = op1.Id },
                new Ficha { FichaCode = "FICHA-E3", ProcessName = "Confección", InstructorName = "Ana Rojas", ProductionOrderId = op2.Id });

            SeedRequirements(context);
            await context.SaveChangesAsync();
        }

        await SeedUsersAsync(context);
        await SeedAlertPreferencesAsync(context);
    }

    private static async Task EnsureSchemaAsync(SipitexDbContext context)
    {
        await EnsureUsersTableAsync(context);

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ProductionSessions" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ProductionSessions" PRIMARY KEY AUTOINCREMENT,
                "FichaId" INTEGER NOT NULL,
                "ProductionOrderId" INTEGER NOT NULL,
                "Units" INTEGER NOT NULL,
                "Observations" TEXT NOT NULL,
                "SessionDate" TEXT NOT NULL,
                CONSTRAINT "FK_ProductionSessions_Fichas_FichaId" FOREIGN KEY ("FichaId") REFERENCES "Fichas" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_ProductionSessions_ProductionOrders_ProductionOrderId" FOREIGN KEY ("ProductionOrderId") REFERENCES "ProductionOrders" ("Id") ON DELETE CASCADE
            );
            """);

        await EnsureColumnAsync(context, "Materials", "LastEntryDate", """ALTER TABLE "Materials" ADD COLUMN "LastEntryDate" TEXT NOT NULL DEFAULT '2026-01-01';""");
        await EnsureColumnAsync(context, "QualityRecords", "MotivoReproceso", """ALTER TABLE "QualityRecords" ADD COLUMN "MotivoReproceso" TEXT NULL;""");
        await EnsureColumnAsync(context, "QualityRecords", "Responsable", """ALTER TABLE "QualityRecords" ADD COLUMN "Responsable" TEXT NULL;""");

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "AlertPreferences" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_AlertPreferences" PRIMARY KEY AUTOINCREMENT,
                "UserId" INTEGER NOT NULL,
                "AlertType" INTEGER NOT NULL,
                "Enabled" INTEGER NOT NULL,
                CONSTRAINT "FK_AlertPreferences_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
            );
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_AlertPreferences_UserId_AlertType" ON "AlertPreferences" ("UserId", "AlertType");
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "AlertDeliveries" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_AlertDeliveries" PRIMARY KEY AUTOINCREMENT,
                "UserId" INTEGER NOT NULL,
                "AlertType" INTEGER NOT NULL,
                "Subject" TEXT NOT NULL,
                "Body" TEXT NOT NULL,
                "SentAt" TEXT NOT NULL,
                "Channel" TEXT NOT NULL,
                CONSTRAINT "FK_AlertDeliveries_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
            );
            """);
    }

    private static async Task EnsureColumnAsync(SipitexDbContext context, string table, string column, string alterSql)
    {
        var connection = context.Database.GetDbConnection();
        await context.Database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{table}\")";
        await using var reader = await command.ExecuteReaderAsync();
        var exists = false;
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
            {
                exists = true;
                break;
            }
        }
        await reader.DisposeAsync();
        if (!exists)
            await context.Database.ExecuteSqlRawAsync(alterSql);
    }

    private static async Task EnsureUsersTableAsync(SipitexDbContext context)
    {
        // Compatible con BD SQLite creada antes de la tabla Users.
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Users" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
                "Nombre" TEXT NOT NULL,
                "Email" TEXT NOT NULL,
                "PasswordHash" TEXT NOT NULL,
                "Rol" TEXT NOT NULL,
                "FichaAsignadaId" INTEGER NULL,
                "PermisosExtendidos" TEXT NOT NULL,
                "IsActive" INTEGER NOT NULL,
                CONSTRAINT "FK_Users_Fichas_FichaAsignadaId" FOREIGN KEY ("FichaAsignadaId") REFERENCES "Fichas" ("Id") ON DELETE SET NULL
            );
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");
            """);
    }

    private static async Task SeedUsersAsync(SipitexDbContext context)
    {
        if (await context.Users.AnyAsync()) return;

        context.Users.AddRange(
            new User
            {
                Nombre = "Administrador SIPITEX",
                Email = "admin@sipitex.test",
                PasswordHash = PasswordHasher.Hash("Admin123!"),
                Rol = UserRoles.Administrador,
                PermisosExtendidos = "GestionUsuarios, VerReportes",
                IsActive = true
            },
            new User
            {
                Nombre = "Laura Gómez",
                Email = "instructor@sipitex.test",
                PasswordHash = PasswordHasher.Hash("Instructor123!"),
                Rol = UserRoles.Instructor,
                PermisosExtendidos = "RegistrarProduccion",
                IsActive = true
            },
            new User
            {
                Nombre = "Pedro Bodega",
                Email = "bodega@sipitex.test",
                PasswordHash = PasswordHasher.Hash("Bodega123!"),
                Rol = UserRoles.Bodeguero,
                PermisosExtendidos = "AprobarSolicitudes",
                IsActive = true
            });

        await context.SaveChangesAsync();
    }

    private static async Task SeedAlertPreferencesAsync(SipitexDbContext context)
    {
        var users = await context.Users.ToListAsync();
        foreach (var user in users)
        {
            var existing = await context.AlertPreferences
                .Where(p => p.UserId == user.Id)
                .Select(p => p.AlertType)
                .ToListAsync();

            foreach (var item in Application.DTOs.AlertCatalog.All)
            {
                if (existing.Contains(item.Type)) continue;
                context.AlertPreferences.Add(new AlertPreference
                {
                    UserId = user.Id,
                    AlertType = item.Type,
                    Enabled = item.Roles.Contains(user.Rol)
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static void SeedRequirements(SipitexDbContext context)
    {
        var rf = new[]
        {
            ("RF01", "Crear, editar y desactivar usuarios por rol.", "Usuarios", ComplianceStatus.Cumple, "CRUD de usuarios con roles."),
            ("RF02", "Autenticación con credenciales propias por rol.", "Usuarios", ComplianceStatus.Cumple, "Login con cookies de autenticación."),
            ("RF03", "Registrar entradas con fecha, cantidad y unidad.", "Inventario", ComplianceStatus.Cumple, "Fecha de última entrada en material."),
            ("RF04", "Consultar stock disponible en tiempo real.", "Inventario", ComplianceStatus.Cumple, "Actualización reactiva."),
            ("RF05", "Bodeguero registra estado del material (Bueno/Regular/Deteriorado).", "Inventario", ComplianceStatus.Cumple, "Selector de estado en inventario."),
            ("RF06", "Instructor solicita materiales (orden, producto, cantidad).", "Salida", ComplianceStatus.Cumple, "Formulario de solicitud."),
            ("RF07", "Bodeguero aprueba / rechaza y registra entrega.", "Salida", ComplianceStatus.Cumple, "Aprobación y rechazo implementados."),
            ("RF08", "Salida trazada por orden y producto.", "Salida", ComplianceStatus.Parcial, "Se guarda orderId pero no historial visible."),
            ("RF09", "Admin crea órdenes con producto, cantidad y fecha.", "Órdenes", ComplianceStatus.Cumple, "Formulario completo."),
            ("RF10", "Estados: Pendiente, En Proceso, Finalizada, Cancelada.", "Órdenes", ComplianceStatus.Parcial, "Solo 'En Proceso' y 'Finalizada'."),
            ("RF11", "Cálculo MRP automático por prenda y talla.", "Órdenes", ComplianceStatus.Cumple, "BOM hardcoded, falta diferenciación por talla."),
            ("RF12", "Fichas asociadas a un proceso de la línea.", "Fichas", ComplianceStatus.Cumple, "Cada ficha tiene proceso."),
            ("RF13", "Un instructor puede tener múltiples fichas.", "Fichas", ComplianceStatus.Parcial, "Estructuralmente posible, sin UI asignación."),
            ("RF14", "Instructor registra sesión diaria: ficha, unidades, observaciones.", "Producción", ComplianceStatus.Cumple, "Sesiones con observaciones persistidas."),
            ("RF15", "Mostrar avance acumulado vs meta de la orden.", "Producción", ComplianceStatus.Cumple, "Tabla de órdenes muestra avance."),
            ("RF16", "Criterios diferenciales por tipo de prenda.", "Calidad", ComplianceStatus.Ausente, "Solo aprobado/reproceso genérico."),
            ("RF17", "Prendas en reproceso con trazabilidad de motivo y responsable.", "Calidad", ComplianceStatus.Cumple, "Motivo y responsable obligatorios en reproceso."),
            ("RF18", "Reportes de producción por período, instructor y orden.", "Estadísticas", ComplianceStatus.Parcial, "Por orden, falta filtro por período/instructor."),
            ("RF19", "Estadísticas de consumo de materiales por producto.", "Estadísticas", ComplianceStatus.Cumple, "Cálculo vía BOM+MRP."),
            ("RF20", "Dashboard con KPIs: unidades, calidad, eficiencia.", "Estadísticas", ComplianceStatus.Cumple, "KPIs y gráfico.")
        };

        foreach (var (code, desc, module, status, obs) in rf)
        {
            context.FunctionalRequirements.Add(new FunctionalRequirement
            {
                Code = code,
                Description = desc,
                Module = module,
                Status = status,
                Observation = obs
            });
        }

        var rnf = new[]
        {
            ("RNF01", "Carga rápida en intranet (<2s)", ComplianceStatus.Cumple, "HTML estático liviano."),
            ("RNF02", "Autenticación con sesión y expiración", ComplianceStatus.Cumple, "Cookies con expiración de 8 horas."),
            ("RNF03", "Control de acceso por rol", ComplianceStatus.Cumple, "Authorize por rol en controladores."),
            ("RNF04", "Disponible desde cualquier PC de la intranet", ComplianceStatus.Parcial, "Requiere servidor HTTP."),
            ("RNF05", "Interfaz responsiva e intuitiva", ComplianceStatus.Cumple, "Layout adaptable."),
            ("RNF06", "Código modular y documentado", ComplianceStatus.Parcial, "Arquitectura por capas."),
            ("RNF07", "Despliegue con Docker Compose", ComplianceStatus.Cumple, "Dockerfile + docker-compose.yml."),
            ("RNF08", "Integridad transaccional", ComplianceStatus.Parcial, "SQLite con EF Core.")
        };

        foreach (var (code, desc, status, obs) in rnf)
        {
            context.NonFunctionalRequirements.Add(new NonFunctionalRequirement
            {
                Code = code,
                Description = desc,
                Status = status,
                Observation = obs
            });
        }
    }
}
