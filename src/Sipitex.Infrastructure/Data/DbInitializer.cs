using Microsoft.EntityFrameworkCore; // Consultas async y MigrateAsync
using Sipitex.Application.Helpers; // PasswordHasher para los usuarios de prueba
using Sipitex.Domain.Entities; // Entidades que inserto en el seed
using Sipitex.Domain.Enums; // MaterialUnit, OrderStatus, UserRoles...
using Sipitex.Infrastructure.Persistence; // SipitexDbContext

namespace Sipitex.Infrastructure.Data;

// Datos iniciales y migraciones al arrancar la app
public static class DbInitializer
{
    // Lo llama Program.cs al iniciar — deja la BD lista con datos de demo
    public static async Task InitializeAsync(SipitexDbContext context)
    {
        // Primero reviso si hay una BD vieja sin historial de migraciones
        await MigrationBaseline.EnsureBaselineAsync(context);
        // BD que ya tenían columnas vía EnsureColumnAsync (antes de migraciones EF):
        // evita "duplicate column name" al aplicar AddFichaTurno.
        await EnsureAddFichaTurnoCompatibleAsync(context);
        // Aplico las migraciones pendientes de EF Core
        await context.Database.MigrateAsync();

        // Solo meto datos de demo si la tabla está vacía
        if (!await context.Materials.AnyAsync())
        {
            var today = DateOnly.FromDateTime(DateTime.Today); // Fecha de hoy para LastEntryDate
            // Cuatro materiales de ejemplo para probar inventario y BOM
            var mat1 = new Material { Code = "mat1", Name = "Tela Jersey", Unit = MaterialUnit.Metros, Stock = 280, MinStock = 80, Status = MaterialStatus.Bueno, LastEntryDate = today };
            var mat2 = new Material { Code = "mat2", Name = "Hilo Poliéster", Unit = MaterialUnit.Metros, Stock = 3200, MinStock = 500, Status = MaterialStatus.Bueno, LastEntryDate = today };
            var mat3 = new Material { Code = "mat3", Name = "Cremallera invisible", Unit = MaterialUnit.Unidades, Stock = 95, MinStock = 40, Status = MaterialStatus.Bueno, LastEntryDate = today };
            var mat4 = new Material { Code = "mat4", Name = "Forro Satín", Unit = MaterialUnit.Metros, Stock = 120, MinStock = 50, Status = MaterialStatus.Regular, LastEntryDate = today };

            context.Materials.AddRange(mat1, mat2, mat3, mat4); // Inserto los 4 de una vez
            await context.SaveChangesAsync(); // Necesito los Id para el BOM y las órdenes

            // Cabeceras de ficha técnica (demo) — habilitadas para órdenes
            var bomCamisa = new BomProduct
            {
                ProductName = "Camisa",
                IsReference = false,
                HabilitadoParaOrdenes = true,
                Notes = null
            };
            var bomPantalon = new BomProduct
            {
                ProductName = "Pantalón",
                IsReference = false,
                HabilitadoParaOrdenes = true,
                Notes = null
            };
            context.BomProducts.AddRange(bomCamisa, bomPantalon);
            await context.SaveChangesAsync();

            // BOM de ejemplo para Camisa y Pantalón
            context.BomItems.AddRange(
                new BomItem { BomProductId = bomCamisa.Id, ProductName = "Camisa", MaterialId = mat1.Id, QuantityPerUnit = 1.6m, Unit = MaterialUnit.Metros },
                new BomItem { BomProductId = bomCamisa.Id, ProductName = "Camisa", MaterialId = mat2.Id, QuantityPerUnit = 18m, Unit = MaterialUnit.Metros },
                new BomItem { BomProductId = bomCamisa.Id, ProductName = "Camisa", MaterialId = mat3.Id, QuantityPerUnit = 1m, Unit = MaterialUnit.Unidades },
                new BomItem { BomProductId = bomPantalon.Id, ProductName = "Pantalón", MaterialId = mat1.Id, QuantityPerUnit = 2.2m, Unit = MaterialUnit.Metros },
                new BomItem { BomProductId = bomPantalon.Id, ProductName = "Pantalón", MaterialId = mat2.Id, QuantityPerUnit = 22m, Unit = MaterialUnit.Metros },
                new BomItem { BomProductId = bomPantalon.Id, ProductName = "Pantalón", MaterialId = mat4.Id, QuantityPerUnit = 0.8m, Unit = MaterialUnit.Metros });

            // Dos órdenes de producción para el demo
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
            await context.SaveChangesAsync(); // Guardo para tener los Id de las órdenes

            // Snapshot de receta al crear (órdenes demo)
            context.ProductionOrderBomSnapshots.AddRange(
                new ProductionOrderBomSnapshot { ProductionOrderId = op1.Id, MaterialId = mat1.Id, MaterialCode = mat1.Code, MaterialName = mat1.Name, QuantityPerUnit = 1.6m, Unit = MaterialUnit.Metros },
                new ProductionOrderBomSnapshot { ProductionOrderId = op1.Id, MaterialId = mat2.Id, MaterialCode = mat2.Code, MaterialName = mat2.Name, QuantityPerUnit = 18m, Unit = MaterialUnit.Metros },
                new ProductionOrderBomSnapshot { ProductionOrderId = op1.Id, MaterialId = mat3.Id, MaterialCode = mat3.Code, MaterialName = mat3.Name, QuantityPerUnit = 1m, Unit = MaterialUnit.Unidades },
                new ProductionOrderBomSnapshot { ProductionOrderId = op2.Id, MaterialId = mat1.Id, MaterialCode = mat1.Code, MaterialName = mat1.Name, QuantityPerUnit = 2.2m, Unit = MaterialUnit.Metros },
                new ProductionOrderBomSnapshot { ProductionOrderId = op2.Id, MaterialId = mat2.Id, MaterialCode = mat2.Code, MaterialName = mat2.Name, QuantityPerUnit = 22m, Unit = MaterialUnit.Metros },
                new ProductionOrderBomSnapshot { ProductionOrderId = op2.Id, MaterialId = mat4.Id, MaterialCode = mat4.Code, MaterialName = mat4.Name, QuantityPerUnit = 0.8m, Unit = MaterialUnit.Metros });
            await context.SaveChangesAsync();

            // Tres fichas de proceso ligadas a las órdenes
            context.Fichas.AddRange(
                new Ficha { FichaCode = "FICHA-T1", ProcessName = "Trazo", InstructorName = "Laura Gómez", Turno = "Mañana", ProductionOrderId = op1.Id },
                new Ficha { FichaCode = "FICHA-C2", ProcessName = "Corte", InstructorName = "Carlos Méndez", Turno = "Mañana", ProductionOrderId = op1.Id },
                new Ficha { FichaCode = "FICHA-E3", ProcessName = "Confección", InstructorName = "Ana Rojas", Turno = "Tarde", ProductionOrderId = op2.Id });

            SeedRequirements(context); // RF y RNF del proyecto académico
            await context.SaveChangesAsync(); // Persisto fichas y requisitos
        }

        // Estos siempre corren (idempotentes) por si faltan usuarios o prefs
        await SeedUsersAsync(context);
        await LinkFichasToInstructorUsersAsync(context);
        await SeedAlertPreferencesAsync(context);
        await EnsureBomProductsAndSnapshotsAsync(context);
        // Fichas técnicas CMTC + materiales faltantes (Stock/MinStock 0); no toca Camisa/Pantalón
        await CmtcBomCatalogSeed.EnsureAsync(context);
    }

