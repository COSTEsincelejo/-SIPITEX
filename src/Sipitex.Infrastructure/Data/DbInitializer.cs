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
        await MigrationBaseline.EnsureBaselineAsync(context);
        // BD que ya tenían columnas vía EnsureColumnAsync (antes de migraciones EF):
        // evita "duplicate column name" al aplicar AddFichaTurno.
        await EnsureAddFichaTurnoCompatibleAsync(context);
        await context.Database.MigrateAsync();

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
                new Ficha { FichaCode = "FICHA-T1", ProcessName = "Trazo", InstructorName = "Laura Gómez", Turno = "Mañana", ProductionOrderId = op1.Id },
                new Ficha { FichaCode = "FICHA-C2", ProcessName = "Corte", InstructorName = "Carlos Méndez", Turno = "Mañana", ProductionOrderId = op1.Id },
                new Ficha { FichaCode = "FICHA-E3", ProcessName = "Confección", InstructorName = "Ana Rojas", Turno = "Tarde", ProductionOrderId = op2.Id });

            SeedRequirements(context);
            await context.SaveChangesAsync();
        }

        await SeedUsersAsync(context);
        await LinkFichasToInstructorUsersAsync(context);
        await SeedAlertPreferencesAsync(context);
    }

    /// <summary>
    /// Si la BD ya tiene columnas de AddFichaTurno (p. ej. EnsureColumnAsync legacy)
    /// pero la migración no está en el historial, las asegura y marca la migración
    /// para que MigrateAsync no intente ALTER TABLE duplicado.
    /// </summary>
    private static async Task EnsureAddFichaTurnoCompatibleAsync(SipitexDbContext context)
    {
        const string migrationId = "20260728231835_AddFichaTurno";

        if (!await TableExistsAsync(context, "ProductionSessions")
            || !await TableExistsAsync(context, "Fichas"))
            return;

        if (!await TableExistsAsync(context, "__EFMigrationsHistory"))
            return;

        if (await MigrationRowExistsAsync(context, migrationId))
            return;

        await EnsureColumnAsync(context, "ProductionSessions", "RegisteredByUserId",
            """ALTER TABLE "ProductionSessions" ADD COLUMN "RegisteredByUserId" INTEGER NULL;""");
        await EnsureColumnAsync(context, "Fichas", "InstructorUserId",
            """ALTER TABLE "Fichas" ADD COLUMN "InstructorUserId" INTEGER NULL;""");
        await EnsureColumnAsync(context, "Fichas", "Turno",
            """ALTER TABLE "Fichas" ADD COLUMN "Turno" TEXT NOT NULL DEFAULT '';""");

        await context.Database.ExecuteSqlRawAsync(
            """CREATE INDEX IF NOT EXISTS "IX_ProductionSessions_RegisteredByUserId" ON "ProductionSessions" ("RegisteredByUserId");""");
        await context.Database.ExecuteSqlRawAsync(
            """CREATE INDEX IF NOT EXISTS "IX_Fichas_InstructorUserId" ON "Fichas" ("InstructorUserId");""");

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ({0}, {1});
            """,
            migrationId,
            MigrationBaseline.EfProductVersion);
    }

    private static async Task EnsureColumnAsync(SipitexDbContext context, string table, string column, string alterSql)
    {
        if (await ColumnExistsAsync(context, table, column))
            return;
        await context.Database.ExecuteSqlRawAsync(alterSql);
    }

    private static async Task<bool> TableExistsAsync(SipitexDbContext context, string table)
    {
        var connection = context.Database.GetDbConnection();
        await context.Database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$n LIMIT 1;";
        var p = command.CreateParameter();
        p.ParameterName = "$n";
        p.Value = table;
        command.Parameters.Add(p);
        return await command.ExecuteScalarAsync() is not null and not DBNull;
    }

    private static async Task<bool> ColumnExistsAsync(SipitexDbContext context, string table, string column)
    {
        var connection = context.Database.GetDbConnection();
        await context.Database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{table}\")";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static async Task<bool> MigrationRowExistsAsync(SipitexDbContext context, string migrationId)
    {
        var connection = context.Database.GetDbConnection();
        await context.Database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = $id LIMIT 1;""";
        var p = command.CreateParameter();
        p.ParameterName = "$id";
        p.Value = migrationId;
        command.Parameters.Add(p);
        return await command.ExecuteScalarAsync() is not null and not DBNull;
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
                PermisosExtendidos = string.Empty,
                IsActive = true
            },
            new User
            {
                Nombre = "Laura Gómez",
                Email = "instructor@sipitex.test",
                PasswordHash = PasswordHasher.Hash("Instructor123!"),
                Rol = UserRoles.Instructor,
                PermisosExtendidos = string.Empty,
                IsActive = true
            },
            new User
            {
                Nombre = "Pedro Bodega",
                Email = "bodega@sipitex.test",
                PasswordHash = PasswordHasher.Hash("Bodega123!"),
                Rol = UserRoles.Bodeguero,
                PermisosExtendidos = string.Empty,
                IsActive = true
            });

        await context.SaveChangesAsync();
    }

    private static async Task LinkFichasToInstructorUsersAsync(SipitexDbContext context)
    {
        var instructors = await context.Users
            .Where(u => u.Rol == UserRoles.Instructor && u.IsActive)
            .ToListAsync();
        if (instructors.Count == 0) return;

        var fichas = await context.Fichas.Where(f => f.InstructorUserId == null).ToListAsync();
        var changed = false;
        foreach (var ficha in fichas)
        {
            var match = instructors.FirstOrDefault(u =>
                string.Equals(u.Nombre, ficha.InstructorName, StringComparison.OrdinalIgnoreCase));
            if (match is null) continue;

            ficha.InstructorUserId = match.Id;
            ficha.InstructorName = match.Nombre;
            if (match.FichaAsignadaId is null)
                match.FichaAsignadaId = ficha.Id;
            changed = true;
        }

        if (changed)
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
