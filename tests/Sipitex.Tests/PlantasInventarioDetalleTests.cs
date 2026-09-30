using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
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

public class PlantasInventarioDetalleTests
{
    [Fact]
    public async Task Detalle_IdInexistente_DevuelveNotFound()
    {
        await using var scope = await Scope.CreateAsync(UserRoles.Administrador, NullCurrentPlantaInventarioAccessor.Instance);

        var result = await scope.Controller.Detalle(999, null, null, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        scope.Activity.Verify(
            a => a.LogAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Detalle_DosPlantas_CadaUnaVeSoloSuStock_AunqueElFiltroGlobalSeaOtraPlanta()
    {
        // Accessor de la planta 2: sin IgnoreQueryFilters la planta 1 quedaría vacía.
        await using var scope = await Scope.CreateAsync(
            UserRoles.Administrador,
            new FixedCurrentPlantaInventarioAccessor([2]));
        await scope.SeedAsync();

        var planta1 = await DetalleVm(scope, 1);
        Assert.Equal("Planta de Inventario 1", planta1.Nombre);
        Assert.True(planta1.Activa);
        Assert.Equal(3, planta1.TotalItems);
        Assert.Equal(3, planta1.Materiales.Count);
        Assert.Equal(["Tela Jersey", "Botón nácar", "Hilo Poliéster"], planta1.Materiales.Select(m => m.Nombre).ToArray());
        Assert.DoesNotContain(planta1.Materiales, m => m.Nombre == "Forro");
        Assert.Equal(PlantaDetalleConsulta.EtiquetaMaterial, planta1.Materiales[0].Categoria);
        Assert.All(planta1.Materiales.Skip(1), m => Assert.Equal(PlantaDetalleConsulta.EtiquetaInsumo, m.Categoria));

        var planta2 = await DetalleVm(scope, 2);
        Assert.Equal("Planta de Inventario 2", planta2.Nombre);
        var unico = Assert.Single(planta2.Materiales);
        Assert.Equal("Forro", unico.Nombre);
        Assert.Equal(1, planta2.TotalItems);
    }

    [Fact]
    public async Task Detalle_PlantaSinStock_ListaVacia()
    {
        await using var scope = await Scope.CreateAsync(UserRoles.Administrador, NullCurrentPlantaInventarioAccessor.Instance);
        var vaciaId = await scope.SeedAsync();

        var vm = await DetalleVm(scope, vaciaId);

        Assert.Equal("Planta vacía", vm.Nombre);
        Assert.Empty(vm.Materiales);
        Assert.Equal(0, vm.TotalItems);
        Assert.Equal(0, vm.TotalBajo);
        Assert.Equal(0, vm.TotalCritico);
        Assert.Equal(0, vm.TotalSinFiltro);
    }

    [Fact]
    public async Task Detalle_BusquedaYCategoria_FiltranNombreCodigoYTipo()
    {
        await using var scope = await Scope.CreateAsync(UserRoles.Administrador, NullCurrentPlantaInventarioAccessor.Instance);
        await scope.SeedAsync();

        var porNombre = await DetalleVm(scope, 1, busqueda: "jersey");
        var tela = Assert.Single(porNombre.Materiales);
        Assert.Equal("Tela Jersey", tela.Nombre);
        Assert.Equal("mat-tela", tela.Codigo);
        Assert.Equal(3, porNombre.TotalSinFiltro);

        var porCodigo = await DetalleVm(scope, 1, busqueda: "MAT-HILO");
        var hilo = Assert.Single(porCodigo.Materiales);
        Assert.Equal("Hilo Poliéster", hilo.Nombre);

        var materiales = await DetalleVm(scope, 1, categoria: "Materiales");
        var soloMaterial = Assert.Single(materiales.Materiales);
        Assert.Equal("Tela Jersey", soloMaterial.Nombre);
        Assert.Equal(PlantaDetalleConsulta.EtiquetaMaterial, soloMaterial.Categoria);

        var insumos = await DetalleVm(scope, 1, categoria: "insumos");
        Assert.Equal(["Botón nácar", "Hilo Poliéster"], insumos.Materiales.Select(m => m.Nombre).ToArray());
        Assert.All(insumos.Materiales, m => Assert.Equal(PlantaDetalleConsulta.EtiquetaInsumo, m.Categoria));
        Assert.DoesNotContain(insumos.Materiales, m => m.Nombre == "Tela Jersey");
    }

    [Fact]
    public async Task Detalle_Nivel_CoincideConStockNivelHelper()
    {
        await using var scope = await Scope.CreateAsync(UserRoles.Administrador, NullCurrentPlantaInventarioAccessor.Instance);
        await scope.SeedAsync();

        var vm = await DetalleVm(scope, 1);
        var tela = vm.Materiales.Single(m => m.Codigo == "mat-tela");
        var hilo = vm.Materiales.Single(m => m.Codigo == "mat-hilo");
        var boton = vm.Materiales.Single(m => m.Codigo == "mat-boton");

        Assert.Equal(StockNivelHelper.Classify(10, 5), tela.NivelStock);
        Assert.Equal(StockNivel.Ok, tela.NivelStock);
        Assert.Equal(StockNivelHelper.Classify(2, 5), hilo.NivelStock);
        Assert.Equal(StockNivel.Bajo, hilo.NivelStock);
        Assert.Equal(StockNivelHelper.Classify(0, 4), boton.NivelStock);
        Assert.Equal(StockNivel.Critico, boton.NivelStock);
        Assert.Equal("metro", tela.UnidadMedida);
        Assert.Equal("unidad", boton.UnidadMedida);
        Assert.Equal(1, vm.TotalBajo);
        Assert.Equal(1, vm.TotalCritico);
        scope.Activity.Verify(
            a => a.LogAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Detalle_Instructor_Forbid()
    {
        await using var scope = await Scope.CreateAsync(UserRoles.Instructor, NullCurrentPlantaInventarioAccessor.Instance);
        await scope.SeedAsync();

        var result = await scope.Controller.Detalle(1, null, null, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Detalle_EncargadoSinAccesoALaPlanta_Forbid()
    {
        await using var scope = await Scope.CreateAsync(
            UserRoles.EncargadoDeBodega,
            new FixedCurrentPlantaInventarioAccessor([1]));
        await scope.SeedAsync();

        var ajena = await scope.Controller.Detalle(2, null, null, CancellationToken.None);
        Assert.IsType<ForbidResult>(ajena);

        var propia = await DetalleVm(scope, 1);
        Assert.Equal(3, propia.Materiales.Count);
        Assert.DoesNotContain(propia.Materiales, m => m.Nombre == "Forro");
    }

    [Fact]
    public async Task Detalle_EncargadoSinPlantasAsignadas_Forbid()
    {
        await using var scope = await Scope.CreateAsync(
            UserRoles.EncargadoDeBodega,
            new FixedCurrentPlantaInventarioAccessor([]));
        await scope.SeedAsync();

        var result = await scope.Controller.Detalle(1, null, null, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public void Detalle_Roles_AdminYEncargado_NoInstructor()
    {
        var method = typeof(PlantasInventarioController).GetMethod(nameof(PlantasInventarioController.Detalle));
        Assert.NotNull(method);
        var roles = method!.GetCustomAttributes<AuthorizeAttribute>()
            .Select(a => a.Roles)
            .FirstOrDefault(r => !string.IsNullOrEmpty(r));
        Assert.NotNull(roles);
        Assert.Contains(UserRoles.Administrador, roles, StringComparison.Ordinal);
        Assert.Contains(UserRoles.EncargadoDeBodega, roles, StringComparison.Ordinal);
        Assert.DoesNotContain(UserRoles.Instructor, roles, StringComparison.Ordinal);
        Assert.Null(method.GetCustomAttribute<HttpPostAttribute>());
    }

    private static async Task<PlantaDetalleViewModel> DetalleVm(
        Scope scope,
        int id,
        string? busqueda = null,
        string? categoria = null)
    {
        var result = await scope.Controller.Detalle(id, busqueda, categoria, CancellationToken.None);
        var view = Assert.IsType<ViewResult>(result);
        return Assert.IsType<PlantaDetalleViewModel>(view.Model);
    }

    private sealed class Scope : IAsyncDisposable
    {
        private readonly string _path;
        public SipitexDbContext Db { get; }
        public PlantasInventarioController Controller { get; }
        public Mock<IActivityLogService> Activity { get; }

        private Scope(string path, SipitexDbContext db, PlantasInventarioController controller, Mock<IActivityLogService> activity)
        {
            _path = path;
            Db = db;
            Controller = controller;
            Activity = activity;
        }

        public static async Task<Scope> CreateAsync(string role, ICurrentPlantaInventarioAccessor accessor)
        {
            var path = Path.Combine(Path.GetTempPath(), $"sipitex-detalle-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<SipitexDbContext>()
                .UseSqlite($"Data Source={path}")
                .Options;
            var db = new SipitexDbContext(options, accessor);
            await db.Database.EnsureCreatedAsync();

            var activity = new Mock<IActivityLogService>();
            var inventory = new InventoryService(
                new MaterialRepository(db),
                Mock.Of<IMaterialRequestRepository>(),
                Mock.Of<IProductionOrderRepository>(),
                Mock.Of<IBomRepository>(),
                Mock.Of<IStockMovementRepository>(),
                new UnitOfWork(db),
                accessor);
            var plantas = new PlantaInventarioService(new PlantaInventarioRepository(db), new UnitOfWork(db));
            var controller = new PlantasInventarioController(
                plantas,
                Mock.Of<IPlantaInventarioReassignmentService>(),
                activity.Object,
                inventory,
                accessor)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = Principal(role) }
                }
            };
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());
            return new Scope(path, db, controller, activity);
        }

        public async Task<int> SeedAsync()
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
            var hilo = new Material
            {
                Code = "mat-hilo",
                Name = "Hilo Poliéster",
                Unit = MaterialUnit.Metros,
                Stock = 2,
                MinStock = 5,
                PlantaInventarioId = 1
            };
            var boton = new Material
            {
                Code = "mat-boton",
                Name = "Botón nácar",
                Unit = MaterialUnit.Unidades,
                Stock = 0,
                MinStock = 4,
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
            Db.Materials.AddRange(tela, hilo, boton, forro);
            var vacia = new PlantaInventario { Nombre = "Planta vacía", Activo = true };
            Db.PlantasInventario.Add(vacia);
            await Db.SaveChangesAsync();

            var product = new BomProduct
            {
                ProductName = "Camisa detalle",
                Codigo = "PRD-DETALLE-1"
            };
            Db.BomProducts.Add(product);
            await Db.SaveChangesAsync();
            Db.BomItems.Add(new BomItem
            {
                BomProductId = product.Id,
                ProductName = product.ProductName,
                MaterialId = tela.Id,
                QuantityPerUnit = 1.2m,
                Unit = MaterialUnit.Metros
            });
            await Db.SaveChangesAsync();
            return vacia.Id;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            try
            {
                if (File.Exists(_path))
                    File.Delete(_path);
            }
            catch (IOException)
            {
                // El archivo temporal puede seguir bloqueado; no afecta el resultado.
            }
        }

        private static ClaimsPrincipal Principal(string role)
        {
            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, "Usuario")
            ], "Test");
            return new ClaimsPrincipal(identity);
        }
    }
}
