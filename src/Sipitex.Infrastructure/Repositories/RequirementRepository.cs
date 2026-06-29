using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class RequirementRepository : IRequirementRepository
{
    private readonly SipitexDbContext _context;

    public RequirementRepository(SipitexDbContext context) => _context = context;

    public async Task<IReadOnlyList<FunctionalRequirement>> GetFunctionalAsync(CancellationToken cancellationToken = default) =>
        await _context.FunctionalRequirements.OrderBy(r => r.Code).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NonFunctionalRequirement>> GetNonFunctionalAsync(CancellationToken cancellationToken = default) =>
        await _context.NonFunctionalRequirements.OrderBy(r => r.Code).ToListAsync(cancellationToken);
}
