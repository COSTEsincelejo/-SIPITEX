namespace Sipitex.Domain.Entities;

// Clave/valor persistido (tarifas, flags) para no redeployar por un ajuste de CMTC.
public class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
