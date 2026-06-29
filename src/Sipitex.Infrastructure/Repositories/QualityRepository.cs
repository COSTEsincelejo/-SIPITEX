using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class QualityRepository : IQualityRepository
{
    private readonly SipitexDbContext _context;

    public QualityRepository(SipitexDbContext context) => _context = context;

    public async Task<IReadOnlyList<QualityRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.QualityRecords
            .Include(q => q.ProductionOrder)
            .OrderByDescending(q => q.InspectionDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(QualityRecord record, CancellationToken cancellationToken = default) =>
        await _context.QualityRecords.AddAsync(record, cancellationToken);
}
