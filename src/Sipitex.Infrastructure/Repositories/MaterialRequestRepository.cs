using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class MaterialRequestRepository : IMaterialRequestRepository
{
    private readonly SipitexDbContext _context;

    public MaterialRequestRepository(SipitexDbContext context) => _context = context;

    public async Task<IReadOnlyList<MaterialRequest>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.MaterialRequests
            .Include(r => r.Material)
            .Include(r => r.ProductionOrder)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(MaterialRequest request, CancellationToken cancellationToken = default) =>
        await _context.MaterialRequests.AddAsync(request, cancellationToken);

    public Task<MaterialRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.MaterialRequests
            .Include(r => r.Material)
            .Include(r => r.ProductionOrder)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public void Update(MaterialRequest request) => _context.MaterialRequests.Update(request);
}
