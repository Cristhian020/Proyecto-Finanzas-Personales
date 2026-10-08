using FinanzasPersonales.Data;
using FinanzasPersonales.Models;
using FinanzasPersonales.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinanzasPersonales.Controllers;

[Authorize(Roles = Roles.Administrador)]
public class EgresosController(FinanzasContext db) : Controller
{
    public async Task<IActionResult> Index(string? buscar)
    {
        var query = db.Egresos.Include(e => e.TipoEgreso).Include(e => e.RenglonEgreso).Include(e => e.TipoPagoDefecto).AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(e => e.Descripcion.Contains(buscar));
        ViewData["Buscar"] = buscar;
        return View(await query.OrderBy(e => e.Descripcion).ToListAsync());
    }

    public async Task<IActionResult> Create() => await Formulario(new Egreso());

    public async Task<IActionResult> Edit(int id)
    {
        var egreso = await db.Egresos.FindAsync(id);
        return egreso is null ? NotFound() : await Formulario(egreso);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Egreso egreso)
    {
        if (!ModelState.IsValid) return await Formulario(egreso);

        if (egreso.Id == 0) db.Add(egreso); else db.Update(egreso);
        await db.SaveChangesAsync();
        TempData["Ok"] = "Egreso guardado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var egreso = await db.Egresos.FindAsync(id);
        if (egreso is null) return NotFound();
        egreso.Estado = !egreso.Estado;
        await db.SaveChangesAsync();
        TempData["Ok"] = $"Egreso {(egreso.Estado ? "activado" : "inactivado")}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Formulario(Egreso egreso)
    {
        // Sólo se ofrecen catálogos activos (más el valor actual si fue inactivado).
        ViewBag.TiposEgreso = new SelectList(await db.TiposEgreso.Where(t => t.Estado || t.Id == egreso.TipoEgresoId).OrderBy(t => t.Descripcion).ToListAsync(), "Id", "Descripcion");
        ViewBag.Renglones = new SelectList(await db.RenglonesEgreso.Where(t => t.Estado || t.Id == egreso.RenglonEgresoId).OrderBy(t => t.Descripcion).ToListAsync(), "Id", "Descripcion");
        ViewBag.TiposPago = new SelectList(await db.TiposPago.Where(t => t.Estado || t.Id == egreso.TipoPagoDefectoId).OrderBy(t => t.Descripcion).ToListAsync(), "Id", "Descripcion");
        return View("Form", egreso);
    }
}
