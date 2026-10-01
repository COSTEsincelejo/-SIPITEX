using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Authorization;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Search;
using Sipitex.Domain.Entities;

namespace Sipitex.Web.Controllers;

// Sugerencias del buscador del encabezado. No registra el texto de la consulta.
[Authorize]
public class BuscarController : Controller
{
    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(2);
    private readonly IBuscadorSugerenciasService _buscador;
    private readonly ICurrentPlantaInventarioAccessor _plantas;

    public BuscarController(IBuscadorSugerenciasService buscador, ICurrentPlantaInventarioAccessor plantas)
    {
        _buscador = buscador;
        _plantas = plantas;
    }

    [HttpGet]
    public async Task<IActionResult> Sugerencias(string? q, CancellationToken cancellationToken)
    {
        var consulta = (q ?? string.Empty).Trim();
        if (consulta.Length < 2)
            return Json(SugerenciasBusquedaDto.Vacias());

        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limite.CancelAfter(Limite);
        try
        {
            var dto = await _buscador.SugerirAsync(consulta, AlcanceActual(), limite.Token);
            return Json(dto);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Json(SugerenciasBusquedaDto.SinEntender());
        }
    }

    private AlcanceBusqueda AlcanceActual()
    {
        var rol = User.FindFirstValue(ClaimTypes.Role) ?? "";
        int? userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0
            ? id
            : null;
        var esInstructor = string.Equals(rol, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase);
        var esAdmin = string.Equals(rol, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase);
        var esEncargado = string.Equals(rol, UserRoles.EncargadoDeBodega, StringComparison.OrdinalIgnoreCase);

        return new AlcanceBusqueda
        {
            Rol = rol,
            UserId = userId,
            Nombre = User.Identity?.Name,
            PlantaInventarioIds = _plantas.PlantaInventarioIds,
            PuedeVerInventario = esAdmin || esEncargado,
            PuedeVerUsuarios = esAdmin,
            PuedeVerGrupos = esAdmin || esInstructor,
            PuedeVerMovimientos = esAdmin || esEncargado,
            PuedeVerReingreso = esAdmin || esEncargado,
            PuedeVerTodasLasFichasTecnicas = !esInstructor || PermissionRules.PuedeGestionarFichasTecnicas(User),
            EsInstructor = esInstructor
        };
    }
}
