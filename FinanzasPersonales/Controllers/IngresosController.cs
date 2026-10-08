using FinanzasPersonales.Data;
using FinanzasPersonales.Models;
using FinanzasPersonales.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinanzasPersonales.Controllers;

[Authorize(Roles = Roles.Administrador)]
public class IngresosController(FinanzasContext db) : Controller
{
    public async Task<IActionResult> Index(string? buscar)
    {
        var query = db.Ingresos.Include(i => i.TipoIngreso).AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(i => i.Descripcion.Contains(buscar) || i.Institucion.Contains(buscar));
        ViewData["Buscar"] = buscar;
        return View(await query.OrderBy(i => i.Descripcion).ToListAsync());
    }

    public async Task<IActionResult> Create() => await Formulario(new Ingreso());

    public async Task<IActionResult> Edit(int id)
    {
        var ingreso = await db.Ingresos.FindAsync(id);
        return ingreso is null ? NotFound() : await Formulario(ingreso);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Ingreso ingreso)
    {
        if (!ModelState.IsValid) return await Formulario(ingreso);

        if (ingreso.Id == 0) db.Add(ingreso); else db.Update(ingreso);
        await db.SaveChangesAsync();
        TempData["Ok"] = "Ingreso guardado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var ingreso = await db.Ingresos.FindAsync(id);
        if (ingreso is null) return NotFound();
        ingreso.Estado = !ingreso.Estado;
        await db.SaveChangesAsync();
        TempData["Ok"] = $"Ingreso {(ingreso.Estado ? "activado" : "inactivado")}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Formulario(Ingreso ingreso)
    {
        ViewBag.TiposIngreso = new SelectList(await db.TiposIngreso.Where(t => t.Estado || t.Id == ingreso.TipoIngresoId).OrderBy(t => t.Descripcion).ToListAsync(), "Id", "Descripcion");
        return View("Form", ingreso);
    }
}
