using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Matriz de cumplimiento de requisitos RF/RNF
public interface IRequirementService
{
    Task<RequirementsViewDto> GetComplianceAsync(CancellationToken cancellationToken = default);
}
