using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Inspecciones de control de calidad
public interface IQualityRepository
{
    Task<IReadOnlyList<QualityRecord>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(QualityRecord record, CancellationToken cancellationToken = default);
}
