using FinanzasPersonales.Data;
using FinanzasPersonales.Models;
using FinanzasPersonales.Services;
using FinanzasPersonales.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanzasPersonales.Controllers;

/// <summary>Inicio de sesión, registro, perfil y cambio de contraseña.</summary>
public class CuentaController(FinanzasContext db, CuentaService cuentas) : Controller
{
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var usuario = await cuentas.ValidarCredencialesAsync(vm.Email, vm.Password);
        if (usuario is null)
        {
            ModelState.AddModelError("", "Correo o contraseña incorrectos, o la cuenta está inactiva.");
            return View(vm);
        }

        await CuentaService.IniciarSesionAsync(HttpContext, usuario, vm.Recordarme);
        return Url.IsLocalUrl(vm.ReturnUrl) ? LocalRedirect(vm.ReturnUrl) : RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult Registro()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new RegistroViewModel());
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroViewModel vm)
    {
        var email = vm.Email.Trim().ToLower();
        var cedula = DocumentoIdentidad.Limpiar(vm.Cedula);
        if (await db.Usuarios.AnyAsync(u => u.Email == email))
            ModelState.AddModelError(nameof(vm.Email), "Ya existe una cuenta con ese correo.");
        if (await db.Usuarios.AnyAsync(u => u.Cedula == cedula))
            ModelState.AddModelError(nameof(vm.Cedula), "Ya existe una cuenta con esa cédula / RNC.");
        if (!ModelState.IsValid) return View(vm);

        // Las cuentas creadas por registro siempre tienen rol Usuario.
        var usuario = new Usuario
        {
            Nombre = vm.Nombre.Trim(),
            Email = email,
            Cedula = cedula,
            TipoPersona = vm.TipoPersona,
            LimiteEgresos = vm.LimiteEgresos,
            DiaCorte = vm.DiaCorte,
            Rol = Rol.Usuario
        };
        cuentas.EstablecerPassword(usuario, vm.Password);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        await CuentaService.IniciarSesionAsync(HttpContext, usuario, recordar: false);
        TempData["Ok"] = $"¡Bienvenido, {usuario.Nombre}! Tu cuenta fue creada.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    public async Task<IActionResult> Perfil()
    {
        var u = await db.Usuarios.FindAsync(User.UsuarioId());
        if (u is null) return await CerrarSesionInvalida();
        return View(new PerfilViewModel
        {
            Nombre = u.Nombre, Email = u.Email, LimiteEgresos = u.LimiteEgresos, DiaCorte = u.DiaCorte,
            Cedula = u.Cedula, TipoPersona = u.TipoPersona, Rol = u.Rol
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Perfil(PerfilViewModel vm)
    {
        var u = await db.Usuarios.FindAsync(User.UsuarioId());
        if (u is null) return await CerrarSesionInvalida();

        var email = vm.Email.Trim().ToLower();
        if (await db.Usuarios.AnyAsync(x => x.Id != u.Id && x.Email == email))
            ModelState.AddModelError(nameof(vm.Email), "Ya existe una cuenta con ese correo.");
        if (!ModelState.IsValid)
        {
            (vm.Cedula, vm.TipoPersona, vm.Rol) = (u.Cedula, u.TipoPersona, u.Rol);
            return View(vm);
        }

        u.Nombre = vm.Nombre.Trim();
        u.Email = email;
        u.LimiteEgresos = vm.LimiteEgresos;
        u.DiaCorte = vm.DiaCorte;
        await db.SaveChangesAsync();
        await CuentaService.IniciarSesionAsync(HttpContext, u, recordar: false); // refresca nombre/correo en la cookie
        TempData["Ok"] = "Perfil actualizado.";
        return RedirectToAction(nameof(Perfil));
    }

    public IActionResult CambiarPassword() => View(new CambiarPasswordViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel vm)
    {
        var u = await db.Usuarios.FindAsync(User.UsuarioId());
        if (u is null) return await CerrarSesionInvalida();

        if (ModelState.IsValid && !cuentas.VerificarPassword(u, vm.Actual))
            ModelState.AddModelError(nameof(vm.Actual), "La contraseña actual no es correcta.");
        if (!ModelState.IsValid) return View(vm);

        cuentas.EstablecerPassword(u, vm.Nueva);
        await db.SaveChangesAsync();
        TempData["Ok"] = "Contraseña actualizada.";
        return RedirectToAction(nameof(Perfil));
    }

    [AllowAnonymous]
    public IActionResult AccesoDenegado() => View();

    private async Task<IActionResult> CerrarSesionInvalida()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
