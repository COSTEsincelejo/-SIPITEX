using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Controllers;

namespace Sipitex.Tests;

public class BodegaServiceTests
{
    private readonly Mock<IBodegaRepository> _bodegas = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private BodegaService CreateSut() => new(_bodegas.Object, _uow.Object);

    [Fact]
    public async Task CreateAsync_NombreValido_CreaBodega()
    {
        _bodegas.Setup(r => r.ExistsByNombreAsync("Bodega 3", It.IsAny<CancellationToken>(), It.IsAny<int?>()))
            .ReturnsAsync(false);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        Bodega? saved = null;
        _bodegas
            .Setup(r => r.AddAsync(It.IsAny<Bodega>(), It.IsAny<CancellationToken>()))
            .Callback<Bodega, CancellationToken>((b, _) => saved = b)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().CreateAsync("  Bodega 3  ");

        Assert.True(result.Success, result.Message);
        Assert.NotNull(saved);
        Assert.Equal("Bodega 3", saved!.Nombre);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NombreVacio_Falla()
    {
        var result = await CreateSut().CreateAsync("   ");

        Assert.False(result.Success);
        Assert.Contains("obligatorio", result.Message, StringComparison.OrdinalIgnoreCase);
        _bodegas.Verify(r => r.AddAsync(It.IsAny<Bodega>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NombreDuplicado_Falla()
    {
        _bodegas.Setup(r => r.ExistsByNombreAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int?>()))
            .ReturnsAsync(true);

        var result = await CreateSut().CreateAsync("bodega 1");

        Assert.False(result.Success);
        Assert.Contains("Ya existe", result.Message, StringComparison.OrdinalIgnoreCase);
        _bodegas.Verify(r => r.AddAsync(It.IsAny<Bodega>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_NombreValido_RenombraSinTocarId()
    {
        var bodega = new Bodega { Id = 2, Nombre = "Bodega 2" };
        _bodegas.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(bodega);
        _bodegas.Setup(r => r.ExistsByNombreAsync("Anexo norte", It.IsAny<CancellationToken>(), 2))
            .ReturnsAsync(false);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().UpdateAsync(2, "  Anexo norte  ");

        Assert.True(result.Success, result.Message);
        Assert.Equal(2, bodega.Id);
        Assert.Equal("Anexo norte", bodega.Nombre);
        _bodegas.Verify(r => r.Update(bodega), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NombreDuplicado_Falla()
    {
        var bodega = new Bodega { Id = 2, Nombre = "Bodega 2" };
        _bodegas.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(bodega);
        _bodegas.Setup(r => r.ExistsByNombreAsync("Bodega 1", It.IsAny<CancellationToken>(), 2))
            .ReturnsAsync(true);

        var result = await CreateSut().UpdateAsync(2, "Bodega 1");

        Assert.False(result.Success);
        Assert.Contains("Ya existe", result.Message, StringComparison.OrdinalIgnoreCase);
        _bodegas.Verify(r => r.Update(It.IsAny<Bodega>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Inexistente_Falla()
    {
        _bodegas.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Bodega?)null);

        var result = await CreateSut().UpdateAsync(99, "Nueva");

        Assert.False(result.Success);
        Assert.Contains("no encontrada", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAsync_SinDependencias_Elimina()
    {
        var bodega = new Bodega { Id = 3, Nombre = "Bodega 3" };
        _bodegas.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(bodega);
        _bodegas.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _bodegas.Setup(r => r.CountDependenciasAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BodegaDependencias(0, 0, 0));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().DeleteAsync(3);

        Assert.True(result.Success, result.Message);
        _bodegas.Verify(r => r.Remove(bodega), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ConMateriales_FallaSinBorrar()
    {
        var bodega = new Bodega { Id = 2, Nombre = "Bodega 2" };
        _bodegas.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(bodega);
        _bodegas.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _bodegas.Setup(r => r.CountDependenciasAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BodegaDependencias(4, 0, 0));

        var result = await CreateSut().DeleteAsync(2);

        Assert.False(result.Success);
        Assert.Contains("4 material", result.Message, StringComparison.OrdinalIgnoreCase);
        _bodegas.Verify(r => r.Remove(It.IsAny<Bodega>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ConSolicitudesYBodegueros_FallaConMensajeClaro()
    {
        var bodega = new Bodega { Id = 2, Nombre = "Bodega 2" };
        _bodegas.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(bodega);
        _bodegas.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _bodegas.Setup(r => r.CountDependenciasAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BodegaDependencias(0, 2, 1));

        var result = await CreateSut().DeleteAsync(2);

        Assert.False(result.Success);
        Assert.Contains("2 solicitud", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1 bodeguero", result.Message, StringComparison.OrdinalIgnoreCase);
        _bodegas.Verify(r => r.Remove(It.IsAny<Bodega>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_UltimaBodega_Falla()
    {
        var bodega = new Bodega { Id = 2, Nombre = "Bodega 2" };
        _bodegas.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(bodega);
        _bodegas.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().DeleteAsync(2);

        Assert.False(result.Success);
        Assert.Contains("última bodega", result.Message, StringComparison.OrdinalIgnoreCase);
        _bodegas.Verify(r => r.CountDependenciasAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _bodegas.Verify(r => r.Remove(It.IsAny<Bodega>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Bodega1PorDefecto_Falla()
    {
        var bodega = new Bodega { Id = 1, Nombre = "Bodega 1" };
        _bodegas.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(bodega);
        _bodegas.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var result = await CreateSut().DeleteAsync(1);

        Assert.False(result.Success);
        Assert.Contains("por defecto", result.Message, StringComparison.OrdinalIgnoreCase);
        _bodegas.Verify(r => r.Remove(It.IsAny<Bodega>()), Times.Never);
    }

    [Fact]
    public void BodegasController_SoloAdministrador_NoBodegueroNiInstructor()
    {
        var classAttr = typeof(BodegasController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAttr);
        Assert.Equal(UserRoles.Administrador, classAttr!.Roles);
        Assert.DoesNotContain(UserRoles.Bodeguero, classAttr.Roles!, StringComparison.Ordinal);
        Assert.DoesNotContain(UserRoles.Instructor, classAttr.Roles!, StringComparison.Ordinal);

        foreach (var method in typeof(BodegasController)
                     .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                     .Where(m => m.Name is nameof(BodegasController.Edit) or nameof(BodegasController.Delete)))
        {
            var methodAttr = method.GetCustomAttribute<AuthorizeAttribute>();
            if (methodAttr?.Roles is string roles)
            {
                Assert.DoesNotContain(UserRoles.Bodeguero, roles, StringComparison.Ordinal);
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
                Assert.DoesNotContain(UserRoles.Bodeguero, attr.Roles!, StringComparison.Ordinal);
            }
        }
    }
}
