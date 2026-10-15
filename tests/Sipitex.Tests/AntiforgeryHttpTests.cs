using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Sipitex.Tests;

[Collection(WebAppCollection.Name)]
public class AntiforgeryHttpTests
{
    private readonly SipitexWebAppFactory _factory;

    public AntiforgeryHttpTests(SipitexWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_SinToken_Devuelve400_YConToken_Entra()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var sinToken = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "admin@sipitex.test",
            ["Password"] = "Admin123!"
        }));
        Assert.Equal(HttpStatusCode.BadRequest, sinToken.StatusCode);

        var pagina = await client.GetAsync("/Account/Login");
        pagina.EnsureSuccessStatusCode();
        var token = ExtraerToken(await pagina.Content.ReadAsStringAsync());
        var conToken = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "admin@sipitex.test",
            ["Password"] = "Admin123!",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, conToken.StatusCode);
        Assert.DoesNotContain("/Account/Login", conToken.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CrearUsuario_YEliminar_ExigenToken()
    {
        var client = await EntrarComoAdminAsync();
        var crear = await client.GetAsync("/Account/CreateUser");
        crear.EnsureSuccessStatusCode();
        var html = await crear.Content.ReadAsStringAsync();
        var token = ExtraerToken(html);
        var correo = "temporal-" + Guid.NewGuid().ToString("N")[..8] + "@sipitex.test";

        var sinToken = await client.PostAsync("/Account/CreateUser", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Nombre"] = "Temporal",
            ["Email"] = correo,
            ["Password"] = "Clave123!",
            ["Rol"] = "Instructor"
        }));
        Assert.Equal(HttpStatusCode.BadRequest, sinToken.StatusCode);

        var creado = await client.PostAsync("/Account/CreateUser", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Nombre"] = "Temporal",
            ["Email"] = correo,
            ["Password"] = "Clave123!",
            ["Rol"] = "Instructor",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, creado.StatusCode);

        var usuarios = await client.GetAsync("/Account/Users");
        usuarios.EnsureSuccessStatusCode();
        var lista = await usuarios.Content.ReadAsStringAsync();
        var id = ExtraerIdJuntoAlCorreo(lista, correo);
        var tokenBorrar = ExtraerToken(lista);

        var borrarSinToken = await client.PostAsync("/Account/DeleteUser", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id"] = id
        }));
        Assert.Equal(HttpStatusCode.BadRequest, borrarSinToken.StatusCode);

        var borrado = await client.PostAsync("/Account/DeleteUser", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id"] = id,
            ["__RequestVerificationToken"] = tokenBorrar
        }));
        Assert.Equal(HttpStatusCode.Redirect, borrado.StatusCode);
    }

    private async Task<HttpClient> EntrarComoAdminAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var login = await client.GetAsync("/Account/Login");
        login.EnsureSuccessStatusCode();
        var token = ExtraerToken(await login.Content.ReadAsStringAsync());
        var post = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "admin@sipitex.test",
            ["Password"] = "Admin123!",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);

        var aceptar = await client.GetAsync("/Privacidad/Aceptar");
        if (aceptar.StatusCode == HttpStatusCode.OK)
        {
            var tokenPolitica = ExtraerToken(await aceptar.Content.ReadAsStringAsync());
            var aceptada = await client.PostAsync("/Privacidad/Aceptar", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["AceptaPolitica"] = "true",
                ["__RequestVerificationToken"] = tokenPolitica
            }));
            Assert.Equal(HttpStatusCode.Redirect, aceptada.StatusCode);
        }

        return client;
    }

    private static string ExtraerToken(string html)
    {
        var match = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""", RegexOptions.IgnoreCase);
        if (!match.Success)
            match = Regex.Match(html, @"value=""([^""]+)""[^>]*name=""__RequestVerificationToken""", RegexOptions.IgnoreCase);
        Assert.True(match.Success, "No se encontró el token antiforgery.");
        return match.Groups[1].Value;
    }

    private static string ExtraerIdJuntoAlCorreo(string html, string correo)
    {
        var match = Regex.Match(
            html,
            Regex.Escape(correo) + @"[\s\S]{0,1200}?name=""id""\s+value=""(\d+)""",
            RegexOptions.IgnoreCase);
        Assert.True(match.Success, "No se encontró el id del usuario creado.");
        return match.Groups[1].Value;
    }
}