    // BD antiguas: crea BomProduct por ProductName distinto y backfill de snapshots faltantes
    private static async Task EnsureBomProductsAndSnapshotsAsync(SipitexDbContext context)
    {
        var productNames = await context.BomItems
            .Select(b => b.ProductName)
            .Distinct()
            .ToListAsync();

        foreach (var name in productNames)
        {
            if (await context.BomProducts.AnyAsync(p => p.ProductName == name))
                continue;

            var product = new BomProduct
            {
                ProductName = name,
                IsReference = false,
                HabilitadoParaOrdenes = true
            };
            context.BomProducts.Add(product);
            await context.SaveChangesAsync();

            var items = await context.BomItems.Where(b => b.ProductName == name).ToListAsync();
            foreach (var item in items)
            {
                item.BomProductId = product.Id;
            }
            await context.SaveChangesAsync();
        }

        // Órdenes sin snapshot: congelar BOM vigente actual (solo una vez)
        var orderIds = await context.ProductionOrders.Select(o => o.Id).ToListAsync();
        foreach (var orderId in orderIds)
        {
            if (await context.ProductionOrderBomSnapshots.AnyAsync(s => s.ProductionOrderId == orderId))
                continue;

            var order = await context.ProductionOrders.FirstAsync(o => o.Id == orderId);
            var bom = await context.BomItems
                .Include(b => b.Material)
                .Where(b => b.ProductName == order.ProductName)
                .ToListAsync();

            foreach (var line in bom)
            {
                context.ProductionOrderBomSnapshots.Add(new ProductionOrderBomSnapshot
                {
                    ProductionOrderId = order.Id,
                    MaterialId = line.MaterialId,
                    MaterialCode = line.Material.Code,
                    MaterialName = line.Material.Name,
                    QuantityPerUnit = line.QuantityPerUnit,
                    Unit = line.Unit
                });
            }
        }

        await context.SaveChangesAsync();
    }

