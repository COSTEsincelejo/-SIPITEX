using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

public interface IFichaRepository
{
    Task<IReadOnlyList<Ficha>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Ficha?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    void Update(Ficha ficha);
}
