namespace Sipitex.Application.Interfaces.Services;

// Rate limit de solicitudes de código por IP (además del límite por usuario en BD).
public interface IAuthCodeRequestGuard
{
    bool IsLimited(string? ipAddress, string purpose);
    void Record(string? ipAddress, string purpose);
}
