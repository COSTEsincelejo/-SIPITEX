using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Data;
using Sipitex.Infrastructure.Import;
using Sipitex.Infrastructure.Persistence;
using Sipitex.Infrastructure.Reporting;
using Sipitex.Infrastructure.Repositories;
using Sipitex.Infrastructure.Services;

namespace Sipitex.Tests.Integration;

/// <summary>
/// E2E CMTC: seed catálogo → import stock (actualiza materiales Stock=0) → orden Cofia →
/// materiales → consumo → MES hasta Terminado + PT → acta.
/// No toca el seed demo Camisa/Pantalón.
/// </summary>
public class FlujoCompletoCmtcE2ETests
{
    private const string Producto = "Cofia";
    private const int CantidadOrden = 2;

    [Fact]
    public async Task FlujoCompleto_Cofia_ConImportStockYActa()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"sipitex-cmtc-e2e-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        await using (var db = new SipitexDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            var admin = new User
            {
                Nombre = "Admin CMTC E2E",
                Email = $"admin-cmtc-e2e-{Guid.NewGuid():N}@test.local",
                PasswordHash = "x",
                Rol = UserRoles.Administrador,
                EmailConfirmed = true,
                IsActive = true
            };
            db.Users.Add(admin);
            // EnsureCreated no siembra NumeracionesDocumento (viene de migración Postgres).
            if (!await db.NumeracionesDocumento.AnyAsync())
            {
                db.NumeracionesDocumento.Add(new NumeracionDocumento
                {
                    Codigo = NumeracionDocumento.OrdenProduccion,
                    Siguiente = NumeracionDocumento.PrimerNumeroOrden
                });
            }

            await db.SaveChangesAsync();

            await CmtcBomCatalogSeed.EnsureAsync(db);

            var cofiaMaterials = await db.Materials
                .Where(m =>
                    m.Name == "Malla 100% poliéster"
                    || m.Name == "Elástico crochet 1cm"
                    || m.Name == "Hilo pespuntes"
                    || m.Name == "Hilaza amarre y filete")
                .ToListAsync();
            Assert.Equal(4, cofiaMaterials.Count);
            Assert.All(cofiaMaterials, m => Assert.Equal(0m, m.Stock));

            var uow = new UnitOfWork(db);
            var import = new InventarioImportService(db, uow);
            await using var plantilla = new MemoryStream(InventarioPlantillaExcel.Crear());
            var sim = await import.SimularAsync(plantilla);
            Assert.True(sim.ArchivoValido, sim.ErrorArchivo);
            Assert.Contains(sim.Validas, v => v.ActualizaExistente && v.Nombre == "Malla 100% poliéster");

            plantilla.Position = 0;
            var confirmado = await import.ConfirmarAsync(plantilla, admin.Id);
            Assert.True(confirmado.Persistido, confirmado.ErrorArchivo);
            Assert.True(confirmado.Importados >= 4, $"Importados={confirmado.Importados}");

            await db.Entry(cofiaMaterials[0]).ReloadAsync();
            var malla = await db.Materials.SingleAsync(m => m.Name == "Malla 100% poliéster");
            Assert.Equal(50m, malla.Stock);
            Assert.True(await db.StockMovements.AnyAsync(m =>
                m.MaterialId == malla.Id && m.TipoMovimiento == StockMovementType.Entrada));

            // Reimport idempotente: no duplica stock
            await using var plantilla2 = new MemoryStream(InventarioPlantillaExcel.Crear());
            var reimport = await import.ConfirmarAsync(plantilla2, admin.Id);
            Assert.Equal(0, reimport.Importados);
            Assert.Equal(50m, (await db.Materials.SingleAsync(m => m.Name == "Malla 100% poliéster")).Stock);

            var services = Wire(db);
            var crear = await services.Orders.CreateOrderAsync(new CreateProductionOrderDto(
                Producto,
                CantidadOrden,
                DateOnly.FromDateTime(DateTime.Today.AddDays(14)),
                ClientName: "E2E CMTC"));
            Assert.True(crear.Success, crear.Message);

            var order = await db.ProductionOrders.SingleAsync(o => o.ProductName == Producto);
            Assert.Equal(OrderStatus.Pendiente, order.Status);

            var aprobar = await services.Orders.ApproveOrderAsync(order.Id, admin.Id);
            Assert.True(aprobar.Success, aprobar.Message);

            var bom = await services.Materials.ImportFromBomAsync(order.Id);
            Assert.True(bom.Success, bom.Message);

            var lineas = await db.ProductionOrderMaterialRequirements
                .Where(l => l.ProductionOrderId == order.Id)
                .ToListAsync();
            Assert.Equal(4, lineas.Count);

            var entrega = await services.Materials.DeliverAsync(
                new DeliverOrderMaterialsDto(
                    order.Id,
                    lineas.Select(l => new DeliverOrderMaterialItemDto(l.Id, l.QuantityRequired)).ToList(),
                    "E2E entrega CMTC"),
                encargadoDeBodegaId: admin.Id);
            Assert.True(entrega.Success, entrega.Message);

            foreach (var linea in lineas)
            {
                var consumo = await services.Consumos.RegisterAsync(new RegisterConsumoMaterialDto(
                    order.Id,
                    linea.MaterialId,
                    linea.QuantityRequired,
                    admin.Id));
                Assert.True(consumo.Success, consumo.Message);
            }

