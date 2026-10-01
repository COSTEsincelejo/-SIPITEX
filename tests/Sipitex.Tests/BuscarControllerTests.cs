using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Search;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Persistence;
using Sipitex.Infrastructure.Search;
using Sipitex.Web.Controllers;

namespace Sipitex.Tests;

public class BuscarControllerTests
{
    [Fact]
    public async Task Sugerencias_MenosDeDosCaracteres_DevuelveVacio()
    {
        var buscador = new Mock<IBuscadorSugerenciasService>();
        var controller = Controller(buscador.Object, NullCurrentPlantaInventarioAccessor.Instance, UserRoles.Administrador);

        var dto = await Json(controller, "a");

        Assert.Equal("", dto.Entendi);
        Assert.Empty(dto.Grupos);
        Assert.Empty(dto.Ejemplos);
        buscador.Verify(
            b => b.SugerirAsync(It.IsAny<string>(), It.IsAny<AlcanceBusqueda>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(UserRoles.Administrador, true, true, false)]
    [InlineData(UserRoles.EncargadoDeBodega, true, false, true)]
    [InlineData(UserRoles.Instructor, false, false, false)]
    public async Task Sugerencias_ArmaElAlcanceDelRol(
        string rol,
        bool inventario,
        bool usuarios,
        bool restringePlantas)
    {
        AlcanceBusqueda? capturado = null;
        var buscador = new Mock<IBuscadorSugerenciasService>();
        buscador
            .Setup(b => b.SugerirAsync(It.IsAny<string>(), It.IsAny<AlcanceBusqueda>(), It.IsAny<CancellationToken>()))
            .Callback<string, AlcanceBusqueda, CancellationToken>((_, alcance, _) => capturado = alcance)
            .ReturnsAsync(SugerenciasBusquedaDto.Vacias());
        ICurrentPlantaInventarioAccessor accessor = rol == UserRoles.EncargadoDeBodega
            ? new FixedCurrentPlantaInventarioAccessor([1])
            : NullCurrentPlantaInventarioAccessor.Instance;
        var controller = Controller(buscador.Object, accessor, rol);

        await Json(controller, "camisa");

        Assert.NotNull(capturado);
        Assert.Equal(inventario, capturado!.PuedeVerInventario);
        Assert.Equal(usuarios, capturado.PuedeVerUsuarios);
        Assert.Equal(restringePlantas, capturado.RestringePlantas);
        if (rol == UserRoles.Instructor)
        {
            Assert.True(capturado.EsInstructor);
            Assert.False(capturado.PuedeVerMovimientos);
            Assert.False(capturado.PuedeVerReingreso);
        }

        if (rol == UserRoles.EncargadoDeBodega)
            Assert.Equal([1], capturado.PlantaInventarioIds);
    }

    [Fact]
    public async Task Sugerencias_EncargadoNoVeOtraPlanta_EInstructorNoVeUsuarios()
    {
        var accessorEncargado = new FixedCurrentPlantaInventarioAccessor([1]);
        await using var dbEncargado = await CreateDbAsync(accessorEncargado);
        await using var dbInstructor = await CreateDbAsync();
        await SeedAsync(dbEncargado);
        await SeedAsync(dbInstructor);

        var encargado = Controller(
            new BuscadorSugerenciasService(dbEncargado),
            accessorEncargado,
            UserRoles.EncargadoDeBodega);
        var deEncargado = await Json(encargado, "boton");
        Assert.DoesNotContain(deEncargado.Grupos.SelectMany(g => g.Items), i =>
            i.Texto.Contains("Botón", StringComparison.Ordinal) || i.Url.Contains("/Detalle/2", StringComparison.Ordinal));

        var instructor = Controller(
            new BuscadorSugerenciasService(dbInstructor),
            NullCurrentPlantaInventarioAccessor.Instance,
            UserRoles.Instructor);
        var deInstructor = await Json(instructor, "laura@sipitex.test");
        Assert.Empty(deInstructor.Grupos);
        Assert.DoesNotContain("@", deInstructor.Entendi, StringComparison.Ordinal);
    }

    private static BuscarController Controller(
        IBuscadorSugerenciasService buscador,
        ICurrentPlantaInventarioAccessor accessor,
        string rol) =>
        new(buscador, accessor)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = Principal(rol) }
            }
        };

    private static ClaimsPrincipal Principal(string rol)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "7"),
            new Claim(ClaimTypes.Role, rol),
            new Claim(ClaimTypes.Name, "Usuario")
        ], "Test");
        return new ClaimsPrincipal(identity);
    }

    private static async Task<SugerenciasBusquedaDto> Json(BuscarController controller, string q)
    {
        var result = await controller.Sugerencias(q, CancellationToken.None);
        return Assert.IsType<SugerenciasBusquedaDto>(Assert.IsType<JsonResult>(result).Value);
    }

    private static async Task<SipitexDbContext> CreateDbAsync(ICurrentPlantaInventarioAccessor? accessor = null)
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var options = new DbContextOptionsBuilder<SipitexDbContext>().UseSqlite(conn).Options;
        var db = accessor is null
            ? new SipitexDbContext(options)
            : new SipitexDbContext(options, accessor);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static async Task SeedAsync(SipitexDbContext db)
    {
        db.Users.Add(new User
        {
            Nombre = "Laura Gómez",
            Email = "laura@sipitex.test",
            PasswordHash = "hash",
            Rol = UserRoles.Instructor,
            IsActive = true
        });
        db.Materials.Add(new Material
        {
            Name = "Botón nácar",
            Code = "mat-boton",
            Unit = MaterialUnit.Unidades,
            Stock = 8,
            MinStock = 2,
            PlantaInventarioId = 2
        });
        await db.SaveChangesAsync();
    }
}
