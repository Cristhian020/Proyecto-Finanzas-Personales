using FinanzasPersonales.Data;
using FinanzasPersonales.Models;
using FinanzasPersonales.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanzasPersonales.Controllers;

/// <summary>
/// CRUD genérico para los catálogos simples (Id, Descripción, Estado).
/// Todas las vistas se comparten en Views/Shared/Catalogo.
/// </summary>
[Authorize(Roles = Roles.Administrador)]
public abstract class CatalogoController<T>(FinanzasContext db) : Controller where T : Catalogo, new()
{
    protected abstract string Titulo { get; }
    protected abstract string TituloSingular { get; }

    private ViewResult Vista(string nombre, object? modelo)
    {
        ViewData["Titulo"] = Titulo;
        ViewData["TituloSingular"] = TituloSingular;
        return View($"~/Views/Shared/Catalogo/{nombre}.cshtml", modelo);
    }

    public async Task<IActionResult> Index(string? buscar)
    {
        var query = db.Set<T>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(c => c.Descripcion.Contains(buscar));
        ViewData["Buscar"] = buscar;
        return Vista("Index", await query.OrderBy(c => c.Descripcion).ToListAsync());
    }

    public IActionResult Create() => Vista("Form", new T());

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.Set<T>().FindAsync(id);
        return item is null ? NotFound() : Vista("Form", item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(T item)
    {
        item.Descripcion = item.Descripcion.Trim();
        if (await db.Set<T>().AnyAsync(c => c.Id != item.Id && c.Descripcion == item.Descripcion))
            ModelState.AddModelError(nameof(Catalogo.Descripcion), "Ya existe un registro con esa descripción.");
        if (!ModelState.IsValid) return Vista("Form", item);

        if (item.Id == 0) db.Add(item); else db.Update(item);
        await db.SaveChangesAsync();
        TempData["Ok"] = $"{TituloSingular} guardado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var item = await db.Set<T>().FindAsync(id);
        if (item is null) return NotFound();
        item.Estado = !item.Estado;
        await db.SaveChangesAsync();
        TempData["Ok"] = $"{TituloSingular} {(item.Estado ? "activado" : "inactivado")}.";
        return RedirectToAction(nameof(Index));
    }
}

public class TiposEgresoController(FinanzasContext db) : CatalogoController<TipoEgreso>(db)
{
    protected override string Titulo => "Tipos de Egreso";
    protected override string TituloSingular => "Tipo de egreso";
}

public class TiposIngresoController(FinanzasContext db) : CatalogoController<TipoIngreso>(db)
{
    protected override string Titulo => "Tipos de Ingreso";
    protected override string TituloSingular => "Tipo de ingreso";
}

public class RenglonesEgresoController(FinanzasContext db) : CatalogoController<RenglonEgreso>(db)
{
    protected override string Titulo => "Renglones de Egreso";
    protected override string TituloSingular => "Renglón de egreso";
}

public class TiposPagoController(FinanzasContext db) : CatalogoController<TipoPago>(db)
{
    protected override string Titulo => "Tipos de Pago";
    protected override string TituloSingular => "Tipo de pago";
}
