using System.ComponentModel.DataAnnotations;

namespace Sipitex.Web.Models;

// Modelos para el login y la gestión de usuarios.
// Son los que reciben los formularios de Account (no van directo a la BD).

// Formulario de inicio de sesión
public class LoginViewModel
{
    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress] // valida formato de correo
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [DataType(DataType.Password)] // en la vista se renderiza como input password
    public string Password { get; set; } = string.Empty;
}

// Para crear/editar usuarios desde el panel de admin
public class UserEditViewModel
{
    public int Id { get; set; } // 0 al crear, >0 al editar

    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    // Si viene vacío al editar, no se cambia la contraseña
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Rol { get; set; } = "Instructor"; // Instructor o Bodeguero al crear

    public int? FichaAsignadaId { get; set; } // opcional, solo para instructores

    public int? BodegaId { get; set; } // opcional; obligatorio si Rol == Bodeguero

    // Permisos extra aparte del rol (claves de ExtendedPermissions)
    public List<string> SelectedPermissions { get; set; } = [];

    public bool IsActive { get; set; } = true; // usuario activo o desactivado
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

    public string Rol { get; set; } = string.Empty; // solo lectura, lo pone el admin

    public string? PhotoPath { get; set; } // ruta de la foto actual en wwwroot

    [StringLength(800, ErrorMessage = "La descripción no puede superar 800 caracteres.")]
    [DataType(DataType.MultilineText)] // textarea en la vista
    [Display(Name = "Descripción de funciones")]
    public string? FuncionDescripcion { get; set; }

    [DataType(DataType.Password)]
    [StringLength(100)]
    public string? NewPassword { get; set; } // opcional al guardar perfil

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden")]
    public string? ConfirmPassword { get; set; }

    public bool RemovePhoto { get; set; } // checkbox para borrar la foto
}

// Paso 1 de recuperar contraseña: solo pide el correo
public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

// Paso 2: el usuario llega con token en el link del correo
public class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty; // viene oculto en el form

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme la contraseña")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
