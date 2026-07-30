using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

// Registros de inspección de calidad
public class QualityRepository : IQualityRepository
{
    private readonly SipitexDbContext _context;

    public QualityRepository(SipitexDbContext context) => _context = context;

    // Incluyo la orden porque en la vista muestro el número OP-xxx
    public async Task<IReadOnlyList<QualityRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.QualityRecords
            .Include(q => q.ProductionOrder)
            .OrderByDescending(q => q.InspectionDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(QualityRecord record, CancellationToken cancellationToken = default) =>
        await _context.QualityRecords.AddAsync(record, cancellationToken);
}
