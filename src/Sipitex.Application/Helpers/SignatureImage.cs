namespace Sipitex.Application.Helpers;

// PNG dibujado en canvas (data URL) → bytes validados para persistir en actas.
public static class SignatureImage
{
    public const int MaxBytes = 100_000;
    private const string PngPrefix = "data:image/png;base64,";

    public static bool IsPng(byte[]? bytes) =>
        bytes is { Length: >= 24 and <= MaxBytes }
        && bytes[0] == 0x89
        && bytes[1] == 0x50
        && bytes[2] == 0x4E
        && bytes[3] == 0x47;

    public static byte[]? FromDataUrl(string? dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl))
            return null;

        var trimmed = dataUrl.Trim();
        var comma = trimmed.IndexOf(',');
        if (comma < 0)
            return null;

        var header = trimmed[..comma];
        if (header.IndexOf("image/png", StringComparison.OrdinalIgnoreCase) < 0)
            return null;

        try
        {
            var bytes = Convert.FromBase64String(trimmed[(comma + 1)..].Trim());
            return IsPng(bytes) ? bytes : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public static string? ToDataUrl(byte[]? png) =>
        IsPng(png) ? PngPrefix + Convert.ToBase64String(png!) : null;
}
