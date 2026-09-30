using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Persistence;
using Sipitex.Infrastructure.Repositories;
using Sipitex.Web.Controllers;
using Sipitex.Web.Models;

namespace Sipitex.Tests;

public class InventarioDentroDePlantasTests
{
    [Fact]
    public async Task Inventario_EncargadoConUnaPlanta_RedirigeAlDetalle()
    {
        var controller = LegacyController(UserRoles.EncargadoDeBodega, new FixedCurrentPlantaInventarioAccessor([4]));

        var index = await controller.Index(CancellationToken.None);
        var redirect = Assert.IsType<RedirectResult>(index);
        Assert.True(redirect.Permanent);
        Assert.Equal("/PlantasInventario/Detalle/4", redirect.Url);
    }

    [Fact]
    public void Inventario_EncargadoConVariasPlantas_RedirigeAlCatalogo()
    {
        var controller = LegacyController(UserRoles.EncargadoDeBodega, new FixedCurrentPlantaInventarioAccessor([1, 2]));

        var result = controller.Movimientos(null, null, null, CancellationToken.None);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.True(redirect.Permanent);
        Assert.Equal("/PlantasInventario/Movimientos", redirect.Url);
    }

    [Fact]
    public async Task Index_EncargadoConUnaPlanta_EntraDirectoASuInventario()
    {
        await using var scope = await Scope.CreateAsync(UserRoles.EncargadoDeBodega, new FixedCurrentPlantaInventarioAccessor([1]));

        var result = await scope.Controller.Index(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(PlantasInventarioController.Detalle), redirect.ActionName);
        Assert.Equal(1, redirect.RouteValues!["id"]);
    }

