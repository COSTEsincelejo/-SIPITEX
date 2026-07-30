using System.ComponentModel.DataAnnotations;

namespace Sipitex.Web.Models;

// Modelos para el login y la gestión de usuarios.
// Son los que reciben los formularios de Account (no van directo a la BD).

public class LoginViewModel
{
    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

// Para crear/editar usuarios desde el panel de admin
public class UserEditViewModel
{
    public int Id { get; set; }

    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    // Si viene vacío al editar, no se cambia la contraseña
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Rol { get; set; } = "Instructor";

    public int? FichaAsignadaId { get; set; }

    // Permisos extra aparte del rol (claves de ExtendedPermissions)
    public List<string> SelectedPermissions { get; set; } = [];

    public bool IsActive { get; set; } = true;
}

// El usuario edita su propio perfil
public class ProfileViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "Correo no válido")]
    [StringLength(160)]
    public string Email { get; set; } = string.Empty;

    public string Rol { get; set; } = string.Empty;

    public string? PhotoPath { get; set; }

    [StringLength(800, ErrorMessage = "La descripción no puede superar 800 caracteres.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Descripción de funciones")]
    public string? FuncionDescripcion { get; set; }

    [DataType(DataType.Password)]
    [StringLength(100)]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden")]
    public string? ConfirmPassword { get; set; }

    public bool RemovePhoto { get; set; }
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme la contraseña")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
