using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Actas de verificación (observación + firma del instructor)
public interface IActaVerificacionRepository
{
    Task<IReadOnlyList<ActaVerificacion>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ActaVerificacion?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(ActaVerificacion acta, CancellationToken cancellationToken = default);
    void Update(ActaVerificacion acta);
}
