namespace Sipitex.Web.Models;

// Lo que muestra la vista de error (Home/Error)
public class ErrorViewModel
{
    // ID de la petición para rastrear el error en logs
    public string? RequestId { get; set; }

    // Solo mostramos el ID si existe, para no dejar hueco raro en la vista
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