    // Caso raro: la BD ya tiene las columnas de AddFichaTurno (SQL manual viejo)
    // pero EF no sabe que esa migración ya corrió → la marco a mano para no duplicar ALTER TABLE
    private static async Task EnsureAddFichaTurnoCompatibleAsync(SipitexDbContext context)
    {
        const string migrationId = "20260728231835_AddFichaTurno"; // Id exacto de la migración EF

        // Si no existen estas tablas, todavía no aplica el parche
        if (!await TableExistsAsync(context, "ProductionSessions")
            || !await TableExistsAsync(context, "Fichas"))
            return;

        // Sin historial de migraciones no puedo marcar nada
        if (!await TableExistsAsync(context, "__EFMigrationsHistory"))
            return;

        // Ya está registrada → no hago nada
        if (await MigrationRowExistsAsync(context, migrationId))
            return;

        // Agrego columnas solo si no existen (evita duplicate column)
        await EnsureColumnAsync(context, "ProductionSessions", "RegisteredByUserId",
            """ALTER TABLE "ProductionSessions" ADD COLUMN "RegisteredByUserId" INTEGER NULL;""");
        await EnsureColumnAsync(context, "Fichas", "InstructorUserId",
            """ALTER TABLE "Fichas" ADD COLUMN "InstructorUserId" INTEGER NULL;""");
        await EnsureColumnAsync(context, "Fichas", "Turno",
            """ALTER TABLE "Fichas" ADD COLUMN "Turno" TEXT NOT NULL DEFAULT '';""");

        // Índices para las FK nuevas
        await context.Database.ExecuteSqlRawAsync(
            """CREATE INDEX IF NOT EXISTS "IX_ProductionSessions_RegisteredByUserId" ON "ProductionSessions" ("RegisteredByUserId");""");
        await context.Database.ExecuteSqlRawAsync(
            """CREATE INDEX IF NOT EXISTS "IX_Fichas_InstructorUserId" ON "Fichas" ("InstructorUserId");""");

        // Le digo a EF que ya aplicó esta migración
        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ({0}, {1});
            """,
            migrationId,
            MigrationBaseline.EfProductVersion);
    }

    // Ejecuta el ALTER solo si la columna no está todavía
    private static async Task EnsureColumnAsync(SipitexDbContext context, string table, string column, string alterSql)
    {
        if (await ColumnExistsAsync(context, table, column))
            return; // Ya existe, salgo
        await context.Database.ExecuteSqlRawAsync(alterSql); // Creo la columna
    }

    // Consulto sqlite_master para ver si la tabla existe
    private static async Task<bool> TableExistsAsync(SipitexDbContext context, string table)
    {
        var connection = context.Database.GetDbConnection(); // Conexión ADO.NET subyacente
        await context.Database.OpenConnectionAsync(); // La abro si estaba cerrada
        await using var command = connection.CreateCommand(); // Comando SQL crudo
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$n LIMIT 1;";
        var p = command.CreateParameter(); // Parámetro para evitar inyección
        p.ParameterName = "$n";
        p.Value = table; // Nombre de la tabla a buscar
        command.Parameters.Add(p);
        return await command.ExecuteScalarAsync() is not null and not DBNull; // Si devuelve algo, existe
    }

    // Revisa columnas de una tabla con PRAGMA table_info
    private static async Task<bool> ColumnExistsAsync(SipitexDbContext context, string table, string column)
    {
        var connection = context.Database.GetDbConnection();
        await context.Database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{table}\")"; // Lista columnas de la tabla
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) // Recorro cada columna
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true; // Encontré la que buscaba
        }
        return false; // No estaba
    }

    // Mira si ya hay una fila en __EFMigrationsHistory para ese migrationId
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

    // Usuarios de prueba para desarrollo (admin, instructor, bodega)
    private static async Task SeedUsersAsync(SipitexDbContext context)
    {
        if (await context.Users.AnyAsync()) return; // Ya hay usuarios, no duplico

        context.Users.AddRange(
            new User
            {
                Nombre = "Administrador SIPITEX",
                Email = "admin@sipitex.test",
                PasswordHash = PasswordHasher.Hash("Admin123!"), // Clave de demo hasheada
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

    // Une fichas con usuarios instructor por nombre y rellena la tabla M2M FichaInstructors
    private static async Task LinkFichasToInstructorUsersAsync(SipitexDbContext context)
    {
        var instructors = await context.Users
            .Where(u => u.Rol == UserRoles.Instructor && u.IsActive)
            .ToListAsync();
        if (instructors.Count == 0) return;

        var fichas = await context.Fichas
            .Include(f => f.Instructors)
            .ToListAsync();
        var changed = false;

        foreach (var ficha in fichas)
        {
            // Datos viejos: solo InstructorName texto → intentar FK
            if (ficha.InstructorUserId is null)
            {
                var match = instructors.FirstOrDefault(u =>
                    string.Equals(u.Nombre, ficha.InstructorName, StringComparison.OrdinalIgnoreCase)
                    || ficha.InstructorName.Contains(u.Nombre, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                {
                    ficha.InstructorUserId = match.Id;
                    if (match.FichaAsignadaId is null)
                        match.FichaAsignadaId = ficha.Id;
                    changed = true;
                }
            }

            // Asegura fila en FichaInstructors a partir del instructor principal
            if (ficha.InstructorUserId is int uid
                && !ficha.Instructors.Any(i => i.UserId == uid))
            {
                ficha.Instructors.Add(new FichaInstructor
                {
                    FichaId = ficha.Id,
                    UserId = uid,
                    AssignedAtUtc = DateTime.UtcNow
                });
                changed = true;
            }

            // Sincroniza InstructorName con los asignados
            if (ficha.Instructors.Count > 0)
            {
                var names = ficha.Instructors
                    .Select(i => instructors.FirstOrDefault(u => u.Id == i.UserId)?.Nombre)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(n => n)
                    .ToList();
                if (names.Count > 0)
                {
                    var joined = string.Join(", ", names!);
                    if (!string.Equals(ficha.InstructorName, joined, StringComparison.Ordinal))
                    {
                        ficha.InstructorName = joined;
                        changed = true;
                    }
                }
            }
        }

        if (changed)
            await context.SaveChangesAsync();
    }

    // Crea preferencias de alerta por defecto para cada usuario según su rol
    private static async Task SeedAlertPreferencesAsync(SipitexDbContext context)
    {
        var users = await context.Users.ToListAsync(); // Todos los usuarios
        foreach (var user in users)
        {
            // Qué tipos de alerta ya tiene configurados
            var existing = await context.AlertPreferences
                .Where(p => p.UserId == user.Id)
                .Select(p => p.AlertType)
                .ToListAsync();

            foreach (var item in Application.DTOs.AlertCatalog.All) // Catálogo completo de alertas
            {
                if (existing.Contains(item.Type)) continue; // Ya tiene esa, salto
                // Activo la alerta solo si el rol del usuario está en la lista del catálogo
                context.AlertPreferences.Add(new AlertPreference
                {
                    UserId = user.Id,
                    AlertType = item.Type,
                    Enabled = item.Roles.Contains(user.Rol) // Prendida si su rol aplica
                });
            }
        }

        await context.SaveChangesAsync();
    }

    // Tabla de trazabilidad RF/RNF del proyecto académico
    private static void SeedRequirements(SipitexDbContext context)
    {
        // Array con todos los requisitos funcionales y su estado de cumplimiento
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

        // Requisitos no funcionales (rendimiento, seguridad, despliegue...)
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
