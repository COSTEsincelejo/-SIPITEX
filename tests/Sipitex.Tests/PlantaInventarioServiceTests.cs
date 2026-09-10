using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Controllers;

namespace Sipitex.Tests;

public class PlantaInventarioServiceTests
{
    private readonly Mock<IPlantaInventarioRepository> _plantas = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private PlantaInventarioService CreateSut() => new(_plantas.Object, _uow.Object);

    [Fact]
    public async Task CreateAsync_NombreValido_CreaPlantaInventario()
    {
        _plantas.Setup(r => r.ExistsByNombreAsync("PlantaInventario 3", It.IsAny<CancellationToken>(), It.IsAny<int?>()))
            .ReturnsAsync(false);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        PlantaInventario? saved = null;
        _plantas
            .Setup(r => r.AddAsync(It.IsAny<PlantaInventario>(), It.IsAny<CancellationToken>()))
            .Callback<PlantaInventario, CancellationToken>((b, _) => saved = b)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().CreateAsync("  PlantaInventario 3  ");

        Assert.True(result.Success, result.Message);
        Assert.NotNull(saved);
        Assert.Equal("PlantaInventario 3", saved!.Nombre);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NombreVacio_Falla()
    {
        var result = await CreateSut().CreateAsync("   ");

        Assert.False(result.Success);
        Assert.Contains("obligatorio", result.Message, StringComparison.OrdinalIgnoreCase);
        _plantas.Verify(r => r.AddAsync(It.IsAny<PlantaInventario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NombreDuplicado_Falla()
    {
        _plantas.Setup(r => r.ExistsByNombreAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int?>()))
            .ReturnsAsync(true);

        var result = await CreateSut().CreateAsync("plantaInventario 1");

        Assert.False(result.Success);
        Assert.Contains("Ya existe", result.Message, StringComparison.OrdinalIgnoreCase);
        _plantas.Verify(r => r.AddAsync(It.IsAny<PlantaInventario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_NombreValido_RenombraSinTocarId()
    {
        var plantaInventario = new PlantaInventario { Id = 2, Nombre = "PlantaInventario 2" };
        _plantas.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(plantaInventario);
        _plantas.Setup(r => r.ExistsByNombreAsync("Anexo norte", It.IsAny<CancellationToken>(), 2))
            .ReturnsAsync(false);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().UpdateAsync(2, "  Anexo norte  ");

        Assert.True(result.Success, result.Message);
        Assert.Equal(2, plantaInventario.Id);
        Assert.Equal("Anexo norte", plantaInventario.Nombre);
        _plantas.Verify(r => r.Update(plantaInventario), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NombreDuplicado_Falla()
    {
        var plantaInventario = new PlantaInventario { Id = 2, Nombre = "PlantaInventario 2" };
        _plantas.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(plantaInventario);
        _plantas.Setup(r => r.ExistsByNombreAsync("PlantaInventario 1", It.IsAny<CancellationToken>(), 2))
            .ReturnsAsync(true);

        var result = await CreateSut().UpdateAsync(2, "PlantaInventario 1");

        Assert.False(result.Success);
        Assert.Contains("Ya existe", result.Message, StringComparison.OrdinalIgnoreCase);
        _plantas.Verify(r => r.Update(It.IsAny<PlantaInventario>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Inexistente_Falla()
    {
        _plantas.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((PlantaInventario?)null);

        var result = await CreateSut().UpdateAsync(99, "Nueva");

        Assert.False(result.Success);
        Assert.Contains("no encontrada", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAsync_SinDependencias_Elimina()
    {
        var plantaInventario = new PlantaInventario { Id = 3, Nombre = "PlantaInventario 3" };
        _plantas.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(plantaInventario);
        _plantas.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _plantas.Setup(r => r.CountDependenciasAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlantaInventarioDependencias(0, 0, 0));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().DeleteAsync(3);

        Assert.True(result.Success, result.Message);
        _plantas.Verify(r => r.Remove(plantaInventario), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ConMateriales_FallaSinBorrar()
    {
        var plantaInventario = new PlantaInventario { Id = 2, Nombre = "PlantaInventario 2" };
        _plantas.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(plantaInventario);
        _plantas.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _plantas.Setup(r => r.CountDependenciasAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlantaInventarioDependencias(4, 0, 0));

        var result = await CreateSut().DeleteAsync(2);

        Assert.False(result.Success);
        Assert.Contains("4 material", result.Message, StringComparison.OrdinalIgnoreCase);
        _plantas.Verify(r => r.Remove(It.IsAny<PlantaInventario>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ConSolicitudesYEncargados_FallaConMensajeClaro()
    {
        var plantaInventario = new PlantaInventario { Id = 2, Nombre = "PlantaInventario 2" };
        _plantas.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(plantaInventario);
        _plantas.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _plantas.Setup(r => r.CountDependenciasAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlantaInventarioDependencias(0, 2, 1));

        var result = await CreateSut().DeleteAsync(2);

        Assert.False(result.Success);
        Assert.Contains("2 solicitud", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1 encargado", result.Message, StringComparison.OrdinalIgnoreCase);
        _plantas.Verify(r => r.Remove(It.IsAny<PlantaInventario>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_UltimaPlantaInventario_Falla()
    {
        var plantaInventario = new PlantaInventario { Id = 2, Nombre = "PlantaInventario 2" };
        _plantas.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(plantaInventario);
        _plantas.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().DeleteAsync(2);

        Assert.False(result.Success);
        Assert.Contains("última planta de inventario", result.Message, StringComparison.OrdinalIgnoreCase);
        _plantas.Verify(r => r.CountDependenciasAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _plantas.Verify(r => r.Remove(It.IsAny<PlantaInventario>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_PlantaInventario1PorDefecto_Falla()
    {
        var plantaInventario = new PlantaInventario { Id = 1, Nombre = "PlantaInventario 1" };
        _plantas.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(plantaInventario);
        _plantas.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var result = await CreateSut().DeleteAsync(1);

        Assert.False(result.Success);
        Assert.Contains("por defecto", result.Message, StringComparison.OrdinalIgnoreCase);
        _plantas.Verify(r => r.Remove(It.IsAny<PlantaInventario>()), Times.Never);
    }

    [Fact]
    public void PlantasInventarioController_SoloAdministrador_NoEncargadoDeBodegaNiInstructor()
    {
        var classAttr = typeof(PlantasInventarioController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAttr);
        Assert.Equal(UserRoles.Administrador, classAttr!.Roles);
        Assert.DoesNotContain(UserRoles.EncargadoDeBodega, classAttr.Roles!, StringComparison.Ordinal);
        Assert.DoesNotContain(UserRoles.Instructor, classAttr.Roles!, StringComparison.Ordinal);

        foreach (var method in typeof(PlantasInventarioController)
                     .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                     .Where(m => m.Name is nameof(PlantasInventarioController.Edit) or nameof(PlantasInventarioController.Delete)))
        {
            var methodAttr = method.GetCustomAttribute<AuthorizeAttribute>();
            if (methodAttr?.Roles is string roles)
            {
                Assert.DoesNotContain(UserRoles.EncargadoDeBodega, roles, StringComparison.Ordinal);
                Assert.DoesNotContain(UserRoles.Instructor, roles, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void AccountController_CreateEditUser_SoloAdministrador()
    {
        foreach (var name in new[] { nameof(AccountController.CreateUser), nameof(AccountController.EditUser) })
        {
            var methods = typeof(AccountController)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => m.Name == name);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<AuthorizeAttribute>();
                Assert.NotNull(attr);
                Assert.Equal(UserRoles.Administrador, attr!.Roles);
                Assert.DoesNotContain(UserRoles.EncargadoDeBodega, attr.Roles!, StringComparison.Ordinal);
            }
        }
    }
}
