using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Persistence;
using Sipitex.Infrastructure.Repositories;

namespace Sipitex.Tests;

public class MaterialRepositoryPaginationTests
{
    [Fact]
    public async Task PageAsync_SegundaPagina_TraeSoloElRestoYNoElDatasetCompleto()
    {
        var path = TempDb();
        try
        {
            await SeedNumberedAsync(path, count: 30);

            await using var db = Create(path);
            var repo = new MaterialRepository(db);
            var (items, total, totalSinFiltro, page) = await repo.PageAsync(
                plantaInventarioId: 1,
                allowedPlantaIds: null,
                nombre: null,
                nivel: null,
                page: 2,
                pageSize: 25);

            Assert.Equal(30, total);
            Assert.Equal(30, totalSinFiltro);
            Assert.Equal(2, page);
            Assert.Equal(5, items.Count);
            Assert.Equal(
                ["Mat 26", "Mat 27", "Mat 28", "Mat 29", "Mat 30"],
                items.Select(m => m.Name).ToArray());
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public async Task PageAsync_FiltroPorNombre_NoMezclaOtrasFilasYConservaElTotalSinFiltro()
    {
        var path = TempDb();
        try
        {
            await SeedNumberedAsync(path, count: 29, extraName: "Hilo especial", extraCode: "mat-page-esp");

            await using var db = Create(path);
            var repo = new MaterialRepository(db);
            var (items, total, totalSinFiltro, page) = await repo.PageAsync(
                plantaInventarioId: 1,
                allowedPlantaIds: null,
                nombre: "especial",
                nivel: null,
                page: 1,
                pageSize: 25);

            Assert.Equal(1, total);
            Assert.Equal(30, totalSinFiltro);
            Assert.Equal(1, page);
            var only = Assert.Single(items);
            Assert.Equal("Hilo especial", only.Name);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public async Task PageAsync_PaginaFueraDeRango_SeAjustaALaUltima()
    {
        var path = TempDb();
        try
        {
            await SeedNumberedAsync(path, count: 30);

            await using var db = Create(path);
            var repo = new MaterialRepository(db);
            var (items, total, _, page) = await repo.PageAsync(
                plantaInventarioId: 1,
                allowedPlantaIds: null,
                nombre: null,
                nivel: null,
                page: 99,
                pageSize: 25);

            Assert.Equal(30, total);
            Assert.Equal(2, page);
            Assert.Equal(5, items.Count);
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static string TempDb() =>
        Path.Combine(Path.GetTempPath(), $"sipitex-page-{Guid.NewGuid():N}.db");

    private static SipitexDbContext Create(string path) =>
        new(
            new DbContextOptionsBuilder<SipitexDbContext>().UseSqlite($"Data Source={path}").Options,
            NullCurrentPlantaInventarioAccessor.Instance);

    private static async Task SeedNumberedAsync(string path, int count, string? extraName = null, string? extraCode = null)
    {
        await using var db = Create(path);
        await db.Database.EnsureCreatedAsync();
        var materials = Enumerable.Range(1, count)
            .Select(i => new Material
            {
                Code = $"mat-page-{i:00}",
                Name = $"Mat {i:00}",
                Unit = MaterialUnit.Unidades,
                Stock = 10,
                MinStock = 2,
                PlantaInventarioId = 1
            })
            .ToList();
        if (extraName is not null)
        {
            materials.Add(new Material
            {
                Code = extraCode ?? "mat-page-extra",
                Name = extraName,
                Unit = MaterialUnit.Unidades,
                Stock = 4,
                MinStock = 2,
                PlantaInventarioId = 1
            });
        }

        db.Materials.AddRange(materials);
        await db.SaveChangesAsync();
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // best-effort
        }
    }
}
