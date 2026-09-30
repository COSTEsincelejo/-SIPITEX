using Sipitex.Web.Hosting;

namespace Sipitex.Tests;

public class RenderListenTests
{
    [Fact]
    public void ListenUrlFor_SinPuerto_NoCambiaLaUrl()
    {
        Assert.Null(RenderListen.ListenUrlFor(null));
        Assert.Null(RenderListen.ListenUrlFor("  "));
    }

    [Fact]
    public void ListenUrlFor_PuertoDeRender_EscuchaEnTodasLasInterfaces()
    {
        Assert.Equal("http://0.0.0.0:10000", RenderListen.ListenUrlFor("10000"));
        Assert.Equal("http://0.0.0.0:8080", RenderListen.ListenUrlFor(" 8080 "));
    }

    [Fact]
    public void ListenUrlFor_PuertoInvalido_FallaConMensajeClaro()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => RenderListen.ListenUrlFor("abc"));
        Assert.Contains("PORT", ex.Message, StringComparison.Ordinal);
    }
}
