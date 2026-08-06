using Moq;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;

namespace Sipitex.Tests;

public class CodigoGeneradorServiceTests
{
    private readonly Mock<ISolicitudMaterialRepository> _repository = new();

    private CodigoGeneradorService CreateSut() => new(_repository.Object);

    [Fact]
    public async Task GenerarCodigoSolicitudMaterial_SinPrevios_EmpiezaEn0001()
    {
        _repository
            .Setup(r => r.GetLastCodigoSolicitudAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var codigo = await CreateSut().GenerarCodigoSolicitudMaterialAsync();

        Assert.Equal("SOL-0001", codigo);
    }

    [Fact]
    public async Task GenerarCodigoEntregaMaterial_SinPrevios_EmpiezaEn0001()
    {
        _repository
            .Setup(r => r.GetLastCodigoEntregaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var codigo = await CreateSut().GenerarCodigoEntregaMaterialAsync();

        Assert.Equal("ENT-0001", codigo);
    }

    [Fact]
    public async Task GenerarCodigosConsecutivos_NoDuplicaSolicitudNiEntrega()
    {
        // Simula secuencia persistida: cada generación avanza el "último" código
        string? lastSol = null;
        string? lastEnt = null;

        _repository
            .Setup(r => r.GetLastCodigoSolicitudAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(lastSol));
        _repository
            .Setup(r => r.GetLastCodigoEntregaAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(lastEnt));

        var sut = CreateSut();
        var solicitudes = new List<string>();
        var entregas = new List<string>();

        for (var i = 0; i < 5; i++)
        {
            var sol = await sut.GenerarCodigoSolicitudMaterialAsync();
            solicitudes.Add(sol);
            lastSol = sol;

            var ent = await sut.GenerarCodigoEntregaMaterialAsync();
            entregas.Add(ent);
            lastEnt = ent;
        }

        Assert.Equal(
            new[] { "SOL-0001", "SOL-0002", "SOL-0003", "SOL-0004", "SOL-0005" },
            solicitudes);
        Assert.Equal(
            new[] { "ENT-0001", "ENT-0002", "ENT-0003", "ENT-0004", "ENT-0005" },
            entregas);
        Assert.Equal(solicitudes.Distinct().Count(), solicitudes.Count);
        Assert.Equal(entregas.Distinct().Count(), entregas.Count);
    }

    [Theory]
    [InlineData("SOL-", null, "SOL-0001")]
    [InlineData("SOL-", "SOL-0009", "SOL-0010")]
    [InlineData("SOL-", "SOL-0099", "SOL-0100")]
    [InlineData("ENT-", "ENT-0001", "ENT-0002")]
    public void SiguienteCodigo_AvanzaConsecutivo(string prefijo, string? ultimo, string esperado)
    {
        Assert.Equal(esperado, CodigoGeneradorService.SiguienteCodigo(prefijo, ultimo));
    }
}
