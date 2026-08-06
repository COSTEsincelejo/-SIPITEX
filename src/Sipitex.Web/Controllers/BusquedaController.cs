using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Web.Controllers;

// Endpoint ligero JSON para el buscador del header (aislado de otros controllers)
[Authorize]
[Route("api/busqueda")]
[ApiController]
public class BusquedaController : ControllerBase
{
    private readonly IBusquedaService _busquedaService;

    public BusquedaController(IBusquedaService busquedaService) =>
        _busquedaService = busquedaService;

    // GET /api/busqueda?q=texto
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var query = (q ?? string.Empty).Trim();
        if (query.Length < 1)
            return Ok(new { resultados = Array.Empty<object>() });

        var items = await _busquedaService.SearchAsync(query, cancellationToken);
        return Ok(new
        {
            resultados = items.Select(i => new
            {
                texto = i.Texto,
                url = i.Url,
                categoria = i.Categoria
            })
        });
    }
}
