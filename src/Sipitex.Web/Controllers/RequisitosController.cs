using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

public class RequisitosController : Controller
{
    private readonly IRequirementService _requirementService;

    public RequisitosController(IRequirementService requirementService) => _requirementService = requirementService;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new RequisitosIndexViewModel
        {
            Requirements = await _requirementService.GetComplianceAsync(cancellationToken)
        });
    }
}
