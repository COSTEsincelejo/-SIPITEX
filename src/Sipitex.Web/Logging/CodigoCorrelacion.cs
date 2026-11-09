using System.Security.Cryptography;

namespace Sipitex.Web.Logging;

// Código corto para que el usuario lo informe sin ver la excepción.
public static class CodigoCorrelacion
{
    public const string Clave = "Sipitex.CodigoError";

    public static string Crear()
    {
        Span<byte> bytes = stackalloc byte[3];
        RandomNumberGenerator.Fill(bytes);
        return "ERR-" + Convert.ToHexString(bytes);
    }
}
