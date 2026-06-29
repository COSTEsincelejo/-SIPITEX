using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

public interface IRequirementRepository
{
    Task<IReadOnlyList<FunctionalRequirement>> GetFunctionalAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NonFunctionalRequirement>> GetNonFunctionalAsync(CancellationToken cancellationToken = default);
}
