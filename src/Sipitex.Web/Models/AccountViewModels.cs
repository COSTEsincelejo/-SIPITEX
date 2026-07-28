using System.ComponentModel.DataAnnotations;

namespace Sipitex.Web.Models;

// Modelos para el login y la gestión de usuarios.
public class LoginViewModel
{
    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class UserEditViewModel
{
    public int Id { get; set; }

    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Rol { get; set; } = "Instructor";

    public int? FichaAsignadaId { get; set; }

    /// <summary>Permisos extendidos seleccionados (claves de <c>ExtendedPermissions</c>).</summary>
    public List<string> SelectedPermissions { get; set; } = [];

    public bool IsActive { get; set; } = true;
}
