namespace Sipitex.Application.Authorization;

// Nombres de las policies que registramos en Program.cs
public static class AuthorizationPolicyNames
{
    // Quién puede entrar al módulo Inventario (consulta y acciones)
    public const string PuedeAccederInventario = "PuedeAccederInventario";
    // Quién puede dar de alta materiales en inventario
    public const string PuedeRegistrarMateriales = "PuedeRegistrarMateriales";
    // Quién puede aprobar solicitudes de bodega
    public const string PuedeAprobarSolicitudes = "PuedeAprobarSolicitudes";
    // Quién puede correr simulación MRP
    public const string PuedeSimularMrp = "PuedeSimularMrp";
    // Quién puede configurar alertas por correo
    public const string PuedeConfigurarAlertas = "PuedeConfigurarAlertas";
}
