namespace Sipitex.Application.Interfaces.Services;

// El proveedor no entregó el mensaje. El texto es el que ve el usuario.
// No incluye el código de 6 dígitos ni la API key.
public class EmailDeliveryException : Exception
{
    public const string UserMessage = "No pudimos enviar el correo, intente de nuevo";

    public int? StatusCode { get; }

    public EmailDeliveryException(string message, int? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
