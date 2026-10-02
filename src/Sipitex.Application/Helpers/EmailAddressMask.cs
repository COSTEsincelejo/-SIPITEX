namespace Sipitex.Application.Helpers;

// Destinatario para logs: deja ver el dominio y oculta el buzón.
public static class EmailAddressMask
{
    public static string Mask(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "***";

        var trimmed = email.Trim();
        var at = trimmed.IndexOf('@');
        if (at <= 0 || at >= trimmed.Length - 1)
            return "***";

        return $"{trimmed[0]}***@{trimmed[(at + 1)..]}";
    }
}
