using Moq;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Tests;

public class PlantaInventarioReassignmentServiceTests
{
    private readonly Mock<IPlantaInventarioRepository> _plantas = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private PlantaInventarioReassignmentService CreateSut()
    {
        _uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));
        return new(_plantas.Object, _uow.Object);
    }

    [Fact]
    public async Task ReassignAndDeleteAsync_OrigenIgualDestino_Falla()
    {
        var result = await CreateSut().ReassignAndDeleteAsync(2, 2);

        Assert.False(result.Success);
        Assert.Contains("distinta", result.Message, StringComparison.OrdinalIgnoreCase);
        _uow.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReassignAndDeleteAsync_Exitoso_DesactivaOrigen()
    {
        var origen = new PlantaInventario { Id = 2, Nombre = "Anexo", Activo = true };
        var destino = new PlantaInventario { Id = 1, Nombre = "Planta de Inventario 1", Activo = true };
        _plantas.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(origen);
        _plantas.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(destino);
        _plantas.Setup(r => r.CountActivasAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _plantas.Setup(r => r.ReassignDependenciasAsync(2, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlantaInventarioReassignmentResult(2, 3, 1, 1, 40));

        var result = await CreateSut().ReassignAndDeleteAsync(2, 1);

        Assert.True(result.Success, result.Message);
        Assert.False(origen.Activo);
        Assert.Contains("40", result.Message);
        Assert.Contains("3 movimiento", result.Message, StringComparison.OrdinalIgnoreCase);
        _plantas.Verify(r => r.Update(origen), Times.Once);
        _plantas.Verify(r => r.Remove(It.IsAny<PlantaInventario>()), Times.Never);
    }

    [Fact]
    public async Task ReassignAndDeleteAsync_UltimaActiva_Falla()
    {
        var origen = new PlantaInventario { Id = 2, Nombre = "Anexo", Activo = true };
        var destino = new PlantaInventario { Id = 1, Nombre = "Planta de Inventario 1", Activo = true };
        _plantas.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(origen);
        _plantas.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(destino);
        _plantas.Setup(r => r.CountActivasAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().ReassignAndDeleteAsync(2, 1);

        Assert.False(result.Success);
        Assert.Contains("última planta", result.Message, StringComparison.OrdinalIgnoreCase);
        _uow.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class PlantaInventarioReassignmentHistoryTests
{
    [Fact]
    public async Task ReassignDependencias_PreservaHistorialDeMovimientos()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"sipitex-reassign-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<SipitexDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;
            await using (var context = new SipitexDbContext(options))
            {
                await context.Database.MigrateAsync();
                var origen = new PlantaInventario { Nombre = "Anexo reasignar", Activo = true };
                context.PlantasInventario.Add(origen);
                await context.SaveChangesAsync();

                var user = new User
                {
                    Nombre = "Admin",
                    Email = $"reassign-{Guid.NewGuid():N}@test.local",
                    PasswordHash = "x",
                    Rol = UserRoles.Administrador,
                    IsActive = true
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                var material = new Material
                {
                    Code = "mat-reassign",
                    Name = "Tela reasignar",
                    Unit = MaterialUnit.Metros,
                    Stock = 12,
                    MinStock = 1,
                    Status = MaterialStatus.Bueno,
                    LastEntryDate = DateOnly.FromDateTime(DateTime.Today),
                    PlantaInventarioId = origen.Id
                };
                context.Materials.Add(material);
                await context.SaveChangesAsync();

                context.StockMovements.Add(new StockMovement
                {
                    MaterialId = material.Id,
                    UsuarioId = user.Id,
                    TipoMovimiento = StockMovementType.Entrada,
                    Origen = StockEntryOrigin.Compra,
                    Cantidad = 12,
                    StockResultante = 12,
                    Referencia = "seed"
                });
                await context.SaveChangesAsync();

                var repo = new Sipitex.Infrastructure.Repositories.PlantaInventarioRepository(context);
                var moved = await repo.ReassignDependenciasAsync(origen.Id, destinoId: 1);
                await context.SaveChangesAsync();

                Assert.Equal(1, moved.Materiales);
                Assert.Equal(1, moved.Movimientos);
                Assert.Equal(12, moved.StockTotal);

                var reloaded = await context.Materials.SingleAsync(m => m.Id == material.Id);
                Assert.Equal(1, reloaded.PlantaInventarioId);

                var history = await context.StockMovements.Where(m => m.MaterialId == material.Id).ToListAsync();
                Assert.Single(history);
                Assert.Equal(12, history[0].Cantidad);
                Assert.Equal("seed", history[0].Referencia);
            }
        }
        finally
        {
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                var p = dbPath + suffix;
                if (File.Exists(p)) File.Delete(p);
            }
        }
    }
}
