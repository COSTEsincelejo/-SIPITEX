// Claims, cookies y autorización de ASP.NET
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
// Servicios de usuarios y reset de contraseña
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Login, perfil, recuperar contraseña y el CRUD de usuarios (solo admin)
[Authorize]
public class AccountController : Controller
{
    // Lo uso en los claims para saber qué foto mostrar en el layout
    public const string PhotoClaimType = "photo";

    // Extensiones que aceptamos para la foto de perfil
    private static readonly HashSet<string> AllowedPhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private const long MaxPhotoBytes = 2 * 1024 * 1024; // 2 MB, para no llenar el servidor

    private readonly IUserAccountService _userAccountService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly IFuncionalidadesReportService _funcionalidadesReportService;
    private readonly IActivityLogService _activityLog;
    private readonly IWebHostEnvironment _environment;

    // Inyecto los servicios que usa todo el controller
    public AccountController(
        IUserAccountService userAccountService,
        IPasswordResetService passwordResetService,
        IFuncionalidadesReportService funcionalidadesReportService,
        IActivityLogService activityLog,
        IWebHostEnvironment environment)
    {
        _userAccountService = userAccountService;
        _passwordResetService = passwordResetService;
        _funcionalidadesReportService = funcionalidadesReportService;
        _activityLog = activityLog;
        _environment = environment;
    }

