using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

public interface IMaterialRequestRepository
{
    Task<IReadOnlyList<MaterialRequest>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(MaterialRequest request, CancellationToken cancellationToken = default);
    Task<MaterialRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    void Update(MaterialRequest request);
}
