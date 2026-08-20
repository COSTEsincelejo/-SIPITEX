using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Persistence;
using Sipitex.Infrastructure.Search;

namespace Sipitex.Tests;

public class BusquedaServiceTests
{
    private static async Task<SipitexDbContext> CreateDbAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseSqlite(conn)
            .Options;
        var db = new SipitexDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    [Fact]
    public async Task SearchAsync_EncuentraMaterialPorNombre()
    {
        await using var db = await CreateDbAsync();
        db.Materials.Add(new Material { Name = "Tela denim", Code = "mat-denim", Unit = MaterialUnit.Metros, Stock = 10, MinStock = 2, BodegaId = 1 });
        db.Materials.Add(new Material { Name = "Hilo blanco", Code = "mat-hilo", Unit = MaterialUnit.Unidades, Stock = 5, MinStock = 1, BodegaId = 1 });
        await db.SaveChangesAsync();

        var sut = new BusquedaService(db);
        var result = await sut.SearchAsync("denim");

        Assert.Contains(result, r => r.Categoria == "Materiales" && r.Texto.Contains("denim", StringComparison.OrdinalIgnoreCase));
        Assert.All(result.Where(r => r.Categoria == "Materiales"), r => Assert.Equal("/Inventario", r.Url));
    }

    [Fact]
    public async Task SearchAsync_QueryVacia_DevuelveVacio()
    {
        await using var db = await CreateDbAsync();
        var sut = new BusquedaService(db);

        var result = await sut.SearchAsync("   ");

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAsync_LimitaACincoPorCategoria()
    {
        await using var db = await CreateDbAsync();
        for (var i = 1; i <= 8; i++)
        {
            db.ProductionOrders.Add(new ProductionOrder
            {
                OrderNumber = $"OP-10{i}",
                ProductName = $"Camisa {i}",
                TotalQuantity = 10,
                ProducedQuantity = 0,
                Status = OrderStatus.EnProceso,
                Deadline = DateOnly.FromDateTime(DateTime.Today.AddDays(10))
            });
        }
        await db.SaveChangesAsync();

        var sut = new BusquedaService(db);
        var result = await sut.SearchAsync("Camisa");

        Assert.Equal(5, result.Count(r => r.Categoria == "Órdenes"));
    }
}
