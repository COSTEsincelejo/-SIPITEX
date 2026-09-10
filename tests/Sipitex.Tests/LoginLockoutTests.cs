using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Web.Security;

namespace Sipitex.Tests;

public class MemoryLoginAttemptGuardTests
{
    [Fact]
    public void FifthFailure_LocksOut_UntilReset()
    {
        var guard = new MemoryLoginAttemptGuard(new MemoryCache(new MemoryCacheOptions()), NullLogger<MemoryLoginAttemptGuard>.Instance);

        for (var i = 0; i < MemoryLoginAttemptGuard.MaxFailedAttempts - 1; i++)
        {
            guard.RecordFailure("user@test", "127.0.0.1");
            Assert.False(guard.IsLockedOut("user@test", "127.0.0.1"));
        }

        guard.RecordFailure("user@test", "127.0.0.1");
        Assert.True(guard.IsLockedOut("user@test", "127.0.0.1"));
        Assert.False(guard.IsLockedOut("other@test", "127.0.0.1"));

        guard.Reset("user@test", "127.0.0.1");
        Assert.False(guard.IsLockedOut("user@test", "127.0.0.1"));
    }
}

[Collection(WebAppCollection.Name)]
public class LoginLockoutTests : IDisposable
{
    private readonly SipitexWebAppFactory _factory = new();

    [Fact]
    public async Task FiveFailedLogins_ThenCorrectPassword_StillBlocked()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        const string email = "bodega@sipitex.test";

        for (var i = 0; i < MemoryLoginAttemptGuard.MaxFailedAttempts; i++)
        {
            var html = WebUtility.HtmlDecode(await PostLoginAsync(client, email, "clave-incorrecta"));
            if (i < MemoryLoginAttemptGuard.MaxFailedAttempts - 1)
                Assert.Contains(LoginAttemptMessages.InvalidCredentials, html, StringComparison.Ordinal);
            else
                Assert.Contains(LoginAttemptMessages.LockedOut, html, StringComparison.Ordinal);
        }

        var lockedRight = WebUtility.HtmlDecode(await PostLoginAsync(client, email, "Bodega123!"));
        Assert.Contains(LoginAttemptMessages.LockedOut, lockedRight, StringComparison.Ordinal);
    }

    private static async Task<string> PostLoginAsync(HttpClient client, string email, string password)
    {
        var loginPage = await client.GetAsync("/Account/Login");
        loginPage.EnsureSuccessStatusCode();
        var token = ExtractAntiforgery(await loginPage.Content.ReadAsStringAsync());
        var post = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, post.StatusCode);
        return await post.Content.ReadAsStringAsync();
    }

    private static string ExtractAntiforgery(string html)
    {
        var match = Regex.Match(
            html,
            @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""",
            RegexOptions.IgnoreCase);
        Assert.True(match.Success, "No se encontró el token antifalsificación.");
        return match.Groups[1].Value;
    }

    public void Dispose() => _factory.Dispose();
}
