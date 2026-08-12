using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sipitex.Application.Authorization;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class InstructorInventarioAccessTests
    : IClassFixture<InstructorInventarioAccessTests.AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;

    public InstructorInventarioAccessTests(AuthWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public void PuedeAccederInventario_InstructorWithoutClaim_IsDenied()
    {
        var user = CreateInstructor();
        Assert.False(PermissionRules.PuedeAccederInventario(user));
    }

    [Fact]
    public void PuedeAccederInventario_InstructorWithInventarioRegistrar_IsAllowed()
    {
        var user = CreateInstructor(ExtendedPermissions.InventarioRegistrar);
        Assert.True(PermissionRules.PuedeAccederInventario(user));
    }

    [Fact]
    public void PuedeAccederInventario_Administrador_IsAllowed()
    {
        var user = CreatePrincipal(UserRoles.Administrador);
        Assert.True(PermissionRules.PuedeAccederInventario(user));
    }

    [Fact]
    public void PuedeAccederInventario_Bodeguero_IsAllowed()
    {
        var user = CreatePrincipal(UserRoles.Bodeguero);
        Assert.True(PermissionRules.PuedeAccederInventario(user));
    }

    [Fact]
    public async Task InventarioIndex_InstructorWithoutPermission_IsForbiddenOrAccessDenied()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, UserRoles.Instructor);

        var response = await client.GetAsync("/Inventario");

        AssertAccessDenied(response);
    }

    [Fact]
    public async Task InventarioIndex_InstructorWithInventarioRegistrar_IsAllowed()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, UserRoles.Instructor);
        client.DefaultRequestHeaders.Add(
            TestAuthHandler.PermissionsHeader,
            ExtendedPermissions.InventarioRegistrar);

        var response = await client.GetAsync("/Inventario");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InventarioIndex_Bodeguero_IsAllowed()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, UserRoles.Bodeguero);

        var response = await client.GetAsync("/Inventario");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static void AssertAccessDenied(HttpResponseMessage response)
    {
        // Cookie auth: Forbid → redirect a AccessDenied; algunos hosts devuelven 403 directo
        if (response.StatusCode == HttpStatusCode.Forbidden)
            return;

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "/Account/AccessDenied",
            response.Headers.Location?.OriginalString ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }

    private static ClaimsPrincipal CreateInstructor(params string[] permissions) =>
        CreatePrincipal(UserRoles.Instructor, permissions);

    private static ClaimsPrincipal CreatePrincipal(string role, params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "42"),
            new(ClaimTypes.Name, "Usuario Test"),
            new(ClaimTypes.Role, role)
        };
        foreach (var permission in permissions)
            claims.Add(new Claim(ExtendedPermissions.ClaimType, permission));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }

    public sealed class AuthWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
    {
        private readonly string _dbPath = Path.Combine(
            Path.GetTempPath(),
            $"sipitex-inv-auth-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseSetting(
                Microsoft.AspNetCore.Hosting.WebHostDefaults.EnvironmentKey,
                "Development");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}",
                    ["Email:Enabled"] = "false"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                        options.DefaultForbidScheme = TestAuthHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            try
            {
                if (File.Exists(_dbPath)) File.Delete(_dbPath);
                foreach (var suffix in new[] { "-shm", "-wal" })
                {
                    var side = _dbPath + suffix;
                    if (File.Exists(side)) File.Delete(side);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "Test";
        public const string RoleHeader = "X-Test-Role";
        public const string PermissionsHeader = "X-Test-Permissions";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(RoleHeader, out var roleValues)
                || string.IsNullOrWhiteSpace(roleValues.ToString()))
            {
                return Task.FromResult(AuthenticateResult.Fail("Sin rol de prueba."));
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "10"),
                new(ClaimTypes.Name, "Instructor Test"),
                new(ClaimTypes.Email, "instructor@test.local"),
                new(ClaimTypes.Role, roleValues.ToString())
            };

            if (Request.Headers.TryGetValue(PermissionsHeader, out var permValues))
            {
                foreach (var perm in permValues.ToString()
                             .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    claims.Add(new Claim(ExtendedPermissions.ClaimType, perm));
                }
            }

            var identity = new ClaimsIdentity(claims, SchemeName);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
