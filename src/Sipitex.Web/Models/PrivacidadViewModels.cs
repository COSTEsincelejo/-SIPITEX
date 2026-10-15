using System.ComponentModel.DataAnnotations;
using Sipitex.Application.Seguridad;

namespace Sipitex.Web.Models;

public sealed class PoliticaPrivacidadViewModel
{
    public required PrivacidadOptions Opciones { get; init; }
    public bool Autenticado { get; init; }
}

public sealed class AceptarPoliticaViewModel
{
    [Range(typeof(bool), "true", "true", ErrorMessage = "Debe aceptar la política de tratamiento de datos personales.")]
    public bool AceptaPolitica { get; set; }
}

public sealed class SolicitudDatosViewModel
{
    [Required]
    public string Tipo { get; set; } = "actualizacion";

    [Required(ErrorMessage = "Escriba qué dato quiere actualizar o por qué pide la eliminación.")]
    [StringLength(800)]
    public string Mensaje { get; set; } = string.Empty;
}
