using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

public class RequirementService : IRequirementService
{
    private readonly IRequirementRepository _requirementRepository;

    public RequirementService(IRequirementRepository requirementRepository)
    {
        _requirementRepository = requirementRepository;
    }

    public async Task<RequirementsViewDto> GetComplianceAsync(CancellationToken cancellationToken = default)
    {
        var rf = await _requirementRepository.GetFunctionalAsync(cancellationToken);
        var rnf = await _requirementRepository.GetNonFunctionalAsync(cancellationToken);

        return new RequirementsViewDto(
            Summarize(rf.Select(r => r.Status)),
            Summarize(rnf.Select(r => r.Status)),
            rf.Select(r => new FunctionalRequirementDto(r.Code, r.Description, r.Module, r.Status, r.Observation)).ToList(),
            rnf.Select(r => new NonFunctionalRequirementDto(r.Code, r.Description, r.Status, r.Observation)).ToList());
    }

    private static RequirementSummaryDto Summarize(IEnumerable<ComplianceStatus> statuses)
    {
        var list = statuses.ToList();
        return new RequirementSummaryDto(
            list.Count(s => s == ComplianceStatus.Cumple),
            list.Count(s => s == ComplianceStatus.Parcial),
            list.Count(s => s == ComplianceStatus.Ausente));
    }
}
