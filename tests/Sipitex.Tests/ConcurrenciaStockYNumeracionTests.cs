using Microsoft.EntityFrameworkCore;
using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Persistence;
using Sipitex.Infrastructure.Repositories;

namespace Sipitex.Tests;

public class ConcurrenciaStockYNumeracionTests
{
    [Fact]
    public async Task DosAjustesDelMismoStock_UnoFallaYElSaldoNoSePisa()
    {
        // Las dos lecturas ocurren antes de cualquier guardado: es la carrera que pisaba el saldo.
        var database = PostgresTestSupport.CreateDatabase();
        try
        {
            int materialId;
            await using (var db = Open(database))
            {
                await db.Database.MigrateAsync();
                var material = new Material
                {
                    Code = "mat-conc",
                    Name = "Tela de prueba",
                    Unit = MaterialUnit.Metros,
                    Stock = 10,
                    MinStock = 1,
                    PlantaInventarioId = 1
                };
                db.Materials.Add(material);
                await db.SaveChangesAsync();
                materialId = material.Id;
            }

            await using var primero = Open(database);
            await using var segundo = Open(database);
            var lecturaA = await primero.Materials.SingleAsync(m => m.Id == materialId);
            var lecturaB = await segundo.Materials.SingleAsync(m => m.Id == materialId);
            Assert.Equal(lecturaA.RowVersion, lecturaB.RowVersion);

            lecturaA.Stock -= 3;
            await new UnitOfWork(primero).SaveChangesAsync();

            lecturaB.Stock -= 3;
            var conflicto = await Assert.ThrowsAsync<ConflictoConcurrenciaStockException>(
                () => new UnitOfWork(segundo).SaveChangesAsync());
            Assert.Equal(ConflictoConcurrenciaStockException.Mensaje, conflicto.Message);

            await using var check = Open(database);
            var stock = await check.Materials.Where(m => m.Id == materialId).Select(m => m.Stock).SingleAsync();
            Assert.Equal(7m, stock);
        }
        finally
        {
            PostgresTestSupport.DropDatabase(database);
        }
    }

    [Fact]
    public async Task DosAjustesSimultaneos_UnoFallaYElSaldoNoSePisa()
    {
        var database = PostgresTestSupport.CreateDatabase();
        try
        {
            int materialId;
            await using (var db = Open(database))
            {
                await db.Database.MigrateAsync();
                var material = new Material
                {
                    Code = "mat-conc-par",
                    Name = "Hilo de prueba",
                    Unit = MaterialUnit.Unidades,
                    Stock = 10,
                    MinStock = 1,
                    PlantaInventarioId = 1
                };
                db.Materials.Add(material);
                await db.SaveChangesAsync();
                materialId = material.Id;
            }

            var listos = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var llegaron = 0;

            async Task<Exception?> Bajar()
            {
                await using var db = Open(database);
                var material = await db.Materials.SingleAsync(m => m.Id == materialId);
                if (Interlocked.Increment(ref llegaron) == 2)
                    listos.TrySetResult();
                await listos.Task.WaitAsync(TimeSpan.FromSeconds(20));
                material.Stock -= 3;
                try
                {
                    await new UnitOfWork(db).SaveChangesAsync();
                    return null;
                }
                catch (ConflictoConcurrenciaStockException ex)
                {
                    return ex;
                }
            }

            var resultados = await Task.WhenAll(Bajar(), Bajar());
            var conflicto = Assert.Single(resultados, r => r is not null);
            Assert.Equal(ConflictoConcurrenciaStockException.Mensaje, conflicto!.Message);

            await using var check = Open(database);
            var stock = await check.Materials.Where(m => m.Id == materialId).Select(m => m.Stock).SingleAsync();
            Assert.Equal(7m, stock);
        }
        finally
        {
            PostgresTestSupport.DropDatabase(database);
        }
    }

    [Fact]
    public async Task AjusteDeStock_InformaElConflictoSinDecirQueSeGuardo()
    {
        var materials = new Mock<IMaterialRepository>();
        materials.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Material { Id = 1, Name = "Tela", Stock = 10, Unit = MaterialUnit.Metros });
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictoConcurrenciaStockException());
        var sut = new InventoryService(
            materials.Object,
            new Mock<IMaterialRequestRepository>().Object,
            new Mock<IProductionOrderRepository>().Object,
            new Mock<IBomRepository>().Object,
            new Mock<IStockMovementRepository>().Object,
            uow.Object);

        var result = await sut.AdjustStockAsync(new AdjustStockDto(1, 7m), actorUserId: 3);

        Assert.False(result.Success);
        Assert.Equal(ConflictoConcurrenciaStockException.Mensaje, result.Message);
    }

    [Fact]
    public async Task DosAltasSimultaneas_RecibenNumerosDistintos()
    {
        var database = PostgresTestSupport.CreateDatabase();
        try
        {
            await using (var db = Open(database))
                await db.Database.MigrateAsync();

            async Task<string> Siguiente()
            {
                await using var db = Open(database);
                return await new ProductionOrderRepository(db).NextOrderNumberAsync();
            }

            var numeros = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Siguiente()));
            Assert.Equal(8, numeros.Distinct().Count());
            Assert.Contains("OP-101", numeros);
            Assert.Contains("OP-108", numeros);
        }
        finally
        {
            PostgresTestSupport.DropDatabase(database);
        }
    }

    [Fact]
    public async Task Numeracion_ContinuaDespuesDelMayorNumeroExistente()
    {
        var database = PostgresTestSupport.CreateDatabase();
        try
        {
            await using var db = Open(database);
            await db.Database.MigrateAsync("20261109210000_EnlazarConsumoConEntregaDeOrden");
            db.ProductionOrders.Add(new ProductionOrder
            {
                OrderNumber = "OP-104",
                ProductName = "Camisa",
                TotalQuantity = 2,
                Deadline = new DateOnly(2026, 12, 1)
            });
            await db.SaveChangesAsync();
            await db.Database.MigrateAsync();

            var siguiente = await new ProductionOrderRepository(db).NextOrderNumberAsync();
            Assert.Equal("OP-105", siguiente);
        }
        finally
        {
            PostgresTestSupport.DropDatabase(database);
        }
    }

    private static SipitexDbContext Open(string database)
    {
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseNpgsql(PostgresTestSupport.BuildConnectionString(database))
            .Options;
        return new SipitexDbContext(options);
    }
}
