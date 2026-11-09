using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Sipitex.Infrastructure.Data;

namespace Sipitex.Tests;

[Collection(WebAppCollection.Name)]
public sealed class LoggingHttpTests : IDisposable
{
    private readonly LoggingWebAppFactory _factory = new();

    [Fact]
    public async Task PeticionNormal_EscribeJsonConRequestId_YLaExcepcionMuestraElCodigo()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var login = await client.GetAsync("/Account/Login");
        login.EnsureSuccessStatusCode();

        var health = await client.GetAsync("/healthz");
        health.EnsureSuccessStatusCode();

        var css = await client.GetAsync("/css/sipitex.css");
        css.EnsureSuccessStatusCode();

        var error = await client.GetAsync("/pruebas/error");
        Assert.Equal(HttpStatusCode.InternalServerError, error.StatusCode);
        var html = await error.Content.ReadAsStringAsync();
        var codigo = Regex.Match(html, @"ERR-[0-9A-F]{6}");
        Assert.True(codigo.Success, html);
        Assert.Contains("Si el problema persiste, informe el código", html, StringComparison.Ordinal);
        Assert.Contains("<strong>" + codigo.Value + "</strong>", html, StringComparison.Ordinal);

        var lineas = File.ReadAllLines(_factory.LogPath).Where(l => l.StartsWith('{')).ToList();
        var peticion = lineas
            .Select(l => JsonDocument.Parse(l))
            .First(doc =>
                doc.RootElement.TryGetProperty("RequestMethod", out _)
                && doc.RootElement.TryGetProperty("RequestPath", out var path)
                && path.GetString() == "/Account/Login");
        Assert.False(string.IsNullOrWhiteSpace(peticion.RootElement.GetProperty("RequestId").GetString()));
        Assert.Equal("GET", peticion.RootElement.GetProperty("RequestMethod").GetString());
        Assert.Equal(200, peticion.RootElement.GetProperty("StatusCode").GetInt32());

        Assert.DoesNotContain(lineas, l => l.Contains("\"RequestPath\":\"/healthz\"", StringComparison.Ordinal));
        Assert.DoesNotContain(lineas, l => l.Contains("\"RequestPath\":\"/health\"", StringComparison.Ordinal));
        Assert.DoesNotContain(lineas, l => l.Contains("/css/sipitex.css", StringComparison.Ordinal));

        var log = string.Join('\n', lineas);
        Assert.Contains(codigo.Value, log, StringComparison.Ordinal);
        Assert.Contains("fallo-controlado-prueba", log, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException", log, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginHttp_NoEscribeLaContrasena()
    {
        const string password = "ClaveProhibida-Log-7K2M";
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var loginPage = await client.GetAsync("/Account/Login");
        loginPage.EnsureSuccessStatusCode();
        var html = await loginPage.Content.ReadAsStringAsync();
        var token = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""", RegexOptions.IgnoreCase);
        Assert.True(token.Success);

        var post = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = DbInitializer.DemoAdminEmail,
            ["Password"] = password,
            ["__RequestVerificationToken"] = token.Groups[1].Value
        }));

        Assert.Equal(HttpStatusCode.OK, post.StatusCode);
        var log = await File.ReadAllTextAsync(_factory.LogPath);
        Assert.DoesNotContain(password, log, StringComparison.Ordinal);
        Assert.Contains("Inicio de sesión fallido", log, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        _factory.Dispose();
        if (File.Exists(_factory.LogPath))
            File.Delete(_factory.LogPath);
    }
}

public sealed class LoggingWebAppFactory : SipitexWebAppFactory
{
    public string LogPath { get; } = Path.Combine(Path.GetTempPath(), $"sipitex-log-{Guid.NewGuid():N}.jsonl");

    protected override string EnvironmentName => "Production";

    protected override IEnumerable<KeyValuePair<string, string?>> ExtraSettings =>
    [
        new("Seed:DemoUsers", "true"),
        new("Pruebas:LogArchivo", LogPath)
    ];
}
