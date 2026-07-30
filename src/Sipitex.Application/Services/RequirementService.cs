using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Matriz de requisitos del proyecto (RF/RNF) para la vista de cumplimiento
public class RequirementService : IRequirementService
{
    private readonly IRequirementRepository _requirementRepository;

    public RequirementService(IRequirementRepository requirementRepository)
    {
        _requirementRepository = requirementRepository;
    }

    // Arma el resumen + listas de requisitos funcionales y no funcionales
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

    // Cuenta cuántos cumplen, parcial o ausente
    private static RequirementSummaryDto Summarize(IEnumerable<ComplianceStatus> statuses)
    {
        var list = statuses.ToList();
        return new RequirementSummaryDto(
            list.Count(s => s == ComplianceStatus.Cumple),
            list.Count(s => s == ComplianceStatus.Parcial),
            list.Count(s => s == ComplianceStatus.Ausente));
    }
}
