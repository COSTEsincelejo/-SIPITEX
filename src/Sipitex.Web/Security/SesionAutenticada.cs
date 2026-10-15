using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Web.Security;

// Arma la cookie de rol, foto, plantas y la versión de política aceptada.
public static class SesionAutenticada
{
    public const string PoliticaClaim = "sipitex.politica";
    public const string PhotoClaimType = "photo";

    public static async Task IniciarAsync(HttpContext http, User user, string? versionPoliticaAceptada)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Nombre),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Rol)
        };

        if (!string.IsNullOrWhiteSpace(user.PhotoPath))
            claims.Add(new Claim(PhotoClaimType, user.PhotoPath));

        if (string.Equals(user.Rol, UserRoles.EncargadoDeBodega, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var plantaId in user.GetAssignedPlantaInventarioIds())
                claims.Add(new Claim(PlantaInventarioClaimTypes.PlantaInventarioId, plantaId.ToString()));
        }

        foreach (var permiso in ExtendedPermissions.Parse(user.PermisosExtendidos))
            claims.Add(new Claim(ExtendedPermissions.ClaimType, permiso));

        if (!string.IsNullOrWhiteSpace(versionPoliticaAceptada))
            claims.Add(new Claim(PoliticaClaim, versionPoliticaAceptada));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
    }

    public static bool PideJson(HttpRequest request)
    {
        if (string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            return true;

        var accept = request.Headers.Accept.ToString();
        return accept.Contains("application/json", StringComparison.OrdinalIgnoreCase)
            && !accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
