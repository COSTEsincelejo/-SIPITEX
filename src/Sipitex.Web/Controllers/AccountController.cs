using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly IUserAccountService _userAccountService;

    public AccountController(IUserAccountService userAccountService) => _userAccountService = userAccountService;

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userAccountService.AuthenticateAsync(model.Email, model.Password, cancellationToken);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Nombre),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Rol)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });

        return RedirectToAction("Index", "Inventario");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpGet]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
    {
        var users = await _userAccountService.GetUsersAsync(cancellationToken);
        return View(users);
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpGet]
    public async Task<IActionResult> CreateUser(CancellationToken cancellationToken)
    {
        ViewBag.Fichas = await GetFichasAsync(cancellationToken);
        return View(new UserEditViewModel());
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(UserEditViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { ViewBag.Fichas = await GetFichasAsync(cancellationToken); return View(model); }

        var permisos = model.PermisosExtendidos?.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList() ?? [];
        var result = await _userAccountService.CreateUserAsync(model.Nombre, model.Email, model.Password, model.Rol, model.FichaAsignadaId, permisos, cancellationToken);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Message ?? "Error"); ViewBag.Fichas = await GetFichasAsync(cancellationToken); return View(model); }

        TempData["Message"] = result.Message;
        return RedirectToAction(nameof(Users));
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpGet]
    public async Task<IActionResult> EditUser(int id, CancellationToken cancellationToken)
    {
        var user = await _userAccountService.GetUserByIdAsync(id, cancellationToken);
        if (user is null) return NotFound();
        ViewBag.Fichas = await GetFichasAsync(cancellationToken);
        return View(new UserEditViewModel
        {
            Id = user.Id,
            Nombre = user.Nombre,
            Email = user.Email,
            Rol = user.Rol,
            FichaAsignadaId = user.FichaAsignadaId,
            PermisosExtendidos = string.Join(", ", user.PermisosExtendidos),
            IsActive = user.IsActive
        });
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(UserEditViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { ViewBag.Fichas = await GetFichasAsync(cancellationToken); return View(model); }

        var permisos = model.PermisosExtendidos?.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList() ?? [];
        var result = await _userAccountService.UpdateUserAsync(model.Id, model.Nombre, model.Email, model.Password, model.Rol, model.FichaAsignadaId, permisos, model.IsActive, cancellationToken);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Message ?? "Error"); ViewBag.Fichas = await GetFichasAsync(cancellationToken); return View(model); }

        TempData["Message"] = result.Message;
        return RedirectToAction(nameof(Users));
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(int id, bool isActive, CancellationToken cancellationToken)
    {
        var result = await _userAccountService.ToggleUserStatusAsync(id, isActive, cancellationToken);
        TempData["Message"] = result.Message;
        return RedirectToAction(nameof(Users));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() => View();

    private async Task<IReadOnlyList<Ficha>> GetFichasAsync(CancellationToken cancellationToken)
    {
        var fichaService = HttpContext.RequestServices.GetService<IFichaService>();
        var fichas = await fichaService!.GetFichasAsync(cancellationToken);
        return fichas.Select(f => new Ficha { Id = f.Id, FichaCode = f.FichaCode, ProcessName = f.ProcessName }).ToList();
    }
}
