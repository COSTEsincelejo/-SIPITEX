using Sipitex.Web;

namespace Sipitex.Tests;

[Collection(WebAppCollection.Name)]
public class VerificarActaHttpTests
{
    private readonly SipitexWebAppFactory _factory;

    public VerificarActaHttpTests(SipitexWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_SinSesion_NoRevelaSiElNumeroExiste()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/Verificar/Acta?numero=ACT-9999&codigo=aaaaaaaaaaaa");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("El código no coincide", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Documento auténtico", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ACT-9999", html, StringComparison.Ordinal);
    }
}
