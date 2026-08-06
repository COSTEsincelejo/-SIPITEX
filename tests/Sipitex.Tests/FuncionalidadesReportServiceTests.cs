using Sipitex.Application.DTOs;
using Sipitex.Application.Services;
using Sipitex.Infrastructure.Reporting;

namespace Sipitex.Tests;

public class FuncionalidadesReportServiceTests
{
    [Fact]
    public void GetCatalog_ConCatalogoPorDefecto_IncluyeModulosPrincipales()
    {
        var sut = new FuncionalidadesReportService();

        var catalog = sut.GetCatalog();

        Assert.NotEmpty(catalog);
        Assert.Contains(catalog, i => i.Modulo.Contains("Inventario", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(catalog, i => i.Modulo.Contains("Administración", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(catalog, i => i.Funcionalidad.Contains("Descargar reporte", StringComparison.OrdinalIgnoreCase));
        Assert.All(catalog, i =>
        {
            Assert.False(string.IsNullOrWhiteSpace(i.Modulo));
            Assert.False(string.IsNullOrWhiteSpace(i.Funcionalidad));
            Assert.False(string.IsNullOrWhiteSpace(i.Descripcion));
            Assert.False(string.IsNullOrWhiteSpace(i.Rol));
        });
    }

    [Fact]
    public void GenerateDocx_ConCatalogoPorDefecto_DevuelveDocxValido()
    {
        var sut = new FuncionalidadesReportService();
        var stamp = new DateTime(2026, 8, 6, 12, 0, 0);

        var file = sut.GenerateDocx(stamp);

        Assert.NotNull(file.Content);
        Assert.True(file.Content.Length > 0);
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", file.ContentType);
        Assert.Equal("SIPITEX_Funcionalidades_20260806_1200.docx", file.FileName);
        // Los .docx son ZIP: firma PK
        Assert.Equal((byte)'P', file.Content[0]);
        Assert.Equal((byte)'K', file.Content[1]);
    }

    [Fact]
    public void GenerateDocx_ConCatalogoVacio_NoFallaYDevuelveDocx()
    {
        var sut = new FuncionalidadesReportService(Array.Empty<FuncionalidadCatalogItem>());

        var file = sut.GenerateDocx();

        Assert.Empty(sut.GetCatalog());
        Assert.NotNull(file.Content);
        Assert.True(file.Content.Length > 0);
        Assert.EndsWith(".docx", file.FileName);
        Assert.Equal((byte)'P', file.Content[0]);
        Assert.Equal((byte)'K', file.Content[1]);
    }

    [Fact]
    public void FuncionalidadesCatalog_Default_CoincideConServicio()
    {
        var sut = new FuncionalidadesReportService();

        Assert.Equal(FuncionalidadesCatalog.Default.Count, sut.GetCatalog().Count);
        Assert.Equal(
            FuncionalidadesCatalog.Default.Select(i => i.Funcionalidad),
            sut.GetCatalog().Select(i => i.Funcionalidad));
    }
}
