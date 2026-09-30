namespace Sipitex.Web.Hosting;

/// <summary>
/// Render inyecta PORT (por defecto 10000) y espera que el proceso escuche en 0.0.0.0.
/// Si la app queda en otro puerto, el deploy se marca fallido y sigue vivo el anterior.
/// </summary>
public static class RenderListen
{
    public static string? ListenUrlFor(string? port)
    {
        if (string.IsNullOrWhiteSpace(port))
            return null;

        if (!int.TryParse(port.Trim(), out var parsed) || parsed is < 1 or > 65535)
        {
            throw new InvalidOperationException(
                $"PORT='{port.Trim()}' no es un puerto TCP válido. Render inyecta PORT (por defecto 10000).");
        }

        return $"http://0.0.0.0:{parsed}";
    }

    public static void Apply()
    {
        var url = ListenUrlFor(Environment.GetEnvironmentVariable("PORT"));
        if (url is null)
            return;

        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", url);
    }
}