    [Fact]
    public async Task Index_EncargadoConVariasPlantas_ListaSoloLasSuyas()
    {
        await using var scope = await Scope.CreateAsync(UserRoles.EncargadoDeBodega, new FixedCurrentPlantaInventarioAccessor([1, 2]));

        var result = await scope.Controller.Index(CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<PlantasInventarioIndexViewModel>(view.Model);
        Assert.Equal([1, 2], vm.PlantasInventario.Select(p => p.Id).OrderBy(id => id).ToArray());
    }

    [Fact]
    public async Task OperacionesEnPlanta1_NoAlteranLaPlanta2_YElEncargadoNoOperaAjena()
    {
        await using var admin = await Scope.CreateAsync(UserRoles.Administrador, NullCurrentPlantaInventarioAccessor.Instance);
        var (telaId, forroId) = await admin.SeedDosPlantasAsync();

        var alta = await admin.Controller.AddMaterial(1, new CreateMaterialForm
        {
            Name = "Cierre",
            Stock = 3,
            Unit = MaterialUnit.Unidades,
            Origen = StockEntryOrigin.Devolucion
        }, CancellationToken.None);
        Assert.IsType<RedirectToActionResult>(alta);

        var edicion = await admin.Controller.EditMaterial(1, new EditMaterialForm
        {
            MaterialId = telaId,
            Name = "Tela editada",
            Unit = MaterialUnit.Metros,
            MinStock = 6,
            CostoAdquisicion = 1
        }, CancellationToken.None);
        Assert.IsType<RedirectToActionResult>(edicion);

        var ajuste = await admin.Controller.AdjustStock(1, new AdjustStockForm
        {
            MaterialId = telaId,
            NewStock = 4
        }, CancellationToken.None);
        Assert.IsType<RedirectToActionResult>(ajuste);

        admin.Db.ChangeTracker.Clear();
        var planta1 = await admin.Db.Materials.IgnoreQueryFilters().Where(m => m.PlantaInventarioId == 1).ToListAsync();
        var planta2 = await admin.Db.Materials.IgnoreQueryFilters().Where(m => m.PlantaInventarioId == 2).ToListAsync();
        Assert.Contains(planta1, m => m.Name == "Cierre" && m.PlantaInventarioId == 1);
        var tela = Assert.Single(planta1, m => m.Id == telaId);
        Assert.Equal("Tela editada", tela.Name);
        Assert.Equal(6, tela.MinStock);
        Assert.Equal(4, tela.Stock);
        var forro = Assert.Single(planta2);
        Assert.Equal(forroId, forro.Id);
        Assert.Equal("Forro", forro.Name);
        Assert.Equal(8, forro.Stock);
        Assert.DoesNotContain(planta2, m => m.Name == "Cierre");

        var detalle1 = await Detalle(admin, 1);
        var detalle2 = await Detalle(admin, 2);
        Assert.Contains(detalle1.Materiales, m => m.Nombre == "Cierre");
        Assert.DoesNotContain(detalle1.Materiales, m => m.Nombre == "Forro");
        Assert.DoesNotContain(detalle2.Materiales, m => m.Nombre == "Cierre" || m.Nombre == "Tela editada");
        Assert.All(detalle1.Materiales, m => Assert.Equal(
            StockNivelHelper.Classify(m.StockActual, m.MinStock),
            m.NivelStock));

        await using var encargado = await Scope.CreateAsync(
            UserRoles.EncargadoDeBodega,
            new FixedCurrentPlantaInventarioAccessor([1]),
            admin.Db);
        var ajena = await encargado.Controller.AdjustStock(2, new AdjustStockForm
        {
            MaterialId = forroId,
            NewStock = 1
        }, CancellationToken.None);
        Assert.IsType<ForbidResult>(ajena);

        var cruzado = await encargado.Controller.AdjustStock(1, new AdjustStockForm
        {
            MaterialId = forroId,
            NewStock = 1
        }, CancellationToken.None);
        Assert.IsType<NotFoundResult>(cruzado);

        var propia = await encargado.Controller.AdjustStock(1, new AdjustStockForm
        {
            MaterialId = telaId,
            NewStock = 3
        }, CancellationToken.None);
        Assert.IsType<RedirectToActionResult>(propia);

        admin.Db.ChangeTracker.Clear();
        var forroFinal = await admin.Db.Materials.IgnoreQueryFilters().SingleAsync(m => m.Id == forroId);
        var telaFinal = await admin.Db.Materials.IgnoreQueryFilters().SingleAsync(m => m.Id == telaId);
        Assert.Equal(8, forroFinal.Stock);
        Assert.Equal(2, forroFinal.PlantaInventarioId);
        Assert.Equal(3, telaFinal.Stock);
        Assert.Equal(1, telaFinal.PlantaInventarioId);
    }

    private static async Task<PlantaDetalleViewModel> Detalle(Scope scope, int id)
    {
        var result = await scope.Controller.Detalle(id, null, null, CancellationToken.None);
        var view = Assert.IsType<ViewResult>(result);
        return Assert.IsType<PlantaDetalleViewModel>(view.Model);
    }

    private static InventarioController LegacyController(string role, ICurrentPlantaInventarioAccessor accessor)
    {
        var controller = new InventarioController(
            Mock.Of<IInventoryService>(),
            Mock.Of<IProductionOrderService>(),
            Mock.Of<IStockMovementService>(),
            accessor)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = Principal(1, role) }
            }
        };
        return controller;
    }

    private static ClaimsPrincipal Principal(int userId, string role)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Name, "Usuario")
        ], "Test");
        return new ClaimsPrincipal(identity);
    }

    private sealed class Scope : IAsyncDisposable
    {
        private readonly string? _path;
        private readonly bool _ownsDb;
        public SipitexDbContext Db { get; }
        public PlantasInventarioController Controller { get; }

        private Scope(string? path, bool ownsDb, SipitexDbContext db, PlantasInventarioController controller)
        {
            _path = path;
            _ownsDb = ownsDb;
            Db = db;
            Controller = controller;
        }

        public static async Task<Scope> CreateAsync(
            string role,
            ICurrentPlantaInventarioAccessor accessor,
            SipitexDbContext? existing = null)
        {
            SipitexDbContext db;
            string? path = null;
            var owns = existing is null;
            if (existing is null)
            {
                path = Path.Combine(Path.GetTempPath(), $"sipitex-inv-planta-{Guid.NewGuid():N}.db");
                var options = new DbContextOptionsBuilder<SipitexDbContext>()
                    .UseSqlite($"Data Source={path}")
                    .Options;
                db = new SipitexDbContext(options, accessor);
                await db.Database.EnsureCreatedAsync();
                db.Users.Add(new User
                {
                    Nombre = "Operador",
                    Email = $"ops-{Guid.NewGuid():N}@sipitex.test",
                    PasswordHash = "x",
                    Rol = role,
                    EmailConfirmed = true
                });
                await db.SaveChangesAsync();
            }
            else
            {
                db = existing;
            }

            var userId = await db.Users.Select(u => u.Id).FirstAsync();
            var inventory = new InventoryService(
                new MaterialRepository(db),
                Mock.Of<IMaterialRequestRepository>(),
                Mock.Of<IProductionOrderRepository>(),
                Mock.Of<IBomRepository>(),
                new StockMovementRepository(db),
                new UnitOfWork(db),
                accessor);
            var plantas = new PlantaInventarioService(new PlantaInventarioRepository(db), new UnitOfWork(db));
            var controller = new PlantasInventarioController(
                plantas,
                Mock.Of<IPlantaInventarioReassignmentService>(),
                Mock.Of<IActivityLogService>(),
                inventory,
                accessor,
                new StockMovementService(new StockMovementRepository(db)))
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = Principal(userId, role) }
                }
            };
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());
            return new Scope(path, owns, db, controller);
        }

        public async Task<(int TelaId, int ForroId)> SeedDosPlantasAsync()
        {
            var tela = new Material
            {
                Code = "mat-tela",
                Name = "Tela Jersey",
                Unit = MaterialUnit.Metros,
                Stock = 10,
                MinStock = 5,
                PlantaInventarioId = 1
            };
            var forro = new Material
            {
                Code = "mat-forro",
                Name = "Forro",
                Unit = MaterialUnit.Metros,
                Stock = 8,
                MinStock = 1,
                PlantaInventarioId = 2
            };
            Db.Materials.AddRange(tela, forro);
            await Db.SaveChangesAsync();
            return (tela.Id, forro.Id);
        }

        public async ValueTask DisposeAsync()
        {
            if (!_ownsDb)
                return;

            await Db.DisposeAsync();
            if (_path is null)
                return;
            try
            {
                if (File.Exists(_path))
                    File.Delete(_path);
            }
            catch (IOException)
            {
                // El archivo temporal puede seguir bloqueado.
            }
        }
    }
}
