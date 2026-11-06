using Microsoft.EntityFrameworkCore;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Data;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Tests;

public class CmtcBomCatalogSeedDuplicateNameTests
{
    [Theory]
    [InlineData("Tela jersey doble punto", "Tela jersey doble punto")]
    [InlineData("Tela jersey doble punto", "TELA JERSEY DOBLE PUNTO")]
    public async Task InitializeAsync_NombresRepetidos_NoLanza_YCompletaElCatalogoSinDuplicar(
        string nombreJerseyA,
        string nombreJerseyB)
    {
        var databaseName = PostgresTestSupport.CreateDatabase();
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseNpgsql(PostgresTestSupport.BuildConnectionString(databaseName))
            .Options;

        try
        {
            await using var db = new SipitexDbContext(options);
            await db.Database.MigrateAsync();

            db.Materials.AddRange(
                Nuevo("jersey-1", nombreJerseyA, plantaId: 1, stock: 12),
                Nuevo("jersey-2", nombreJerseyB, plantaId: 2, stock: 7),
                // El de menor Id queda en mayúsculas a propósito: el BOM debe enlazarlo a él.
                Nuevo("ori-alta", "TELA ORIÓN 100% POLIÉSTER", plantaId: 2, stock: 4),
                Nuevo("ori-baja", "tela orión 100% poliéster", plantaId: 1, stock: 9),
                Nuevo("hilo-1", "hilo pespuntes", plantaId: 1, stock: 3),
                Nuevo("hilo-2", "HILO PESPUNTES", plantaId: 2, stock: 5));
            await db.SaveChangesAsync();

            var oriAltaId = await db.Materials.Where(m => m.Code == "ori-alta").Select(m => m.Id).SingleAsync();
            var oriBajaId = await db.Materials.Where(m => m.Code == "ori-baja").Select(m => m.Id).SingleAsync();
            Assert.True(oriAltaId < oriBajaId);

            await DbInitializer.InitializeAsync(db, seedDemoUsers: false);

            var materials = await db.Materials.AsNoTracking().ToListAsync();
            Assert.Equal(2, CountName(materials, "Tela jersey doble punto"));
            Assert.Equal(2, CountName(materials, "Tela Orión 100% poliéster"));
            Assert.Equal(2, CountName(materials, "Hilo pespuntes"));
            Assert.Equal(1, CountName(materials, "Malla 100% poliéster"));
            Assert.Equal(1, CountName(materials, "Lona 90% algodón / 10% poliéster"));
            Assert.DoesNotContain(materials, m => m.Code == "mat1");

            var malla = materials.Single(m =>
                string.Equals(m.Name, "Malla 100% poliéster", StringComparison.OrdinalIgnoreCase));
            Assert.StartsWith("cmtc-pendiente-", malla.Code, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0m, malla.Stock);

            Assert.Equal(12m, materials.Single(m => m.Code == "jersey-1").Stock);
            Assert.Equal(7m, materials.Single(m => m.Code == "jersey-2").Stock);
            Assert.Equal(nombreJerseyA, materials.Single(m => m.Code == "jersey-1").Name);
            Assert.Equal(nombreJerseyB, materials.Single(m => m.Code == "jersey-2").Name);

            var chaleco = await db.BomProducts.SingleAsync(p => p.Referencia == "C-03-004-334");
            var lineaOrion = await db.BomItems.SingleAsync(i =>
                i.BomProductId == chaleco.Id && i.QuantityPerUnit == 1.65m);
            Assert.Equal(oriAltaId, lineaOrion.MaterialId);

            var materiales = materials.Count;
            var fichas = await db.BomProducts.CountAsync();
            var lineas = await db.BomItems.CountAsync();

            await DbInitializer.InitializeAsync(db, seedDemoUsers: false);

            Assert.Equal(materiales, await db.Materials.CountAsync());
            Assert.Equal(fichas, await db.BomProducts.CountAsync());
            Assert.Equal(lineas, await db.BomItems.CountAsync());
        }
        finally
        {
            PostgresTestSupport.DropDatabase(databaseName);
        }
    }

    private static int CountName(IEnumerable<Material> materials, string name) =>
        materials.Count(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));

    private static Material Nuevo(string code, string name, int plantaId, decimal stock) => new()
    {
        Code = code,
        Name = name,
        Unit = MaterialUnit.Metros,
        Stock = stock,
        MinStock = 0,
        Status = MaterialStatus.Bueno,
        LastEntryDate = new DateOnly(2026, 11, 6),
        PlantaInventarioId = plantaId
    };
}
