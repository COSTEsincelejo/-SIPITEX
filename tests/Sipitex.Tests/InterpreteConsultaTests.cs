using Sipitex.Application.Search;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class InterpreteConsultaTests
{
    public static IEnumerable<object[]> Consultas()
    {
        yield return ["OP-001", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(1, c.NumeroOrden);
            Assert.Contains("orden de producción 1", c.Frase, StringComparison.Ordinal);
        })];
        yield return ["op001", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(1, c.NumeroOrden);
        })];
        yield return ["orden 1", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(1, c.NumeroOrden);
        })];
        yield return ["ORDEN 12", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(12, c.NumeroOrden);
        })];
        yield return ["planta 1", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(1, c.NumeroPlanta);
            Assert.Contains("planta de inventario 1", c.Frase, StringComparison.Ordinal);
        })];
        yield return ["PLANTA DE INVENTARIO 2", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(2, c.NumeroPlanta);
            Assert.Contains("planta de inventario", c.Frase, StringComparison.Ordinal);
        })];
        yield return ["bodega 1", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(1, c.NumeroPlanta);
            SinBodega(c);
        })];
        yield return ["Bódega 2", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(2, c.NumeroPlanta);
            SinBodega(c);
        })];
        yield return ["bdega 1", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(1, c.NumeroPlanta);
            SinBodega(c);
        })];
        yield return ["stock bajo", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(StockNivel.Bajo, c.NivelStock);
            Assert.Contains("stock bajo", c.Frase, StringComparison.Ordinal);
        })];
        yield return ["STOCK CRÍTICO", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(StockNivel.Critico, c.NivelStock);
            Assert.False(c.Agotados);
            Assert.Contains("stock crítico", c.Frase, StringComparison.Ordinal);
        })];
        yield return ["sin mínimo", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(StockNivel.SinMinimo, c.NivelStock);
            Assert.Contains("sin mínimo", c.Frase, StringComparison.Ordinal);
        })];
        yield return ["agotados", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(StockNivel.Critico, c.NivelStock);
            Assert.True(c.Agotados);
            Assert.Contains("agotados", c.Frase, StringComparison.Ordinal);
        })];
        yield return ["crítico planta 1", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(StockNivel.Critico, c.NivelStock);
            Assert.Equal(1, c.NumeroPlanta);
            Assert.Equal("Entendí: materiales con stock crítico en la planta de inventario 1", c.Frase);
        })];
        yield return ["stock bajo planta de inventario 2", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(StockNivel.Bajo, c.NivelStock);
            Assert.Equal(2, c.NumeroPlanta);
            Assert.Contains("planta de inventario 2", c.Frase, StringComparison.Ordinal);
        })];
        yield return ["critco", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(StockNivel.Critico, c.NivelStock);
        })];
        yield return ["tela jersey", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal("tela jersey", c.TextoRestante);
            Assert.Contains("Entendí: tela jersey", c.Frase, StringComparison.Ordinal);
        })];
        yield return ["Camisa", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal("camisa", c.TextoRestante);
        })];
        yield return ["camisa planta 1", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal("camisa", c.TextoRestante);
            Assert.Equal(1, c.NumeroPlanta);
            Assert.Contains("planta de inventario 1", c.Frase, StringComparison.Ordinal);
        })];
        yield return ["crítico tela", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(StockNivel.Critico, c.NivelStock);
            Assert.Equal("tela", c.TextoRestante);
        })];
        yield return ["solicitudes pendientes", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(FiltroSolicitudBusqueda.Pendientes, c.FiltroSolicitud);
            Assert.Contains("pendientes", c.Frase, StringComparison.Ordinal);
        })];
        yield return ["Solicitudes Aprobadas", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(FiltroSolicitudBusqueda.Aprobadas, c.FiltroSolicitud);
        })];
        yield return ["solicitudes pendintes", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(FiltroSolicitudBusqueda.Pendientes, c.FiltroSolicitud);
        })];
        yield return ["SOL-0007", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(7, c.NumeroSolicitud);
            Assert.Contains("solicitud 7", c.Frase, StringComparison.Ordinal);
        })];
        yield return ["sol7", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(7, c.NumeroSolicitud);
        })];
        yield return ["instructor laura", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.True(c.QuierePersonas);
            Assert.Equal("laura", c.TextoPersona);
            Assert.Equal("Entendí: personas", c.Frase);
        })];
        yield return ["ana@correo.com", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.True(c.QuierePersonas);
            Assert.Equal("ana@correo.com", c.TextoPersona);
        })];
        yield return ["movimientos", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(NavegacionBusqueda.Movimientos, c.Navegacion);
            Assert.Contains("movimientos de stock", c.Frase, StringComparison.Ordinal);
        })];
        yield return ["movientos", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(NavegacionBusqueda.Movimientos, c.Navegacion);
        })];
        yield return ["reingreso", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(NavegacionBusqueda.Reingreso, c.Navegacion);
        })];
        yield return ["reingeso", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(NavegacionBusqueda.Reingreso, c.Navegacion);
        })];
        yield return ["grupos de confección", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal(NavegacionBusqueda.Grupos, c.Navegacion);
            Assert.Contains("grupos de confección", c.Frase, StringComparison.Ordinal);
        })];
        yield return ["ficha técnica pantalón", (Action<ConsultaInterpretada>)(c =>
        {
            Reconocida(c);
            Assert.Equal("pantalon", c.TextoFichaTecnica);
            Assert.Contains("ficha técnica", c.Frase, StringComparison.Ordinal);
        })];
        yield return ["xqzpt", (Action<ConsultaInterpretada>)(c =>
        {
            Assert.True(c.ConsultaValida);
            Assert.False(c.Reconocida);
            Assert.Equal("", c.Frase);
        })];
        yield return ["??", (Action<ConsultaInterpretada>)(c =>
        {
            Assert.False(c.ConsultaValida);
            Assert.False(c.Reconocida);
        })];
        yield return ["a", (Action<ConsultaInterpretada>)(c =>
        {
            Assert.False(c.ConsultaValida);
        })];
    }

    [Theory]
    [MemberData(nameof(Consultas))]
    public void Interpreta(string consulta, Action<ConsultaInterpretada> comprobar)
    {
        var resultado = new InterpreteConsulta().Interpretar(consulta);
        comprobar(resultado);
        if (resultado.Reconocida)
            Assert.StartsWith("Entendí:", resultado.Frase, StringComparison.Ordinal);
    }

    [Fact]
    public void CubreAlMenos25Consultas()
    {
        Assert.True(Consultas().Count() >= 25);
    }

    private static void Reconocida(ConsultaInterpretada consulta)
    {
        Assert.True(consulta.ConsultaValida);
        Assert.True(consulta.Reconocida);
    }

    private static void SinBodega(ConsultaInterpretada consulta) =>
        Assert.DoesNotContain("bodega", consulta.Frase, StringComparison.OrdinalIgnoreCase);
}
