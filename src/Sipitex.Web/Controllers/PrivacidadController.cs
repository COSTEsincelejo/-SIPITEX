using System.Security.Claims;
using System.Text;
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
    public const string EstadoRecibida = "recibida";
    public const string PlazoReclamo = "15 días hábiles, prorrogables 8";

    private readonly PrivacidadOptions _opciones;
    private readonly int _minutosInactividad;
    private readonly IAceptacionTratamientoService _aceptaciones;
    private readonly IUserAccountService _cuentas;
    private readonly IActivityLogService _actividad;
    private readonly IEmailSender _correo;

    public PrivacidadController(
        IOptions<PrivacidadOptions> opciones,
        IOptions<SeguridadOptions> seguridad,
        IAceptacionTratamientoService aceptaciones,
        IUserAccountService cuentas,
        IActivityLogService actividad,
        IEmailSender correo)
    {
        _opciones = opciones.Value;
        _minutosInactividad = seguridad.Value.MinutosEfectivos();
        _aceptaciones = aceptaciones;
        _cuentas = cuentas;
        _actividad = actividad;
        _correo = correo;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Index(string? version)
    {
        var pedida = string.IsNullOrWhiteSpace(version) ? _opciones.VersionVigente() : version.Trim();
        if (!string.Equals(pedida, PoliticaPrivacidad.VersionAnterior, StringComparison.Ordinal))
            pedida = _opciones.VersionVigente();

        ViewData["Title"] = "Política de tratamiento de datos";
        ViewData["Breadcrumb"] = "SIPITEX / Privacidad";
        return View(new PoliticaPrivacidadViewModel
        {
            Opciones = _opciones,
            Documento = PoliticaPrivacidadDocumento.Crear(_opciones, pedida, _minutosInactividad),
            Autenticado = User.Identity?.IsAuthenticated == true
        });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Descargar(CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Challenge();

        var historial = await _aceptaciones.HistorialAsync(userId, cancellationToken);
        if (historial.Count == 0)
            return RedirectToAction(nameof(Index));

        var texto = Constancia(historial);
        var nombre = "constancia-tratamiento-datos.txt";
        return File(Encoding.UTF8.GetBytes(texto), "text/plain; charset=utf-8", nombre);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Aceptar(CancellationToken cancellationToken)
    {
        if (await YaAceptoAsync(cancellationToken))
            return RedirectToAction("Index", "Home");

        ViewData["Title"] = "Aceptar política de datos";
        ViewData["Breadcrumb"] = "SIPITEX / Privacidad";
        return View(await ModeloAceptarAsync(cancellationToken));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aceptar(
        [Bind(nameof(AceptarPoliticaViewModel.AceptaPolitica))] AceptarPoliticaViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var aviso = await ModeloAceptarAsync(cancellationToken);
            aviso.AceptaPolitica = model.AceptaPolitica;
            return View(aviso);
        }

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

        var tipo = NormalizarTipo(model.Tipo);
        if (tipo is null)
        {
            TempData["Message"] = "Seleccione rectificación, supresión o revocatoria.";
            TempData["IsSuccess"] = false;
            return RedirectToAction("Profile", "Account");
        }

        var accion = AccionDe(tipo);
        var texto = RedactorDatosSensibles.Limpiar(model.Mensaje.Trim()) ?? "";
        var detalle = $"Tipo={tipo}; Estado={EstadoRecibida}; PlazoRespuesta={PlazoReclamo}";
        if (!string.IsNullOrWhiteSpace(texto))
            detalle += "; Detalle=" + texto;

        await _actividad.LogAsync(
            userId,
            accion,
            ActivityLogEntities.User,
            userId.ToString(),
            detalle,
            cancellationToken);

        await AvisarResponsableAsync(userId, tipo, texto, cancellationToken);

        TempData["Message"] =
            "La solicitud de " + Etiqueta(tipo) +
            " quedó registrada en la auditoría con estado recibida. " +
            "El plazo de respuesta es de máximo 15 días hábiles, prorrogables 8 más si le avisan los motivos.";
        TempData["IsSuccess"] = true;
        return RedirectToAction("Profile", "Account");
    }

    private async Task AvisarResponsableAsync(int userId, string tipo, string texto, CancellationToken cancellationToken)
    {
        var destino = _opciones.CorreoContacto?.Trim();
        if (string.IsNullOrWhiteSpace(destino))
            return;

        var user = await _cuentas.GetUserByIdAsync(userId, cancellationToken);
        var etiqueta = Etiqueta(tipo);
        var cuerpo = "Solicitud de " + etiqueta + " de datos personales.\n"
            + "Estado: " + EstadoRecibida + "\n"
            + "Plazo de respuesta: " + PlazoReclamo + "\n"
            + "Usuario: " + (user?.Nombre ?? "#" + userId) + "\n"
            + "Correo: " + (user?.Email ?? "") + "\n"
            + "Detalle: " + texto;

        try
        {
            await _correo.SendAsync(
                destino,
                _opciones.ResponsableNombre,
                "SIPITEX · solicitud de " + etiqueta + " de datos",
                cuerpo,
                cancellationToken);
        }
        catch
        {
            // La auditoría ya quedó. Un correo caído no debe perder la solicitud.
        }
    }

    private string Constancia(IReadOnlyList<AceptacionTratamientoDto> historial)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Constancia de aceptación de la política de tratamiento de datos — SIPITEX");
        sb.AppendLine("El historial conserva las aceptaciones anteriores.");
        sb.AppendLine();
        foreach (var fila in historial)
        {
            var ip = string.IsNullOrWhiteSpace(fila.DireccionIp) ? "no disponible" : fila.DireccionIp.Trim();
            sb.AppendLine($"Versión {fila.VersionPolitica} | {fila.AceptadaEnUtc:yyyy-MM-dd HH:mm} UTC | IP {ip}");
        }

        var vistas = new HashSet<string>(StringComparer.Ordinal);
        foreach (var fila in historial)
        {
            if (!vistas.Add(fila.VersionPolitica))
                continue;

            sb.AppendLine();
            sb.AppendLine(new string('-', 40));
            sb.AppendLine(PoliticaPrivacidadDocumento.Crear(_opciones, fila.VersionPolitica, _minutosInactividad).ATexto());
        }

        return sb.ToString();
    }

    private async Task<AceptarPoliticaViewModel> ModeloAceptarAsync(CancellationToken cancellationToken)
    {
        string? anterior = null;
        if (TryUserId(out var userId))
        {
            var ultima = await _aceptaciones.UltimaAsync(userId, cancellationToken);
            if (ultima is not null
                && !string.Equals(ultima.VersionPolitica, _opciones.VersionVigente(), StringComparison.Ordinal))
            {
                anterior = ultima.VersionPolitica;
            }
        }

        return new AceptarPoliticaViewModel
        {
            VersionVigente = _opciones.VersionVigente(),
            VersionAnterior = anterior,
            AceptaPolitica = false
        };
    }

    private static string? NormalizarTipo(string? tipo)
    {
        return (tipo ?? "").Trim().ToLowerInvariant() switch
        {
            "actualizacion" or "rectificacion" => "rectificacion",
            "supresion" => "supresion",
            "revocatoria" => "revocatoria",
            _ => null
        };
    }

    private static string AccionDe(string tipo) => tipo switch
    {
        "supresion" => ActivityLogActions.SolicitarSupresionDatos,
        "revocatoria" => ActivityLogActions.SolicitarRevocatoriaDatos,
        _ => ActivityLogActions.SolicitarActualizacionDatos
    };

    private static string Etiqueta(string tipo) => tipo switch
    {
        "supresion" => "supresión",
        "revocatoria" => "revocatoria",
        _ => "rectificación"
    };

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
