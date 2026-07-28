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
    public const string PhotoClaimType = "photo";

    private static readonly HashSet<string> AllowedPhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private const long MaxPhotoBytes = 2 * 1024 * 1024;

    private readonly IUserAccountService _userAccountService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly IWebHostEnvironment _environment;

    public AccountController(
        IUserAccountService userAccountService,
        IPasswordResetService passwordResetService,
        IWebHostEnvironment environment)
    {
        _userAccountService = userAccountService;
        _passwordResetService = passwordResetService;
        _environment = environment;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        ViewBag.SuccessMessage = TempData["SuccessMessage"] as string;
        return View(new LoginViewModel());
    }

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

        await SignInUserAsync(user);
        return RedirectToAction("Index", "Inventario");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        var publicBaseUrl = $"{Request.Scheme}://{Request.Host}";
        await _passwordResetService.RequestResetAsync(model.Email, publicBaseUrl, cancellationToken);
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ResetPassword(string? token, string? email)
    {
        return View(new ResetPasswordViewModel
        {
            Token = token ?? string.Empty,
            Email = email ?? string.Empty
        });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        if (!string.Equals(model.NewPassword, model.ConfirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "Las contraseñas no coinciden.");
            return View(model);
        }

        var result = await _passwordResetService.ResetPasswordAsync(
            model.Email, model.Token, model.NewPassword, cancellationToken);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Enlace inválido o expirado.");
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message ?? "Contraseña actualizada. Ya puede iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null) return Challenge();

        return View(ToProfileViewModel(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model, IFormFile? photo, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null) return Challenge();

        model.Id = user.Id;
        model.Rol = user.Rol;
        model.PhotoPath = user.PhotoPath;

        if (!ModelState.IsValid)
            return View(model);

        string? newPhotoPath = null;
        string? previousPhotoPath = user.PhotoPath;

        if (photo is { Length: > 0 })
        {
            var saveResult = await SaveProfilePhotoAsync(user.Id, photo, cancellationToken);
            if (!saveResult.Success)
            {
                ModelState.AddModelError(string.Empty, saveResult.Error!);
                return View(model);
            }
            newPhotoPath = saveResult.Path;
            model.RemovePhoto = false;
        }

        var result = await _userAccountService.UpdateProfileAsync(
            user.Id,
            model.Nombre,
            model.Email,
            model.FuncionDescripcion,
            model.NewPassword,
            newPhotoPath,
            model.RemovePhoto,
            cancellationToken);

        if (!result.Success)
        {
            if (!string.IsNullOrWhiteSpace(newPhotoPath))
                DeleteProfilePhotoFile(newPhotoPath);
            ModelState.AddModelError(string.Empty, result.Message ?? "No se pudo actualizar el perfil.");
            return View(model);
        }

        if ((model.RemovePhoto || !string.IsNullOrWhiteSpace(newPhotoPath)) &&
            !string.IsNullOrWhiteSpace(previousPhotoPath) &&
            !string.Equals(previousPhotoPath, newPhotoPath, StringComparison.OrdinalIgnoreCase))
        {
            DeleteProfilePhotoFile(previousPhotoPath);
        }

        var refreshed = await _userAccountService.GetUserByIdAsync(user.Id, cancellationToken);
        if (refreshed is not null)
            await SignInUserAsync(refreshed);

        TempData["Message"] = result.Message;
        return RedirectToAction(nameof(Profile));
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
        return View(new UserEditViewModel { Rol = UserRoles.Instructor });
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(UserEditViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { ViewBag.Fichas = await GetFichasAsync(cancellationToken); return View(model); }

        var permisos = model.SelectedPermissions ?? [];
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
            SelectedPermissions = ExtendedPermissions.Parse(user.PermisosExtendidos).ToList(),
            IsActive = user.IsActive
        });
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(UserEditViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { ViewBag.Fichas = await GetFichasAsync(cancellationToken); return View(model); }

        var permisos = model.SelectedPermissions ?? [];
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

    private async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idValue, out var userId)) return null;
        return await _userAccountService.GetUserByIdAsync(userId, cancellationToken);
    }

    private async Task SignInUserAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Nombre),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Rol)
        };

        if (!string.IsNullOrWhiteSpace(user.PhotoPath))
            claims.Add(new Claim(PhotoClaimType, user.PhotoPath));

        foreach (var permiso in ExtendedPermissions.Parse(user.PermisosExtendidos))
            claims.Add(new Claim(ExtendedPermissions.ClaimType, permiso));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
    }

    private static ProfileViewModel ToProfileViewModel(User user) => new()
    {
        Id = user.Id,
        Nombre = user.Nombre,
        Email = user.Email,
        Rol = user.Rol,
        PhotoPath = user.PhotoPath,
        FuncionDescripcion = user.FuncionDescripcion
    };

    private async Task<(bool Success, string? Path, string? Error)> SaveProfilePhotoAsync(
        int userId,
        IFormFile photo,
        CancellationToken cancellationToken)
    {
        if (photo.Length > MaxPhotoBytes)
            return (false, null, "La foto no puede superar 2 MB.");

        var extension = Path.GetExtension(photo.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedPhotoExtensions.Contains(extension))
            return (false, null, "Formato no válido. Use JPG, PNG o WEBP.");

        var contentType = photo.ContentType?.ToLowerInvariant() ?? string.Empty;
        if (!contentType.StartsWith("image/", StringComparison.Ordinal))
            return (false, null, "El archivo debe ser una imagen.");

        var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{userId}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var physicalPath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = System.IO.File.Create(physicalPath))
            await photo.CopyToAsync(stream, cancellationToken);

        return (true, $"/uploads/profiles/{fileName}", null);
    }

    private void DeleteProfilePhotoFile(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        var normalized = relativePath.TrimStart('~').TrimStart('/');
        if (!normalized.StartsWith("uploads/profiles/", StringComparison.OrdinalIgnoreCase))
            return;

        var physicalPath = Path.Combine(_environment.WebRootPath, normalized.Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(physicalPath))
            System.IO.File.Delete(physicalPath);
    }

    private async Task<IReadOnlyList<Ficha>> GetFichasAsync(CancellationToken cancellationToken)
    {
        var fichaService = HttpContext.RequestServices.GetService<IFichaService>();
        var fichas = await fichaService!.GetFichasAsync(cancellationToken: cancellationToken);
        return fichas.Select(f => new Ficha { Id = f.Id, FichaCode = f.FichaCode, ProcessName = f.ProcessName }).ToList();
    }
}