    // Pantalla de login (GET)
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        // Si venían de reset password exitoso, mostramos el mensaje verde
        ViewBag.SuccessMessage = TempData["SuccessMessage"] as string;
        // ViewModel vacío para email y contraseña
        return View(new LoginViewModel());
    }

    // Valida credenciales y deja al usuario logueado
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        // Si el form viene mal, vuelvo a la vista con errores
        if (!ModelState.IsValid) return View(model);

        // Pregunto al servicio si email y clave cuadran
        var user = await _userAccountService.AuthenticateAsync(model.Email, model.Password, cancellationToken);
        // Usuario no existe, inactivo o contraseña mala
        if (user is null)
        {
            // Error genérico para no decir si falló el email o la clave
            ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
            return View(model);
        }

        // Cookie lista con rol, foto y permisos
        await SignInUserAsync(user);
        // Instructor no tiene Inventario general; Admin/Bodeguero van al stock
        if (string.Equals(user.Rol, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("Index", "Ordenes");
        return RedirectToAction("Index", "Inventario");
    }

    // Formulario "olvidé mi contraseña"
    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    // Manda el correo con el link de reset
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        // El servicio arma el link del correo con esta URL base
        var publicBaseUrl = $"{Request.Scheme}://{Request.Host}";
        await _passwordResetService.RequestResetAsync(model.Email, publicBaseUrl, cancellationToken);
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    // Vista de "revisa tu correo"
    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    // Abre el form de nueva contraseña con token y email de la URL
    [AllowAnonymous]
    [HttpGet]
    public IActionResult ResetPassword(string? token, string? email)
    {
        // Token y email vienen en la URL del correo
        return View(new ResetPasswordViewModel
        {
            Token = token ?? string.Empty,
            Email = email ?? string.Empty
        });
    }

    // Guarda la contraseña nueva si el token sigue válido
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        // Validación extra porque ConfirmPassword a veces no alcanza con DataAnnotations
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

        // Mensaje para el login después del redirect
        TempData["SuccessMessage"] = result.Message ?? "Contraseña actualizada. Ya puede iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    // Cierra sesión y borra la cookie
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // Muestra el perfil del usuario logueado
    [HttpGet]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        // Si no hay usuario en BD, que ASP.NET pida login otra vez
        if (user is null) return Challenge();

        return View(ToProfileViewModel(user));
    }

    // Actualiza datos del perfil y opcionalmente la foto
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model, IFormFile? photo, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null) return Challenge();

        // Relleno campos que no vienen del form pero sí los necesito al guardar
        model.Id = user.Id;
        model.Rol = user.Rol;
        model.PhotoPath = user.PhotoPath;

        if (!ModelState.IsValid)
            return View(model);

        string? newPhotoPath = null;
        string? previousPhotoPath = user.PhotoPath;

        // Si subieron archivo, lo guardo en disco primero
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
            // Si falló el update, borro la foto nueva para no dejar basura en uploads
            if (!string.IsNullOrWhiteSpace(newPhotoPath))
                DeleteProfilePhotoFile(newPhotoPath);
            ModelState.AddModelError(string.Empty, result.Message ?? "No se pudo actualizar el perfil.");
            return View(model);
        }

        // Solo borro la foto vieja si realmente cambió o la quitaron
        if ((model.RemovePhoto || !string.IsNullOrWhiteSpace(newPhotoPath)) &&
            !string.IsNullOrWhiteSpace(previousPhotoPath) &&
            !string.Equals(previousPhotoPath, newPhotoPath, StringComparison.OrdinalIgnoreCase))
        {
            DeleteProfilePhotoFile(previousPhotoPath);
        }

        // Refresco la cookie porque el nombre/foto/permisos pueden haber cambiado
        var refreshed = await _userAccountService.GetUserByIdAsync(user.Id, cancellationToken);
        if (refreshed is not null)
            await SignInUserAsync(refreshed);

        TempData["Message"] = result.Message;
        return RedirectToAction(nameof(Profile));
    }

    // Listado de usuarios (solo admin)
    [Authorize(Roles = UserRoles.Administrador)]
    [HttpGet]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
    {
        // Traigo todos los usuarios de la BD
        var users = await _userAccountService.GetUsersAsync(cancellationToken);
        // La vista muestra la tabla con nombre, rol, activo, etc.
        return View(users);
    }

    // Form vacío para crear usuario con combo de fichas
    [Authorize(Roles = UserRoles.Administrador)]
    [HttpGet]
    public async Task<IActionResult> CreateUser(CancellationToken cancellationToken)
    {
        // Paso las fichas para el dropdown de asignación
        ViewBag.Fichas = await GetFichasAsync(cancellationToken);
        // Por defecto el rol nuevo es Instructor
        return View(new UserEditViewModel { Rol = UserRoles.Instructor });
    }

    // Crea el usuario en BD con rol y permisos
    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(UserEditViewModel model, CancellationToken cancellationToken)
    {
        // Validación del lado del servidor (DataAnnotations)
        if (!ModelState.IsValid) { ViewBag.Fichas = await GetFichasAsync(cancellationToken); return View(model); }

        // Lista de permisos extendidos marcados en el form
        var permisos = model.SelectedPermissions ?? [];
        // El servicio hashea la clave y guarda en BD
        var result = await _userAccountService.CreateUserAsync(model.Nombre, model.Email, model.Password, model.Rol, model.FichaAsignadaId, permisos, cancellationToken);
        // Si falló (correo duplicado, rol inválido, etc.) me quedo en el form
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Message ?? "Error"); ViewBag.Fichas = await GetFichasAsync(cancellationToken); return View(model); }

        if (TryGetActorUserId(out var actorId))
        {
            await _activityLog.LogAsync(
                actorId,
                "CreateUser",
                "User",
                entityId: model.Email.Trim().ToLowerInvariant(),
                details: $"Nombre={model.Nombre.Trim()}; Rol={model.Rol}",
                cancellationToken);
        }

        // Mensaje verde en el listado
        TempData["Message"] = result.Message;
        return RedirectToAction(nameof(Users));
    }

    // Carga un usuario para editarlo
    [Authorize(Roles = UserRoles.Administrador)]
    [HttpGet]
    public async Task<IActionResult> EditUser(int id, CancellationToken cancellationToken)
    {
        // Busco el usuario por id de la URL
        var user = await _userAccountService.GetUserByIdAsync(id, cancellationToken);
        // 404 si no existe
        if (user is null) return NotFound();
        // Fichas para el combo de asignación
        ViewBag.Fichas = await GetFichasAsync(cancellationToken);
        // Armo el view model con lo que trae la BD
        return View(new UserEditViewModel
        {
            Id = user.Id,
            Nombre = user.Nombre,
            Email = user.Email,
            Rol = user.Rol,
            FichaAsignadaId = user.FichaAsignadaId,
            // Deserializo los permisos guardados como string
            SelectedPermissions = ExtendedPermissions.Parse(user.PermisosExtendidos).ToList(),
            IsActive = user.IsActive
        });
    }

    // Guarda cambios del usuario (rol, ficha, activo, etc.)
    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(UserEditViewModel model, CancellationToken cancellationToken)
    {
        // Reviso campos obligatorios del form
        if (!ModelState.IsValid) { ViewBag.Fichas = await GetFichasAsync(cancellationToken); return View(model); }

        // Permisos que el admin marcó en los checkboxes
        var permisos = model.SelectedPermissions ?? [];
        // Update en BD; contraseña es opcional si viene vacía
        var result = await _userAccountService.UpdateUserAsync(model.Id, model.Nombre, model.Email, model.Password, model.Rol, model.FichaAsignadaId, permisos, model.IsActive, cancellationToken);
        // Error de negocio (ej. no bajar rol al admin principal)
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Message ?? "Error"); ViewBag.Fichas = await GetFichasAsync(cancellationToken); return View(model); }

        TempData["Message"] = result.Message;
        return RedirectToAction(nameof(Users));
    }

    // Activar/desactivar sin borrar el usuario de la BD
    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(int id, bool isActive, CancellationToken cancellationToken)
    {
        var result = await _userAccountService.ToggleUserStatusAsync(id, isActive, cancellationToken);
        if (result.Success && TryGetActorUserId(out var actorId))
        {
            await _activityLog.LogAsync(
                actorId,
                "ToggleUserStatus",
                "User",
                entityId: id.ToString(),
                details: isActive ? "IsActive=true" : "IsActive=false",
                cancellationToken);
        }

        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Users));
    }

    // Gap #1: hard delete con confirmación; bloquea si hay dependencias / self / último admin
    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) || actorId <= 0)
        {
            TempData["Message"] = "Sesión no válida.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Users));
        }

        var target = await _userAccountService.GetUserByIdAsync(id, cancellationToken);
        var result = await _userAccountService.DeleteUserAsync(id, actorId, cancellationToken);
        if (result.Success)
        {
            await _activityLog.LogAsync(
                actorId,
                "DeleteUser",
                "User",
                entityId: id.ToString(),
                details: target is null
                    ? null
                    : $"Nombre={target.Nombre}; Email={target.Email}; Rol={target.Rol}",
                cancellationToken);
        }

        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Users));
    }

    // Descarga Word con el catálogo de funcionalidades del sistema
    [Authorize(Roles = UserRoles.Administrador)]
    [HttpGet]
    public IActionResult DescargarFuncionalidades()
    {
        var file = _funcionalidadesReportService.GenerateDocx();
        return File(file.Content, file.ContentType, file.FileName);
    }

    // Vista cuando el usuario no tiene permiso para entrar
    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() => View();

    // Saco el usuario de la cookie y lo busco en BD (más confiable que solo leer claims)
    private async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idValue, out var userId)) return null;
        return await _userAccountService.GetUserByIdAsync(userId, cancellationToken);
    }

    private bool TryGetActorUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;

    // Armo la cookie de autenticación con rol, foto y permisos extendidos
    private async Task SignInUserAsync(User user)
    {
        // Claims básicos que usa el layout y la autorización
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Nombre),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Rol)
        };

        if (!string.IsNullOrWhiteSpace(user.PhotoPath))
            claims.Add(new Claim(PhotoClaimType, user.PhotoPath));

        // Cada permiso extra va como claim aparte
        foreach (var permiso in ExtendedPermissions.Parse(user.PermisosExtendidos))
            claims.Add(new Claim(ExtendedPermissions.ClaimType, permiso));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
    }

    // Pasa entidad User al modelo de la vista de perfil
    private static ProfileViewModel ToProfileViewModel(User user) => new()
    {
        Id = user.Id,
        Nombre = user.Nombre,
        Email = user.Email,
        Rol = user.Rol,
        PhotoPath = user.PhotoPath,
        FuncionDescripcion = user.FuncionDescripcion
    };

    // Guarda la imagen en wwwroot/uploads/profiles con nombre único
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

        // Ruta relativa para guardar en BD y mostrar en el HTML
        return (true, $"/uploads/profiles/{fileName}", null);
    }

    // Por seguridad solo borro archivos dentro de uploads/profiles
    private void DeleteProfilePhotoFile(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        var normalized = relativePath.TrimStart('~').TrimStart('/');
        // Evito path traversal fuera de la carpeta de perfiles
        if (!normalized.StartsWith("uploads/profiles/", StringComparison.OrdinalIgnoreCase))
            return;

        var physicalPath = Path.Combine(_environment.WebRootPath, normalized.Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(physicalPath))
            System.IO.File.Delete(physicalPath);
    }

    // Para el combo de ficha al crear/editar usuario
    private async Task<IReadOnlyList<Ficha>> GetFichasAsync(CancellationToken cancellationToken)
    {
        var fichaService = HttpContext.RequestServices.GetService<IFichaService>();
        var fichas = await fichaService!.GetFichasAsync(cancellationToken: cancellationToken);
        // Solo necesito id, código y proceso para el dropdown
        return fichas.Select(f => new Ficha { Id = f.Id, FichaCode = f.FichaCode, ProcessName = f.ProcessName }).ToList();
    }
}
