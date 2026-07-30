namespace Sipitex.Web.Models;

// Lo que muestra la vista de error (Home/Error)
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    // Solo mostramos el ID si existe, para no dejar hueco raro en la vista
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
