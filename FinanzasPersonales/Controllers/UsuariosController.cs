using FinanzasPersonales.Data;
using FinanzasPersonales.Models;
using FinanzasPersonales.Services;
using FinanzasPersonales.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanzasPersonales.Controllers;

[Authorize(Roles = Roles.Administrador)]
public class UsuariosController(FinanzasContext db, CuentaService cuentas) : Controller
{
    public async Task<IActionResult> Index(string? buscar)
    {
        var query = db.Usuarios.AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var digitos = DocumentoIdentidad.Limpiar(buscar);
            query = query.Where(u => u.Nombre.Contains(buscar) || u.Email.Contains(buscar) || (digitos != "" && u.Cedula.Contains(digitos)));
        }
        ViewData["Buscar"] = buscar;
        return View(await query.OrderBy(u => u.Nombre).ToListAsync());
    }

    public IActionResult Create() => View("Form", new Usuario());

    public async Task<IActionResult> Edit(int id)
    {
        var usuario = await db.Usuarios.FindAsync(id);
        return usuario is null ? NotFound() : View("Form", usuario);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Usuario form)
    {
        form.Cedula = DocumentoIdentidad.Limpiar(form.Cedula);
        form.Email = form.Email.Trim().ToLower();

        if (await db.Usuarios.AnyAsync(u => u.Id != form.Id && u.Cedula == form.Cedula))
            ModelState.AddModelError(nameof(Usuario.Cedula), "Ya existe un usuario con esa cédula / RNC.");
        if (await db.Usuarios.AnyAsync(u => u.Id != form.Id && u.Email == form.Email))
            ModelState.AddModelError(nameof(Usuario.Email), "Ya existe un usuario con ese correo.");
        if (form.Id == 0 && string.IsNullOrWhiteSpace(form.NuevaPassword))
            ModelState.AddModelError(nameof(Usuario.NuevaPassword), "Indique una contraseña para el nuevo usuario.");

        var esUnoMismo = form.Id == User.UsuarioId();
        if (esUnoMismo && (form.Rol != Rol.Administrador || !form.Estado))
            ModelState.AddModelError("", "No puede quitarse el rol de administrador ni inactivar su propia cuenta.");

        if (!ModelState.IsValid) return View("Form", form);

        var usuario = form.Id == 0 ? new Usuario() : await db.Usuarios.FindAsync(form.Id);
        if (usuario is null) return NotFound();

        usuario.Nombre = form.Nombre.Trim();
        usuario.Email = form.Email;
        usuario.Cedula = form.Cedula;
        usuario.TipoPersona = form.TipoPersona;
        usuario.LimiteEgresos = form.LimiteEgresos;
        usuario.DiaCorte = form.DiaCorte;
        usuario.Rol = form.Rol;
        usuario.Estado = form.Estado;
        if (!string.IsNullOrWhiteSpace(form.NuevaPassword)) cuentas.EstablecerPassword(usuario, form.NuevaPassword);

        if (usuario.Id == 0) db.Add(usuario);
        await db.SaveChangesAsync();
        TempData["Ok"] = "Usuario guardado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        if (id == User.UsuarioId())
        {
            TempData["Error"] = "No puede inactivar su propia cuenta.";
            return RedirectToAction(nameof(Index));
        }
        var usuario = await db.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();
        usuario.Estado = !usuario.Estado;
        await db.SaveChangesAsync();
        TempData["Ok"] = $"Usuario {(usuario.Estado ? "activado" : "inactivado")}.";
        return RedirectToAction(nameof(Index));
    }
}
