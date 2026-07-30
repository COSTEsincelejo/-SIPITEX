namespace Sipitex.Application.Authorization;

// Nombres de las policies que registramos en Program.cs
public static class AuthorizationPolicyNames
{
    public const string PuedeRegistrarMateriales = "PuedeRegistrarMateriales";
    public const string PuedeAprobarSolicitudes = "PuedeAprobarSolicitudes";
    public const string PuedeSimularMrp = "PuedeSimularMrp";
    public const string PuedeConfigurarAlertas = "PuedeConfigurarAlertas";
}
