using System.Net.Mail;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Email;
using Sipitex.Web;
using Sipitex.Web.Helpers;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

[Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor},{UserRoles.EncargadoDeBodega}")]
public class ActasController : Controller
{
    private readonly IActaMovimientoService _actas;
    private readonly IProductionOrderService _orders;
    private readonly IStockMovementService _stock;
    private readonly IMaterialConsumptionService _consumos;
    private readonly ICurrentPlantaInventarioAccessor _plantaAccessor;
    private readonly IPlantaInventarioService _plantas;
    private readonly IEmailSender _correo;
    private readonly AppOptions _app;

    public ActasController(
        IActaMovimientoService actas,
        IProductionOrderService orders,
        IStockMovementService stock,
        IMaterialConsumptionService consumos,
        ICurrentPlantaInventarioAccessor plantaAccessor,
        IPlantaInventarioService plantas,
        IEmailSender correo,
        IOptions<AppOptions> app)
    {
        _actas = actas;
        _orders = orders;
        _stock = stock;
        _consumos = consumos;
        _plantaAccessor = plantaAccessor;
        _plantas = plantas;
        _correo = correo;
        _app = app.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Index(ActaEstado? estado, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Actas de ingreso y egreso";
        ViewData["Breadcrumb"] = "SIPITEX / Operación / Actas";
        var filter = await BuildFilterAsync(cancellationToken);
        var actas = await _actas.GetAllAsync(filter, cancellationToken);
        var visibles = estado is ActaEstado filtro
            ? actas.Where(a => a.Estado == filtro).ToList()
            : actas;
        return View(new ActasIndexViewModel
        {
            Actas = visibles,
            Estado = estado,
            FiltroSinResultados = estado is not null && visibles.Count == 0 && actas.Count > 0,
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? orderId, ActaOrigen? origen, CancellationToken cancellationToken)
    {
        var (_, role, name) = CurrentViewer();
        var form = new CreateActaForm
        {
            ProductionOrderId = orderId,
            Origen = origen ?? ActaOrigen.Manual,
            EntregaNombre = name ?? string.Empty,
            EntregaCargo = role ?? string.Empty
        };
        return View(await BuildCreateViewModel(form, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Form")] CreateActaForm form, CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var userId))
        {
            TempData["Message"] = "No se pudo identificar al usuario.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
            return View(await BuildCreateViewModel(form, cancellationToken));

        var result = await _actas.CreateAsync(new CreateActaDto(
            form.Tipo,
            form.Origen,
            form.EntregaNombre,
            form.EntregaCargo ?? string.Empty,
            form.EntregaConforme,
            form.RecibeNombre,
            form.RecibeCargo ?? string.Empty,
            form.RecibeConforme,
            userId,
            form.Observaciones,
            form.ProductionOrderId,
            form.EstadoOrigen,
            form.EstadoDestino,
            form.StockMovementIds,
            form.ConsumoIds,
            SignatureImage.FromDataUrl(form.EntregaFirmaDataUrl),
            SignatureImage.FromDataUrl(form.RecibeFirmaDataUrl),
            form.PlantaOrigenId,
            form.PlantaDestinoId), cancellationToken);

        if (result is { Success: true, Value: { } acta })
        {
            var mensaje = result.Message ?? $"Acta {acta.Numero} registrada.";
            var envioOk = true;
            if (!string.IsNullOrWhiteSpace(form.EnviarPdfCorreo))
            {
                var envio = await EnviarPdfAsync(acta, form.EnviarPdfCorreo, cancellationToken);
                envioOk = envio.Ok;
                mensaje = envio.Ok
                    ? $"{mensaje} El PDF se envió por correo."
                    : $"{mensaje} No se pudo enviar el PDF: {envio.Error}";
            }

            TempData["Message"] = mensaje;
            TempData["IsSuccess"] = envioOk;
            return RedirectToAction(nameof(Details), new { id = acta.Id, descargar = 1 });
        }

        AddActaError(result.Message);
        return View(await BuildCreateViewModel(form, cancellationToken));
    }

    private async Task<ActaCreateViewModel> BuildCreateViewModel(CreateActaForm form, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Nueva acta";
        ViewData["Breadcrumb"] = "SIPITEX / Operación / Actas / Nueva";
        var (userId, role, name) = CurrentViewer();
        var orders = await _orders.GetOrdersAsync(userId, role, name, cancellationToken);
        var selected = form.ProductionOrderId is int id && orders.Any(o => o.Id == id)
            ? id
            : orders.FirstOrDefault()?.Id;
        form.ProductionOrderId = selected;
        return new ActaCreateViewModel
        {
            Orders = orders,
            Movimientos = await _stock.GetHistoryAsync(null, null, null, cancellationToken),
            Consumos = selected is int oid
                ? await _consumos.GetByOrderAsync(oid, cancellationToken)
                : [],
            ActasPorStock = await _actas.GetActasPorMovimientoStockAsync(cancellationToken),
            ActasPorConsumo = await _actas.GetActasPorConsumoAsync(cancellationToken),
            PlantasActivas = (await _plantas.GetAllAsync(cancellationToken)).Where(p => p.Activo).OrderBy(p => p.Nombre).ToList(),
            Form = form
        };
    }

    private void AddActaError(string? message)
    {
        var text = string.IsNullOrWhiteSpace(message) ? "No se pudo registrar el acta." : message;
        if (text.Contains("quien entrega", StringComparison.OrdinalIgnoreCase)
            && text.Contains("quien recibe", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Form.EntregaNombre", "El nombre de quien entrega es obligatorio.");
            ModelState.AddModelError("Form.RecibeNombre", "El nombre de quien recibe es obligatorio.");
            return;
        }

        if (text.Contains("planta", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Form.PlantaOrigenId", text);
            return;
        }

        if (text.Contains("quien entrega", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Form.EntregaFirmaDataUrl", text);
            return;
        }

        if (text.Contains("quien recibe", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Form.RecibeFirmaDataUrl", text);
            return;
        }

        ModelState.AddModelError(string.Empty, text);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var filter = await BuildFilterAsync(cancellationToken);
        var acta = await _actas.GetByIdAsync(id, filter, cancellationToken);
        if (acta is null)
            return NotFound();
        ViewData["Title"] = acta.Numero;
        ViewData["Breadcrumb"] = $"SIPITEX / Operación / Actas / {acta.Numero}";
        return View(BuildDetailsViewModel(acta));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Firmar(int id, [Bind(Prefix = "Firmar")] FirmarActaForm form, CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var userId))
            return Challenge();

        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var result = await _actas.FirmarRecibeAsync(
            id,
            userId,
            role,
            form.Conforme,
            SignatureImage.FromDataUrl(form.FirmaDataUrl),
            cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Acta firmada." : "No se pudo firmar el acta.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = UserRoles.Administrador)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Anular(int id, [Bind(Prefix = "Anular")] AnularActaForm form, CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var userId))
            return Challenge();
        if (!ModelState.IsValid)
        {
            var filter = await BuildFilterAsync(cancellationToken);
            var acta = await _actas.GetByIdAsync(id, filter, cancellationToken);
            if (acta is null)
                return NotFound();
            var model = BuildDetailsViewModel(acta);
            model.Anular = form;
            return View(nameof(Details), model);
        }

        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var result = await _actas.AnularAsync(id, userId, role, form.Motivo, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Acta anulada." : "No se pudo anular el acta.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Details), new { id });
    }

    private ActaDetailsViewModel BuildDetailsViewModel(ActaMovimientoDto acta)
    {
        var (userId, role, _) = CurrentViewer();
        var admin = string.Equals(role, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase);
        var pendiente = acta.Estado == ActaEstado.PendienteFirma;
        return new ActaDetailsViewModel
        {
            Acta = acta,
            PuedeFirmar = pendiente && (admin || (userId is int uid && uid == acta.CreadoPorUserId)),
            PuedeAnular = admin && acta.Estado != ActaEstado.Anulada,
            PuedeEnviarCorreo = admin || (userId is int creador && creador == acta.CreadoPorUserId),
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        };
    }

    [HttpGet]
    public async Task<IActionResult> Pdf(int id, CancellationToken cancellationToken)
    {
        var filter = await BuildFilterAsync(cancellationToken);
        var visible = await _actas.GetByIdAsync(id, filter, cancellationToken);
        if (visible is null)
            return NotFound();
        var result = await _actas.ExportPdfAsync(id, cancellationToken, UrlVerificacion(visible));
        if (!result.Success || result.Value is null)
            return NotFound();
        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarCorreo(int id, [Bind(Prefix = "Enviar")] EnviarActaPdfForm form, CancellationToken cancellationToken)
    {
        var filter = await BuildFilterAsync(cancellationToken);
        var acta = await _actas.GetByIdAsync(id, filter, cancellationToken);
        if (acta is null)
            return NotFound();
        if (!PuedeEnviar(acta))
            return Forbid();

        if (!ModelState.IsValid)
        {
            var model = BuildDetailsViewModel(acta);
            model.Enviar = form;
            return View(nameof(Details), model);
        }

        var envio = await EnviarPdfAsync(acta, form.Correo, cancellationToken);
        TempData["Message"] = envio.Ok ? "El PDF se envió por correo." : envio.Error;
        TempData["IsSuccess"] = envio.Ok;
        return RedirectToAction(nameof(Details), new { id });
    }

    private bool PuedeEnviar(ActaMovimientoDto acta)
    {
        var (userId, role, _) = CurrentViewer();
        var admin = string.Equals(role, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase);
        return admin || (userId is int uid && uid == acta.CreadoPorUserId);
    }

    private async Task<(bool Ok, string? Error)> EnviarPdfAsync(
        ActaMovimientoDto acta,
        string correo,
        CancellationToken cancellationToken)
    {
        var destino = (correo ?? string.Empty).Trim();
        if (!MailAddress.TryCreate(destino, out _))
            return (false, "Indique un correo válido.");

        var url = UrlVerificacion(acta);
        var pdf = await _actas.ExportPdfAsync(acta.Id, cancellationToken, url);
        if (!pdf.Success || pdf.Value is null)
            return (false, "No se pudo generar el PDF.");

        var archivo = pdf.Value;
        var cuerpo =
            $"Acta {acta.Numero} ({DisplayHelper.StatusText(acta.Tipo)}) del {acta.FechaUtc:yyyy-MM-dd HH:mm} UTC.\n" +
            $"Verifique el documento: {url}";
        try
        {
            await _correo.SendAsync(
                destino,
                acta.RecibeNombre,
                $"SIPITEX · {acta.Numero}",
                cuerpo,
                [new EmailAttachment(archivo.FileName, archivo.ContentType, archivo.Content)],
                cancellationToken);
            return (true, null);
        }
        catch (EmailDeliveryException ex)
        {
            return (false, ex.Message);
        }
    }

    private string UrlVerificacion(ActaMovimientoDto acta)
    {
        var codigo = CodigoDe(acta);
        var baseUrl = string.IsNullOrWhiteSpace(_app.BaseUrl)
            ? $"{Request.Scheme}://{Request.Host.Value}".TrimEnd('/')
            : _app.BaseUrl.Trim().TrimEnd('/');
        return $"{baseUrl}/Verificar/Acta?numero={Uri.EscapeDataString(acta.Numero)}&codigo={Uri.EscapeDataString(codigo)}";
    }

    private string CodigoDe(ActaMovimientoDto acta)
    {
        var pdf = HttpContext.RequestServices.GetRequiredService<IActaPdfService>();
        return pdf.CodigoVerificacion(acta);
    }

    private async Task<ActaViewerFilter?> BuildFilterAsync(CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        if (userId is not int uid || string.IsNullOrWhiteSpace(role))
            return null;
        if (string.Equals(role, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase))
            return new ActaViewerFilter(role, uid, [], []);

        IReadOnlyList<int> plantas = _plantaAccessor.PlantaInventarioIds ?? [];
        IReadOnlyCollection<int> orders = [];
        if (string.Equals(role, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase))
        {
            var list = await _orders.GetOrdersAsync(uid, role, name, cancellationToken);
            orders = list.Select(o => o.Id).ToHashSet();
        }

        return new ActaViewerFilter(role, uid, plantas, orders);
    }

    private (int? UserId, string? Role, string? Name) CurrentViewer()
    {
        int? userId = null;
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            userId = id;
        return (userId, User.FindFirstValue(ClaimTypes.Role), User.FindFirstValue(ClaimTypes.Name));
    }

    private bool TryGetActorUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;
}
