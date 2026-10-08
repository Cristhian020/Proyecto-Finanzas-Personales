using System.Diagnostics;
using FinanzasPersonales.Data;
using FinanzasPersonales.Models;
using FinanzasPersonales.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanzasPersonales.Controllers;

public class HomeController(FinanzasContext db) : Controller
{
    public async Task<IActionResult> Index() =>
        View(User.EsAdmin() ? await PanelAdministrador() : await PanelUsuario());

    /// <summary>Resumen del periodo actual del usuario (según su día de corte).</summary>
    private async Task<DashboardViewModel> PanelUsuario()
    {
        var u = await db.Usuarios.FindAsync(User.UsuarioId());
        var p = CorteService.PeriodoQueContiene(u!.DiaCorte, DateTime.Today);
        var periodo = db.Transacciones.Where(t => t.UsuarioId == u.Id && t.Estado
            && t.FechaTransaccion >= p.Desde && t.FechaTransaccion <= p.Hasta);

        var egresosPorRenglon = await periodo.Where(t => t.TipoTransaccion == TipoTransaccion.Egreso)
            .GroupBy(t => t.Egreso!.RenglonEgreso!.Descripcion)
            .Select(g => new { Renglon = g.Key, Total = g.Sum(t => t.Monto) })
            .OrderByDescending(x => x.Total)
            .ToListAsync();

        return new DashboardViewModel
        {
            Nombre = u.Nombre,
            Desde = p.Desde,
            Hasta = p.Hasta,
            Transacciones = await periodo.CountAsync(),
            Ingresos = await periodo.Where(t => t.TipoTransaccion == TipoTransaccion.Ingreso).SumAsync(t => (decimal?)t.Monto) ?? 0m,
            Egresos = egresosPorRenglon.Sum(x => x.Total),
            LimiteEgresos = u.LimiteEgresos,
            BalanceUltimoCorte = await db.Cortes.Where(c => c.UsuarioId == u.Id)
                .OrderByDescending(c => c.Anio).ThenByDescending(c => c.Mes)
                .Select(c => (decimal?)c.BalanceAlCorte).FirstOrDefaultAsync(),
            EgresosPorRenglon = egresosPorRenglon.Select(x => (x.Renglon, x.Total)).ToList(),
            Ultimas = await db.Transacciones.Include(t => t.Egreso).Include(t => t.Ingreso)
                .Where(t => t.UsuarioId == u.Id)
                .OrderByDescending(t => t.FechaTransaccion).ThenByDescending(t => t.Id).Take(6).ToListAsync()
        };
    }

    /// <summary>Resumen global del mes calendario para el administrador.</summary>
    private async Task<DashboardViewModel> PanelAdministrador()
    {
        var desde = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var hasta = desde.AddMonths(1).AddDays(-1);
        var delMes = db.Transacciones.Where(t => t.Estado && t.FechaTransaccion >= desde && t.FechaTransaccion <= hasta);

        var vm = new DashboardViewModel
        {
            EsAdmin = true,
            Nombre = User.Identity!.Name!,
            Desde = desde,
            Hasta = hasta,
            Usuarios = await db.Usuarios.CountAsync(u => u.Estado),
            Transacciones = await delMes.CountAsync(),
            Ingresos = await delMes.Where(t => t.TipoTransaccion == TipoTransaccion.Ingreso).SumAsync(t => (decimal?)t.Monto) ?? 0m,
            Egresos = await delMes.Where(t => t.TipoTransaccion == TipoTransaccion.Egreso).SumAsync(t => (decimal?)t.Monto) ?? 0m,
            Ultimas = await db.Transacciones.Include(t => t.Usuario).Include(t => t.Egreso).Include(t => t.Ingreso)
                .OrderByDescending(t => t.FechaRegistro).ThenByDescending(t => t.Id).Take(6).ToListAsync()
        };

        // Usuarios que superan su límite en su periodo actual.
        foreach (var u in await db.Usuarios.Where(u => u.Estado && u.LimiteEgresos > 0).ToListAsync())
        {
            var p = CorteService.PeriodoQueContiene(u.DiaCorte, DateTime.Today);
            var gastado = await db.Transacciones
                .Where(t => t.UsuarioId == u.Id && t.Estado && t.TipoTransaccion == TipoTransaccion.Egreso
                            && t.FechaTransaccion >= p.Desde && t.FechaTransaccion <= p.Hasta)
                .SumAsync(t => (decimal?)t.Monto) ?? 0m;
            if (gastado > u.LimiteEgresos) vm.SobreLimite.Add((u, gastado));
        }

        return vm;
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
