namespace Sipitex.Application.Authorization;

// Nombres de las policies que registramos en Program.cs
public static class AuthorizationPolicyNames
{
    // Quién puede dar de alta materiales en inventario
    public const string PuedeRegistrarMateriales = "PuedeRegistrarMateriales";
    // Quién puede aprobar solicitudes de bodega
    public const string PuedeAprobarSolicitudes = "PuedeAprobarSolicitudes";
    // Quién puede correr simulación MRP
    public const string PuedeSimularMrp = "PuedeSimularMrp";
    // Quién puede crear/editar fichas técnicas (BOM)
    public const string PuedeGestionarFichasTecnicas = "PuedeGestionarFichasTecnicas";
    // Quién puede configurar alertas por correo
    public const string PuedeConfigurarAlertas = "PuedeConfigurarAlertas";
}
