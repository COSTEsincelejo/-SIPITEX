using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Seguridad;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;
using Sipitex.Web.Security;

namespace Sipitex.Web.Controllers;

// Política pública y la pantalla que bloquea el resto de la app hasta aceptar la versión vigente.
public class PrivacidadController : Controller
{
    private readonly PrivacidadOptions _opciones;
    private readonly IAceptacionTratamientoService _aceptaciones;
    private readonly IUserAccountService _cuentas;
    private readonly IActivityLogService _actividad;
    private readonly IEmailSender _correo;

    public PrivacidadController(
        IOptions<PrivacidadOptions> opciones,
        IAceptacionTratamientoService aceptaciones,
        IUserAccountService cuentas,
        IActivityLogService actividad,
        IEmailSender correo)
    {
        _opciones = opciones.Value;
        _aceptaciones = aceptaciones;
        _cuentas = cuentas;
        _actividad = actividad;
        _correo = correo;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "Política de tratamiento de datos";
        ViewData["Breadcrumb"] = "SIPITEX / Privacidad";
        return View(new PoliticaPrivacidadViewModel
        {
            Opciones = _opciones,
            Autenticado = User.Identity?.IsAuthenticated == true
        });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Aceptar(CancellationToken cancellationToken)
    {
        if (await YaAceptoAsync(cancellationToken))
            return RedirectToAction("Index", "Home");

        ViewData["Title"] = "Aceptar política de datos";
        ViewData["Breadcrumb"] = "SIPITEX / Privacidad";
        return View(new AceptarPoliticaViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aceptar(AceptarPoliticaViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (!TryUserId(out var userId))
            return Challenge();

        await _aceptaciones.RegistrarAsync(userId, Ip(), cancellationToken);
        await _actividad.LogAsync(
            userId,
            ActivityLogActions.AceptarPoliticaDatos,
            ActivityLogEntities.User,
            userId.ToString(),
            "Version=" + _opciones.VersionVigente(),
            cancellationToken);

        var user = await _cuentas.GetUserByIdAsync(userId, cancellationToken);
        if (user is not null)
            await SesionAutenticada.IniciarAsync(HttpContext, user, _opciones.VersionVigente());

        TempData["Message"] = "Quedó registrada su aceptación de la política de tratamiento de datos.";
        TempData["IsSuccess"] = true;
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Solicitar(SolicitudDatosViewModel model, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Challenge();

        if (!ModelState.IsValid)
        {
            TempData["Message"] = "Escriba el detalle de la solicitud.";
            TempData["IsSuccess"] = false;
            return RedirectToAction("Profile", "Account");
        }

        var esSupresion = string.Equals(model.Tipo, "supresion", StringComparison.OrdinalIgnoreCase);
        var accion = esSupresion
            ? ActivityLogActions.SolicitarSupresionDatos
            : ActivityLogActions.SolicitarActualizacionDatos;
        var texto = RedactorDatosSensibles.Limpiar(model.Mensaje.Trim()) ?? "";

        await _actividad.LogAsync(
            userId,
            accion,
            ActivityLogEntities.User,
            userId.ToString(),
            texto,
            cancellationToken);

        await AvisarResponsableAsync(userId, esSupresion, texto, cancellationToken);

        TempData["Message"] = "La solicitud quedó registrada. El responsable del tratamiento la verá en Auditoría.";
        TempData["IsSuccess"] = true;
        return RedirectToAction("Profile", "Account");
    }

    private async Task AvisarResponsableAsync(int userId, bool supresion, string texto, CancellationToken cancellationToken)
    {
        var destino = _opciones.CorreoContacto?.Trim();
        if (string.IsNullOrWhiteSpace(destino))
            return;

        var user = await _cuentas.GetUserByIdAsync(userId, cancellationToken);
        var tipo = supresion ? "supresión" : "actualización";
        var cuerpo = "Solicitud de " + tipo + " de datos personales.\n"
            + "Usuario: " + (user?.Nombre ?? "#" + userId) + "\n"
            + "Correo: " + (user?.Email ?? "") + "\n"
            + "Detalle: " + texto;

        try
        {
            await _correo.SendAsync(
                destino,
                _opciones.ResponsableNombre,
                "SIPITEX · solicitud de " + tipo + " de datos",
                cuerpo,
                cancellationToken);
        }
        catch
        {
            // La auditoría ya quedó. Un correo caído no debe perder la solicitud.
        }
    }

    private async Task<bool> YaAceptoAsync(CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return false;
        var version = await _aceptaciones.VersionVigenteAsync(userId, cancellationToken);
        return version is not null;
    }

    private bool TryUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;

    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
