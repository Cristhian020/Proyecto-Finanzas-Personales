using FinanzasPersonales.Data;
using FinanzasPersonales.Models;
using FinanzasPersonales.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinanzasPersonales.Controllers;

/// <summary>Proceso de corte mensual y reporte de cortes.</summary>
public class CortesController(FinanzasContext db, CorteService servicio) : Controller
{
    private IQueryable<Corte> Visibles() =>
        User.EsAdmin() ? db.Cortes : db.Cortes.Where(c => c.UsuarioId == User.UsuarioId());

    public async Task<IActionResult> Index()
    {
        var lista = await Visibles().Include(c => c.Usuario)
            .OrderByDescending(c => c.Anio).ThenByDescending(c => c.Mes).ThenBy(c => c.Usuario!.Nombre)
            .ToListAsync();
        return View(lista);
    }

    public async Task<IActionResult> Procesar(int? usuarioId)
    {
        await CargarUsuarios();
        var vm = new ProcesarCorteViewModel { UsuarioId = User.EsAdmin() ? usuarioId : User.UsuarioId() };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Procesar(ProcesarCorteViewModel vm)
    {
        // Un usuario normal sólo puede procesar su propio corte.
        if (!User.EsAdmin())
        {
            vm.UsuarioId = User.UsuarioId();
            ModelState.Remove(nameof(vm.UsuarioId));
        }
        if (ModelState.IsValid)
        {
            var (corte, error) = await servicio.ProcesarAsync(vm.UsuarioId!.Value, vm.Anio, vm.Mes);
            if (corte is not null)
            {
                TempData["Ok"] = $"Corte {corte.Mes:00}/{corte.Anio} procesado. Balance al corte: {corte.BalanceAlCorte:C}.";
                return RedirectToAction(nameof(Detalle), new { id = corte.Id });
            }
            ModelState.AddModelError("", error!);
        }
        await CargarUsuarios();
        return View(vm);
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var corte = await Visibles().Include(c => c.Usuario).FirstOrDefaultAsync(c => c.Id == id);
        if (corte is null) return NotFound();

        var transacciones = await db.Transacciones
            .Include(t => t.Egreso!).ThenInclude(e => e.RenglonEgreso)
            .Include(t => t.Ingreso).Include(t => t.TipoPago)
            .Where(t => t.UsuarioId == corte.UsuarioId && t.Estado
                        && t.FechaTransaccion >= corte.FechaDesde && t.FechaTransaccion <= corte.FechaCorte)
            .OrderBy(t => t.FechaTransaccion).ThenBy(t => t.Id)
            .ToListAsync();

        var porRenglon = transacciones
            .Where(t => t.TipoTransaccion == TipoTransaccion.Egreso)
            .GroupBy(t => t.Egreso?.RenglonEgreso?.Descripcion ?? "(Sin renglón)")
            .Select(g => (g.Key, g.Sum(t => t.Monto)))
            .OrderByDescending(x => x.Item2)
            .ToList();

        return View(new DetalleCorteViewModel { Corte = corte, Transacciones = transacciones, EgresosPorRenglon = porRenglon });
    }

    /// <summary>Reporte de cortes entre fechas y/o por usuario.</summary>
    public async Task<IActionResult> Reporte(ReporteCorteFiltro filtro)
    {
        if (!User.EsAdmin()) filtro.UsuarioId = User.UsuarioId();
        var q = db.Cortes.Include(c => c.Usuario).AsQueryable();
        if (filtro.UsuarioId is not null) q = q.Where(c => c.UsuarioId == filtro.UsuarioId);
        if (filtro.Desde is not null) q = q.Where(c => c.FechaCorte >= filtro.Desde);
        if (filtro.Hasta is not null) q = q.Where(c => c.FechaCorte <= filtro.Hasta);

        await CargarUsuarios(soloActivos: false);
        return View(new ReporteCorteViewModel
        {
            Filtro = filtro,
            Cortes = await q.OrderBy(c => c.Usuario!.Nombre).ThenBy(c => c.Anio).ThenBy(c => c.Mes).ToListAsync()
        });
    }

    private async Task CargarUsuarios(bool soloActivos = true)
    {
        if (!User.EsAdmin()) return;
        var usuarios = await db.Usuarios.Where(u => !soloActivos || u.Estado).OrderBy(u => u.Nombre).ToListAsync();
        ViewBag.Usuarios = new SelectList(usuarios, "Id", "Nombre");
    }
}
