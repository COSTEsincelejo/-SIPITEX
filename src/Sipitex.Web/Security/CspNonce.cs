using System.Security.Cryptography;

namespace Sipitex.Web.Security;

// Nonce de un solo uso por respuesta. Los <script> sin src lo reciben para no usar 'unsafe-inline'.
public static class CspNonce
{
    public const string ItemKey = "SipitexCspNonce";

    public static string Crear() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

    public static string De(HttpContext http) =>
        http.Items[ItemKey] as string ?? "";
}
