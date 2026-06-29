using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

public interface IQualityService
{
    Task<IReadOnlyList<QualityRecordDto>> GetRecordsAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> AddRecordAsync(CreateQualityRecordDto dto, CancellationToken cancellationToken = default);
}
