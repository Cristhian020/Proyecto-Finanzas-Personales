using System.ComponentModel.DataAnnotations;
using FinanzasPersonales.Data;
using FinanzasPersonales.Models;
using FinanzasPersonales.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinanzasPersonales.Controllers;

/// <summary>
/// Registro de transacciones. El administrador gestiona las de todos los usuarios;
/// un usuario normal sólo ve y modifica las suyas.
/// </summary>
public class TransaccionesController(FinanzasContext db, CorteService cortes) : Controller
{
    private IQueryable<Transaccion> Visibles() =>
        User.EsAdmin() ? db.Transacciones : db.Transacciones.Where(t => t.UsuarioId == User.UsuarioId());

    public async Task<IActionResult> Index()
    {
        var lista = await Visibles()
            .Include(t => t.Usuario).Include(t => t.Egreso).Include(t => t.Ingreso).Include(t => t.TipoPago)
            .OrderByDescending(t => t.FechaTransaccion).ThenByDescending(t => t.Id)
            .Take(100)
            .ToListAsync();
        return View(lista);
    }

    public async Task<IActionResult> Create() =>
        await Formulario(new Transaccion { UsuarioId = User.EsAdmin() ? null : User.UsuarioId() });

    public async Task<IActionResult> Edit(int id)
    {
        var t = await Visibles().FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        if (await cortes.FechaEstaCerradaAsync(t.UsuarioId!.Value, t.FechaTransaccion))
        {
            TempData["Error"] = "La transacción pertenece a un periodo ya cortado y no puede modificarse.";
            return RedirectToAction(nameof(Index));
        }
        return await Formulario(t);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Transaccion t)
    {
        // Un usuario normal sólo puede registrar transacciones a su nombre.
        if (!User.EsAdmin())
        {
            t.UsuarioId = User.UsuarioId();
            ModelState.Remove(nameof(t.UsuarioId));
        }

        // MVC omite Validate() cuando algún campo ya tiene error (p. ej. el UsuarioId que no envía
        // un usuario normal), así que las reglas del modelo se ejecutan siempre aquí.
        ValidarReglas(t);

        // Sólo se guarda el concepto que corresponde al tipo de transacción.
        if (t.TipoTransaccion == TipoTransaccion.Egreso) t.IngresoId = null; else t.EgresoId = null;

        var tipoPago = t.TipoPagoId is null ? null : await db.TiposPago.FindAsync(t.TipoPagoId);
        if (tipoPago is { EsTarjeta: true } && string.IsNullOrWhiteSpace(t.NoTarjeta))
            ModelState.AddModelError(nameof(t.NoTarjeta), "Indique los últimos 4 dígitos de la tarjeta.");
        if (tipoPago is { EsTarjeta: false }) t.NoTarjeta = null;

        Usuario? usuario = null;
        if (t.UsuarioId is not null)
        {
            usuario = await db.Usuarios.FindAsync(t.UsuarioId);
            if (await cortes.FechaEstaCerradaAsync(t.UsuarioId.Value, t.FechaTransaccion))
                ModelState.AddModelError(nameof(t.FechaTransaccion), "Esa fecha pertenece a un periodo ya cortado para este usuario.");
        }

        if (t.Id != 0)
        {
            var original = await Visibles().AsNoTracking().FirstOrDefaultAsync(x => x.Id == t.Id);
            if (original is null) return NotFound();
            if (await cortes.FechaEstaCerradaAsync(original.UsuarioId!.Value, original.FechaTransaccion))
                ModelState.AddModelError("", "La transacción original pertenece a un periodo ya cortado.");
            t.FechaRegistro = original.FechaRegistro;
        }
        else
        {
            t.FechaRegistro = DateTime.Now;
        }

        if (!ModelState.IsValid) return await Formulario(t);

        if (t.Id == 0) db.Add(t); else db.Update(t);
        await db.SaveChangesAsync();
        TempData["Ok"] = $"Transacción No. {t.Id} guardada correctamente.";

        if (usuario is not null && t.TipoTransaccion == TipoTransaccion.Egreso && usuario.LimiteEgresos > 0)
            await AvisarLimite(usuario, t.FechaTransaccion);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var t = await Visibles().FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        if (await cortes.FechaEstaCerradaAsync(t.UsuarioId!.Value, t.FechaTransaccion))
        {
            TempData["Error"] = "La transacción pertenece a un periodo ya cortado y no puede anularse.";
            return RedirectToAction(nameof(Index));
        }
        t.Estado = !t.Estado;
        await db.SaveChangesAsync();
        TempData["Ok"] = $"Transacción No. {t.Id} {(t.Estado ? "reactivada" : "anulada")}.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Devuelve el tipo de pago por defecto de un egreso (para autollenar el formulario).</summary>
    public async Task<IActionResult> TipoPagoDefecto(int egresoId)
    {
        var egreso = await db.Egresos.FindAsync(egresoId);
        return Json(egreso?.TipoPagoDefectoId);
    }

    private void ValidarReglas(Transaccion t)
    {
        foreach (var r in t.Validate(new ValidationContext(t)))
            foreach (var campo in r.MemberNames)
                if (ModelState[campo]?.Errors.Any(e => e.ErrorMessage == r.ErrorMessage) != true)
                    ModelState.AddModelError(campo, r.ErrorMessage!);
    }

    private async Task AvisarLimite(Usuario usuario, DateTime fecha)
    {
        var (total, periodo) = await cortes.EgresosDelPeriodoAsync(usuario, fecha);
        if (total > usuario.LimiteEgresos)
        {
            var quien = usuario.Id == User.UsuarioId() ? "Has gastado" : $"{usuario.Nombre} ha gastado";
            TempData["Warning"] =
                $"¡Atención! {quien} {total:C} en el periodo {periodo.Desde:dd/MM/yyyy} – {periodo.Hasta:dd/MM/yyyy}, " +
                $"superando el límite de egresos de {usuario.LimiteEgresos:C} por {total - usuario.LimiteEgresos:C}.";
        }
    }

    private async Task<IActionResult> Formulario(Transaccion t)
    {
        if (User.EsAdmin())
            ViewBag.Usuarios = new SelectList(await db.Usuarios.Where(u => u.Estado || u.Id == t.UsuarioId).OrderBy(u => u.Nombre).ToListAsync(), "Id", "Nombre");
        ViewBag.Egresos = new SelectList(await db.Egresos.Where(e => e.Estado || e.Id == t.EgresoId).OrderBy(e => e.Descripcion).ToListAsync(), "Id", "Descripcion");
        ViewBag.Ingresos = new SelectList(await db.Ingresos.Where(i => i.Estado || i.Id == t.IngresoId).OrderBy(i => i.Descripcion).ToListAsync(), "Id", "Descripcion");
        var tiposPago = await db.TiposPago.Where(p => p.Estado || p.Id == t.TipoPagoId).OrderBy(p => p.Descripcion).ToListAsync();
        ViewBag.TiposPago = new SelectList(tiposPago, "Id", "Descripcion");
        ViewBag.TiposPagoTarjeta = tiposPago.Where(p => p.EsTarjeta).Select(p => p.Id).ToList();
        return View("Form", t);
    }
}