            await services.Flow.EnsureStagesForOrderAsync(order.Id, admin.Nombre);
            var stages = (await db.ProductionOrderStages
                .Where(s => s.ProductionOrderId == order.Id)
                .OrderBy(s => s.SortOrder)
                .ToListAsync()).ToList();
            Assert.Equal(5, stages.Count);
            Assert.Equal("Trazo", stages[0].Name);
            Assert.Equal("Terminado", stages[^1].Name);

            // Avance MES: enviar toda la cantidad hasta Terminado (robusto sin Quality).
            for (var i = 0; i < stages.Count - 1; i++)
            {
                var from = stages[i];
                await db.Entry(from).ReloadAsync();
                var send = await services.Flow.SendToNextAsync(
                    new SendToNextStageDto(from.Id, CantidadOrden, null),
                    admin.Id,
                    admin.Nombre,
                    UserRoles.Administrador);
                Assert.True(send.Success, $"Send {from.Name}: {send.Message}");
            }

            var terminado = stages[^1];
            await db.Entry(terminado).ReloadAsync();
            Assert.True(terminado.QuantityAvailable >= CantidadOrden);

            var pt = await services.Flow.PartialInventoryInAsync(
                new PartialInventoryInDto(order.Id, terminado.Id, CantidadOrden, "E2E PT Cofia"),
                admin.Id,
                admin.Nombre,
                UserRoles.Administrador);
            Assert.True(pt.Success, pt.Message);

            await db.Entry(order).ReloadAsync();
            Assert.Equal(CantidadOrden, order.ProducedQuantity);
            var fg = await db.FinishedGoodStocks.SingleOrDefaultAsync(f => f.ProductName == Producto);
            Assert.NotNull(fg);
            Assert.Equal(CantidadOrden, fg!.Stock);

            var acta = await services.Actas.CreateAsync(new CreateActaDto(
                ActaTipo.Egreso,
                ActaOrigen.Consumo,
                "Admin CMTC E2E",
                "Administrador",
                true,
                "Bodega CMTC",
                "Encargado de bodega",
                true,
                admin.Id,
                Observaciones: "Acta E2E Cofia",
                ProductionOrderId: order.Id,
                EntregaFirmaPng: TestPng.Bytes,
                RecibeFirmaPng: TestPng.Bytes,
                PlantaOrigenId: 1));
            Assert.True(acta.Success, acta.Message);
            Assert.NotNull(acta.Value);
            Assert.StartsWith("ACT-", acta.Value!.Numero, StringComparison.Ordinal);
            Assert.Equal(ActaTipo.Egreso, acta.Value.Tipo);
            Assert.True(acta.Value.Detalles.Count >= 4);

            // Stock de insumos bajó por la entrega (consumo no vuelve a descontar).
            var mallaFinal = await db.Materials.SingleAsync(m => m.Name == "Malla 100% poliéster");
            Assert.Equal(50m - (0.14m * CantidadOrden), mallaFinal.Stock);

            Assert.DoesNotContain(await db.BomProducts.Select(p => p.ProductName).ToListAsync(),
                n => n is "Camisa" or "Pantalón");
        }

        TryDelete(dbPath);
    }

    private static Services Wire(SipitexDbContext db)
    {
        var uow = new UnitOfWork(db);
        var orders = new ProductionOrderRepository(db);
        var boms = new BomRepository(db);
        var snapshots = new ProductionOrderBomSnapshotRepository(db);
        var reqs = new OrderMaterialRequirementRepository(db);
        var flowRepo = new ProductionFlowRepository(db);
        var changeLogs = new OrderChangeLogRepository(db);
        var fichas = new FichaRepository(db);
        var materials = new MaterialRepository(db);
        var stock = new StockMovementRepository(db);
        var users = new UserRepository(db);
        var consumos = new ConsumoMaterialRepository(db);
        var prendas = new PrendaTrazableRepository(db);
        var actas = new ActaMovimientoRepository(db);
        var activity = new ActivityLogService(db, users);

        var lifecycle = new ProductLifecycleService(
            new ProductStateMachine(),
            orders,
            prendas,
            flowRepo,
            activity,
            uow);

        var flow = new ProductionFlowService(
            orders,
            flowRepo,
            reqs,
            snapshots,
            boms,
            users,
            materials,
            stock,
            uow,
            lifecycle,
            NullLogger<ProductionFlowService>.Instance,
            consumos);

        var orderService = new ProductionOrderService(
            orders,
            boms,
            snapshots,
            reqs,
            flowRepo,
            flow,
            changeLogs,
            fichas,
            uow,
            new ProductionConsumptionService(boms, materials, stock),
            NullLogger<ProductionOrderService>.Instance);

        var materialService = new OrderMaterialService(
            orders,
            reqs,
            materials,
            snapshots,
            stock,
            uow);

        var consumoService = new MaterialConsumptionService(
            consumos,
            orders,
            materials,
            users,
            reqs,
            new MaterialConsumptionCostService(),
            uow);

        var actaService = new ActaMovimientoService(
            actas,
            stock,
            consumos,
            orders,
            users,
            new ActaPdfService(),
            uow,
            NullLogger<ActaMovimientoService>.Instance,
            new PlantaInventarioRepository(db),
            activity);

        return new Services(orderService, materialService, consumoService, flow, actaService);
    }

    private sealed record Services(
        IProductionOrderService Orders,
        IOrderMaterialService Materials,
        IMaterialConsumptionService Consumos,
        IProductionFlowService Flow,
        IActaMovimientoService Actas);

    private static void TryDelete(string dbPath)
    {
        try
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
            foreach (var suffix in new[] { "-shm", "-wal" })
            {
                var side = dbPath + suffix;
                if (File.Exists(side)) File.Delete(side);
            }
        }
        catch
        {
            // best-effort
        }
    }
}
