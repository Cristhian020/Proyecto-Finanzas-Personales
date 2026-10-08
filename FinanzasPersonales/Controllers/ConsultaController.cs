using FinanzasPersonales.Data;
using FinanzasPersonales.Models;
using FinanzasPersonales.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinanzasPersonales.Controllers;

/// <summary>Consulta de transacciones por criterios.</summary>
public class ConsultaController(FinanzasContext db) : Controller
{
    public async Task<IActionResult> Index(ConsultaFiltro filtro, bool buscar = false)
    {
        // Un usuario normal sólo consulta sus propias transacciones.
        if (!User.EsAdmin()) filtro.UsuarioId = User.UsuarioId();
        var vm = new ConsultaViewModel { Filtro = filtro };

        if (buscar)
        {
            var q = db.Transacciones
                .Include(t => t.Usuario).Include(t => t.TipoPago)
                .Include(t => t.Egreso!).ThenInclude(e => e.TipoEgreso)
                .Include(t => t.Egreso!).ThenInclude(e => e.RenglonEgreso)
                .Include(t => t.Ingreso!).ThenInclude(i => i.TipoIngreso)
                .AsQueryable();

            if (!filtro.IncluirAnuladas) q = q.Where(t => t.Estado);
            if (filtro.UsuarioId is not null) q = q.Where(t => t.UsuarioId == filtro.UsuarioId);
            if (filtro.TipoTransaccion is not null) q = q.Where(t => t.TipoTransaccion == filtro.TipoTransaccion);
            if (filtro.Desde is not null) q = q.Where(t => t.FechaTransaccion >= filtro.Desde);
            if (filtro.Hasta is not null) q = q.Where(t => t.FechaTransaccion <= filtro.Hasta);
            if (filtro.TipoEgresoId is not null) q = q.Where(t => t.Egreso!.TipoEgresoId == filtro.TipoEgresoId);
            if (filtro.RenglonEgresoId is not null) q = q.Where(t => t.Egreso!.RenglonEgresoId == filtro.RenglonEgresoId);
            if (filtro.TipoIngresoId is not null) q = q.Where(t => t.Ingreso!.TipoIngresoId == filtro.TipoIngresoId);
            if (filtro.TipoPagoId is not null) q = q.Where(t => t.TipoPagoId == filtro.TipoPagoId);

            vm.Resultados = await q.OrderBy(t => t.FechaTransaccion).ThenBy(t => t.Id).ToListAsync();
        }

        if (User.EsAdmin())
            ViewBag.Usuarios = new SelectList(await db.Usuarios.OrderBy(u => u.Nombre).ToListAsync(), "Id", "Nombre");
        ViewBag.TiposEgreso = new SelectList(await db.TiposEgreso.OrderBy(x => x.Descripcion).ToListAsync(), "Id", "Descripcion");
        ViewBag.Renglones = new SelectList(await db.RenglonesEgreso.OrderBy(x => x.Descripcion).ToListAsync(), "Id", "Descripcion");
        ViewBag.TiposIngreso = new SelectList(await db.TiposIngreso.OrderBy(x => x.Descripcion).ToListAsync(), "Id", "Descripcion");
        ViewBag.TiposPago = new SelectList(await db.TiposPago.OrderBy(x => x.Descripcion).ToListAsync(), "Id", "Descripcion");
        ViewBag.Buscar = buscar;
        return View(vm);
    }
}
