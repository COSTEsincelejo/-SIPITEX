using Microsoft.EntityFrameworkCore; // Include, OrderByDescending...
using Sipitex.Application.Interfaces.Repositories; // IQualityRepository
using Sipitex.Domain.Entities; // QualityRecord
using Sipitex.Infrastructure.Persistence; // SipitexDbContext

namespace Sipitex.Infrastructure.Repositories;

// Registros de inspección de calidad
public class QualityRepository : IQualityRepository
{
    private readonly SipitexDbContext _context;

    public QualityRepository(SipitexDbContext context) => _context = context;

    // Incluyo la orden porque en la vista muestro el número OP-xxx
    public async Task<IReadOnlyList<QualityRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.QualityRecords
            .Include(q => q.ProductionOrder) // Traigo la orden para el número OP-xxx
            .OrderByDescending(q => q.InspectionDate) // Inspecciones recientes primero
            .ToListAsync(cancellationToken);

    // Guarda un registro de inspección nuevo
    public async Task AddAsync(QualityRecord record, CancellationToken cancellationToken = default) =>
        await _context.QualityRecords.AddAsync(record, cancellationToken);
}
