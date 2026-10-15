using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Seguridad;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Services;

public sealed class AceptacionTratamientoService : IAceptacionTratamientoService
{
    private readonly SipitexDbContext _db;
    private readonly IUserRepository _users;
    private readonly PrivacidadOptions _opciones;

    public AceptacionTratamientoService(
        SipitexDbContext db,
        IUserRepository users,
        IOptions<PrivacidadOptions> opciones)
    {
        _db = db;
        _users = users;
        _opciones = opciones.Value;
    }

    public async Task<string?> VersionVigenteAsync(int userId, CancellationToken cancellationToken = default)
    {
        var ultima = await UltimaAsync(userId, cancellationToken);
        var vigente = _opciones.VersionVigente();
        return ultima is not null && string.Equals(ultima.VersionPolitica, vigente, StringComparison.Ordinal)
            ? vigente
            : null;
    }

    public async Task<AceptacionTratamientoDto?> UltimaAsync(int userId, CancellationToken cancellationToken = default)
    {
        var fila = await _db.AceptacionesTratamiento.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.AceptadaEnUtc)
            .ThenByDescending(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return fila is null
            ? null
            : new AceptacionTratamientoDto(fila.VersionPolitica, fila.AceptadaEnUtc, fila.DireccionIp);
    }

    public async Task RegistrarAsync(int userId, string? direccionIp, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return;

        var version = _opciones.VersionVigente();
        var ya = await _db.AceptacionesTratamiento
            .AnyAsync(a => a.UserId == userId && a.VersionPolitica == version, cancellationToken);
        if (ya)
            return;

        _db.AceptacionesTratamiento.Add(new AceptacionTratamiento
        {
            UserId = userId,
            VersionPolitica = version,
            AceptadaEnUtc = DateTime.UtcNow,
            DireccionIp = RecortarIp(direccionIp)
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RegistrarPorCorreoAsync(string email, string? direccionIp, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        var user = await _users.GetByEmailAsync(email, cancellationToken);
        if (user is null)
            return;

        await RegistrarAsync(user.Id, direccionIp, cancellationToken);
    }

    private static string? RecortarIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return null;
        var limpia = ip.Trim();
        return limpia.Length <= 64 ? limpia : limpia[..64];
    }
}
