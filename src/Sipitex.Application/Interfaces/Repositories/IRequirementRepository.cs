using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Requisitos funcionales y no funcionales del proyecto
public interface IRequirementRepository
{
    Task<IReadOnlyList<FunctionalRequirement>> GetFunctionalAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NonFunctionalRequirement>> GetNonFunctionalAsync(CancellationToken cancellationToken = default);
}
