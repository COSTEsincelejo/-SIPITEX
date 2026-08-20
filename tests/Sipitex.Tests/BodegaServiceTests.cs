using Moq;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class BodegaServiceTests
{
    private readonly Mock<IBodegaRepository> _bodegas = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private BodegaService CreateSut() => new(_bodegas.Object, _uow.Object);

    [Fact]
    public async Task CreateAsync_NombreValido_CreaBodega()
    {
        _bodegas.Setup(r => r.ExistsByNombreAsync("Bodega 3", It.IsAny<CancellationToken>()))
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
        _bodegas.Setup(r => r.ExistsByNombreAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateSut().CreateAsync("bodega 1");

        Assert.False(result.Success);
        Assert.Contains("Ya existe", result.Message, StringComparison.OrdinalIgnoreCase);
        _bodegas.Verify(r => r.AddAsync(It.IsAny<Bodega>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
