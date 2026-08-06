using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Fichas del SENA: crear, filtrar y registrar producción por sesión
[Authorize]
public class FichasController : Controller
{
    // CRUD de fichas y sesiones de producción
    private readonly IFichaService _fichaService;
    // Para llenar el combo de órdenes en los forms
    private readonly IProductionOrderService _orderService;

    public FichasController(IFichaService fichaService, IProductionOrderService orderService)
    {
        _fichaService = fichaService;
        _orderService = orderService;
    }

    // Listado con filtros opcionales por código, instructor y turno
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpGet]
    public async Task<IActionResult> Index(
        string? fichaCode,
        string? instructor,
        string? turno,
        CancellationToken cancellationToken) =>
        // Armo el VM con filtros y devuelvo la vista
        View(await BuildViewModel(fichaCode, instructor, turno, cancellationToken));

    // Crea una ficha nueva y la puede ligar a una orden
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFicha([Bind(Prefix = "CreateFicha")] CreateFichaForm form, CancellationToken cancellationToken)
    {
        // Saco id, rol y nombre del usuario logueado
        var (userId, role, _) = CurrentViewer();
        // Si es instructor, la ficha queda asociada a su usuario automáticamente
        int? instructorUserId = string.Equals(role, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase)
            ? userId
            : null;

        // El servicio valida código único, exclusividad orden/texto y guarda en BD
        var result = await _fichaService.CreateFichaAsync(
            new CreateFichaDto(
                form.FichaCode,
                form.ProcessName,
                form.InstructorName,
                form.Turno,
                form.ProductionOrderId is > 0 ? form.ProductionOrderId : null,
                form.AssignedOrderText),
            instructorUserId,
            cancellationToken);

        // Mensaje para mostrar después del redirect
        TempData["Message"] = result.Message ?? (result.Success ? "Ficha registrada." : "Error al registrar ficha.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    // Registra producción de una sesión con orden, ficha y unidades
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register([Bind(Prefix = "Register")] RegisterProductionForm form, CancellationToken cancellationToken)
    {
        // Quién está registrando (para permisos en el servicio)
        var (userId, role, name) = CurrentViewer();
        // Guarda sesión y actualiza avance de la orden
        var result = await _fichaService.RegisterSessionAsync(
            new RegisterProductionDto(form.ProductionOrderId, form.FichaId, form.Units, form.Observations),
            userId,
            role,
            name,
            cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Sesión registrada." : "Error al registrar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    // Atajo desde la tabla de fichas sin abrir el formulario completo
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickRegister(int fichaId, int units, string? observations, CancellationToken cancellationToken)
    {
        // Datos del usuario para validar que el instructor solo toque sus fichas
        var (userId, role, name) = CurrentViewer();
        // Usa la orden que ya tiene la ficha asignada
        var result = await _fichaService.QuickRegisterAsync(fichaId, units, observations, userId, role, name, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Registro exitoso." : "Error al registrar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    // Arma todo lo que muestra la vista Index (fichas, sesiones, forms, filtros)
    private async Task<FichasIndexViewModel> BuildViewModel(
        string? fichaCode,
        string? instructor,
        string? turno,
        CancellationToken cancellationToken)
    {
        // Usuario actual para filtrar por rol
        var (userId, role, name) = CurrentViewer();
        // Todas las órdenes para los combos
        var orders = await _orderService.GetOrdersAsync(cancellationToken);
        // El servicio ya filtra por rol (instructor solo ve lo suyo)
        var fichas = (await _fichaService.GetFichasAsync(userId, role, name, cancellationToken)).AsEnumerable();
        // Últimas sesiones registradas
        var sessions = (await _fichaService.GetRecentSessionsAsync(userId, role, name, cancellationToken)).AsEnumerable();

        // Filtros de la barra (código, instructor, turno) — se aplican en memoria
        if (!string.IsNullOrWhiteSpace(fichaCode))
        {
            // Fichas cuyo código contiene el texto buscado
            fichas = fichas.Where(f => f.FichaCode.Contains(fichaCode, StringComparison.OrdinalIgnoreCase));
            // Lo mismo en sesiones
            sessions = sessions.Where(s => s.FichaCode.Contains(fichaCode, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(instructor))
        {
            // Filtro por nombre de instructor en fichas
            fichas = fichas.Where(f => f.InstructorName.Contains(instructor, StringComparison.OrdinalIgnoreCase));
            sessions = sessions.Where(s => s.InstructorName.Contains(instructor, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(turno))
        {
            // Turno exacto (mañana/tarde/noche)
            fichas = fichas.Where(f => string.Equals(f.Turno, turno, StringComparison.OrdinalIgnoreCase));
            sessions = sessions.Where(s => string.Equals(s.Turno, turno, StringComparison.OrdinalIgnoreCase));
        }

        // Materializo porque ya terminé de filtrar
        var fichaList = fichas.ToList();
        var sessionList = sessions.ToList();

        // Form de crear ficha con valores por defecto
        // Si es instructor, dejo su nombre ya puesto en el form de crear
        var create = new CreateFichaForm();
        if (User.IsInRole(UserRoles.Instructor) && !string.IsNullOrWhiteSpace(name))
            create.InstructorName = name!;

        return new FichasIndexViewModel
        {
            // Tabla de fichas filtradas
            Fichas = fichaList,
            // Para dropdown de órdenes
            Orders = orders,
            // Historial de sesiones
            Sessions = sessionList,
            // La vista muestra u oculta cosas según sea admin
            IsAdministrator = User.IsInRole(UserRoles.Administrador),
            CreateFicha = create,
            // Form de registro con primera orden y ficha preseleccionadas
            Register = new RegisterProductionForm
            {
                ProductionOrderId = orders.FirstOrDefault()?.Id ?? 0,
                FichaId = fichaList.FirstOrDefault()?.Id ?? 0
            },
            // Valores actuales de los filtros (para que queden en los inputs)
            FichaCodeFilter = fichaCode,
            InstructorFilter = instructor,
            TurnoFilter = turno,
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        };
    }

    // Datos del usuario logueado que el servicio usa para permisos
    private (int? UserId, string? Role, string? Name) CurrentViewer()
    {
        // Empiezo sin id por si el claim no viene
        int? userId = null;
        // El id del usuario está en NameIdentifier
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            userId = id;

        // Rol para saber si es instructor o admin
        var role = User.FindFirstValue(ClaimTypes.Role);
        // Nombre para fichas legacy sin FK
        var name = User.FindFirstValue(ClaimTypes.Name);
        return (userId, role, name);
    }
}
