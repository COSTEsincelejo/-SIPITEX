using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

public interface IRequirementService
{
    Task<RequirementsViewDto> GetComplianceAsync(CancellationToken cancellationToken = default);
}
