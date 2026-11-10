using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Web.Helpers;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class VerificarController : Controller
{
    public const string PoliticaLimite = "verificar-acta";

    private readonly IActaMovimientoService _actas;

    public VerificarController(IActaMovimientoService actas) => _actas = actas;

    [HttpGet]
    [EnableRateLimiting(PoliticaLimite)]
    public async Task<IActionResult> Acta(string? numero, string? codigo, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Verificar acta";
        ViewData["Breadcrumb"] = "SIPITEX / Verificar acta";
        var resultado = await _actas.VerificarPublicoAsync(numero, codigo, cancellationToken);
        if (!resultado.Autentica || resultado.Numero is null || resultado.Tipo is null || resultado.FechaUtc is null || resultado.Estado is null)
        {
            return View(new VerificarActaViewModel());
        }

        return View(new VerificarActaViewModel
        {
            Autentica = true,
            Numero = resultado.Numero,
            Tipo = DisplayHelper.StatusText(resultado.Tipo.Value),
            Fecha = $"{resultado.FechaUtc.Value:yyyy-MM-dd HH:mm} UTC",
            Estado = DisplayHelper.StatusText(resultado.Estado.Value),
            CantidadItems = resultado.CantidadItems
        });
    }
}
