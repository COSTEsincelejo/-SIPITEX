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
        // Con esto traigo RF y RNF por separado desde el repo
        var rf = await _requirementRepository.GetFunctionalAsync(cancellationToken);
        var rnf = await _requirementRepository.GetNonFunctionalAsync(cancellationToken);

        // Armo el DTO con resúmenes y listas detalladas
        return new RequirementsViewDto(
            Summarize(rf.Select(r => r.Status)),
            Summarize(rnf.Select(r => r.Status)),
            rf.Select(r => new FunctionalRequirementDto(r.Code, r.Description, r.Module, r.Status, r.Observation)).ToList(),
            rnf.Select(r => new NonFunctionalRequirementDto(r.Code, r.Description, r.Status, r.Observation)).ToList());
    }

    // Cuenta cuántos cumplen, parcial o ausente
    private static RequirementSummaryDto Summarize(IEnumerable<ComplianceStatus> statuses)
    {
        // Materializo para no enumerar dos veces
        var list = statuses.ToList();
        return new RequirementSummaryDto(
            // Cuántos están en verde (cumple)
            list.Count(s => s == ComplianceStatus.Cumple),
            // Cuántos en amarillo (parcial)
            list.Count(s => s == ComplianceStatus.Parcial),
            // Cuántos en rojo (ausente)
            list.Count(s => s == ComplianceStatus.Ausente));
    }
}
