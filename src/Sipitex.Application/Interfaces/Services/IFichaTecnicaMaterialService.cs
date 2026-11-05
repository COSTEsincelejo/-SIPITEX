using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

public interface IFichaTecnicaMaterialService
{
    Task<FichaTecnicaMaterialDetalle?> ObtenerAsync(int materialId, CancellationToken cancellationToken = default);

    Task<FichaTecnicaMaterialResult> GuardarAsync(
        GuardarFichaTecnicaMaterialDto dto,
        int actorUserId,
        CancellationToken cancellationToken = default);
}
