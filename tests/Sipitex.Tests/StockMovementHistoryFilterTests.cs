using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Persistence;
using Sipitex.Infrastructure.Repositories;

namespace Sipitex.Tests;

/// <summary>
/// Gap #15: el historial filtra por rango de fechas y por material.
/// </summary>
public class StockMovementHistoryFilterTests
{
    private static async Task<(SipitexDbContext Db, string Path)> CreateDbAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sipitex-stock-hist-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;
        var db = new SipitexDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return (db, path);
    }

    private static async Task CleanupAsync(SipitexDbContext db, string path)
    {
        await db.DisposeAsync();
        try
        {
            if (File.Exists(path)) File.Delete(path);
            foreach (var suffix in new[] { "-shm", "-wal" })
            {
                var side = path + suffix;
                if (File.Exists(side)) File.Delete(side);
            }
        }
        catch
        {
            // best-effort
        }
    }

    [Fact]
    public async Task GetHistoryAsync_FiltersByDateRangeAndMaterial()
    {
        var (db, path) = await CreateDbAsync();
        try
        {
            var user = new User
            {
                Nombre = "Bodeguero",
                Email = "bodega@test.local",
                PasswordHash = "x",
                Rol = UserRoles.Bodeguero
            };
            var matA = new Material
            {
                Code = "A1",
                Name = "Tela A",
                Unit = MaterialUnit.Metros,
                Stock = 100,
                MinStock = 10,
                Status = MaterialStatus.Bueno,
                LastEntryDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };
            var matB = new Material
            {
                Code = "B1",
                Name = "Hilo B",
                Unit = MaterialUnit.Metros,
                Stock = 50,
                MinStock = 5,
                Status = MaterialStatus.Bueno,
                LastEntryDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };
            db.Users.Add(user);
            db.Materials.AddRange(matA, matB);
            await db.SaveChangesAsync();

            db.StockMovements.AddRange(
                new StockMovement
                {
                    MaterialId = matA.Id,
                    UsuarioId = user.Id,
                    FechaUtc = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
                    TipoMovimiento = StockMovementType.Entrada,
                    Cantidad = 10,
                    StockResultante = 10,
                    Referencia = "seed-early-A"
                },
                new StockMovement
                {
                    MaterialId = matA.Id,
                    UsuarioId = user.Id,
                    FechaUtc = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc),
                    TipoMovimiento = StockMovementType.Ajuste,
                    Cantidad = 5,
                    StockResultante = 15,
                    Referencia = "seed-mid-A"
                },
                new StockMovement
                {
                    MaterialId = matB.Id,
                    UsuarioId = user.Id,
                    FechaUtc = new DateTime(2026, 8, 10, 14, 0, 0, DateTimeKind.Utc),
                    TipoMovimiento = StockMovementType.Salida,
                    Cantidad = 2,
                    StockResultante = 48,
                    Referencia = "seed-mid-B"
                },
                new StockMovement
                {
                    MaterialId = matA.Id,
                    UsuarioId = user.Id,
                    FechaUtc = new DateTime(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc),
                    TipoMovimiento = StockMovementType.Salida,
                    Cantidad = 1,
                    StockResultante = 14,
                    Referencia = "seed-late-A"
                });
            await db.SaveChangesAsync();

            var service = new StockMovementService(new StockMovementRepository(db));

            var byMaterial = await service.GetHistoryAsync(null, null, matA.Id);
            Assert.Equal(3, byMaterial.Count);
            Assert.All(byMaterial, m => Assert.Equal(matA.Id, m.MaterialId));

            var byDate = await service.GetHistoryAsync(
                new DateOnly(2026, 8, 9),
                new DateOnly(2026, 8, 11),
                null);
            Assert.Equal(2, byDate.Count);
            Assert.Contains(byDate, m => m.Referencia == "seed-mid-A");
            Assert.Contains(byDate, m => m.Referencia == "seed-mid-B");

            var byBoth = await service.GetHistoryAsync(
                new DateOnly(2026, 8, 9),
                new DateOnly(2026, 8, 11),
                matA.Id);
            Assert.Single(byBoth);
            Assert.Equal("seed-mid-A", byBoth[0].Referencia);
            Assert.Equal(user.Nombre, byBoth[0].UsuarioNombre);
            Assert.Equal("Tela A", byBoth[0].MaterialName);
        }
        finally
        {
            await CleanupAsync(db, path);
        }
    }
}
