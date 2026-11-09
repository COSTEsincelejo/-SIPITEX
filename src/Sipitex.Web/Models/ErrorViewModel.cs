namespace Sipitex.Web.Models;

// Lo que muestra la vista de error (Home/Error)
public class ErrorViewModel
{
    // ID de la petición para rastrear el error en logs
    public string? RequestId { get; set; }

    // Código corto que el usuario puede informar. No es el stack trace.
    public string? CodigoError { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public bool ShowCodigoError => !string.IsNullOrEmpty(CodigoError);
}
