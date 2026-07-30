using Microsoft.EntityFrameworkCore; // OrderBy, ToListAsync
using Sipitex.Application.Interfaces.Repositories; // IRequirementRepository
using Sipitex.Domain.Entities; // FunctionalRequirement, NonFunctionalRequirement
using Sipitex.Infrastructure.Persistence; // SipitexDbContext

namespace Sipitex.Infrastructure.Repositories;

// Requisitos funcionales y no funcionales del proyecto (tabla de trazabilidad)
public class RequirementRepository : IRequirementRepository
{
    private readonly SipitexDbContext _context;

    public RequirementRepository(SipitexDbContext context) => _context = context;

    // Lista todos los RF ordenados por código (RF01, RF02...)
    public async Task<IReadOnlyList<FunctionalRequirement>> GetFunctionalAsync(CancellationToken cancellationToken = default) =>
        await _context.FunctionalRequirements.OrderBy(r => r.Code).ToListAsync(cancellationToken);

    // Lista todos los RNF ordenados por código
    public async Task<IReadOnlyList<NonFunctionalRequirement>> GetNonFunctionalAsync(CancellationToken cancellationToken = default) =>
        await _context.NonFunctionalRequirements.OrderBy(r => r.Code).ToListAsync(cancellationToken);
}
