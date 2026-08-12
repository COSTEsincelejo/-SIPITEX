using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Inspecciones de calidad por orden
public interface IQualityService
{
    Task<IReadOnlyList<QualityRecordDto>> GetRecordsAsync(
        int? viewerUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> AddRecordAsync(
        CreateQualityRecordDto dto,
        int? viewerUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default);
}
