using FinanzasPersonales.Data;
using FinanzasPersonales.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanzasPersonales.Services;

public record Periodo(int Anio, int Mes, DateTime Desde, DateTime Hasta);

/// <summary>
/// Reglas del corte mensual. El periodo de un mes va desde el día siguiente al corte
/// del mes anterior hasta el día de corte del usuario en ese mes (inclusive).
/// </summary>
public class CorteService(FinanzasContext db)
{
    public static Periodo PeriodoDe(int diaCorte, int anio, int mes)
    {
        var hasta = new DateTime(anio, mes, diaCorte);
        var desde = hasta.AddMonths(-1).AddDays(1);
        return new Periodo(anio, mes, desde, hasta);
    }

    /// <summary>Periodo al que pertenece una fecha según el día de corte.</summary>
    public static Periodo PeriodoQueContiene(int diaCorte, DateTime fecha)
    {
        var mes = fecha.Day <= diaCorte ? fecha : fecha.AddMonths(1);
        return PeriodoDe(diaCorte, mes.Year, mes.Month);
    }

    /// <summary>Fecha hasta la cual las transacciones del usuario están cerradas (último corte), o null.</summary>
    public async Task<DateTime?> FechaCerradaAsync(int usuarioId) =>
        await db.Cortes.Where(c => c.UsuarioId == usuarioId)
            .MaxAsync(c => (DateTime?)c.FechaCorte);

    public async Task<bool> FechaEstaCerradaAsync(int usuarioId, DateTime fecha)
    {
        var cerrada = await FechaCerradaAsync(usuarioId);
        return cerrada.HasValue && fecha.Date <= cerrada.Value;
    }

    /// <summary>Total de egresos activos del usuario en el periodo que contiene la fecha.</summary>
    public async Task<(decimal total, Periodo periodo)> EgresosDelPeriodoAsync(Usuario usuario, DateTime fecha)
    {
        var p = PeriodoQueContiene(usuario.DiaCorte, fecha);
        var total = await db.Transacciones
            .Where(t => t.UsuarioId == usuario.Id && t.Estado && t.TipoTransaccion == TipoTransaccion.Egreso
                        && t.FechaTransaccion >= p.Desde && t.FechaTransaccion <= p.Hasta)
            .SumAsync(t => (decimal?)t.Monto) ?? 0m;
        return (total, p);
    }

    /// <summary>
    /// Procesa (o reprocesa, si es el último) el corte de un usuario para un año/mes.
    /// Devuelve el corte o un mensaje de error.
    /// </summary>
    public async Task<(Corte? corte, string? error)> ProcesarAsync(int usuarioId, int anio, int mes)
    {
        var usuario = await db.Usuarios.FindAsync(usuarioId);
        if (usuario is null) return (null, "El usuario no existe.");

        var periodo = PeriodoDe(usuario.DiaCorte, anio, mes);
        if (periodo.Hasta > DateTime.Today)
            return (null, $"El periodo aún no ha terminado (cierra el {periodo.Hasta:dd/MM/yyyy}).");

        var cortes = await db.Cortes.Where(c => c.UsuarioId == usuarioId)
            .OrderBy(c => c.Anio).ThenBy(c => c.Mes).ToListAsync();
        var existente = cortes.FirstOrDefault(c => c.Anio == anio && c.Mes == mes);
        var ultimo = cortes.LastOrDefault();

        if (existente is not null && existente != ultimo)
            return (null, "Sólo se puede reprocesar el último corte del usuario.");

        Corte? anterior;
        if (existente is not null)
        {
            anterior = cortes.Count > 1 ? cortes[^2] : null;
        }
        else
        {
            anterior = ultimo;
            if (ultimo is not null)
            {
                var siguiente = new DateTime(ultimo.Anio, ultimo.Mes, 1).AddMonths(1);
                if (siguiente.Year != anio || siguiente.Month != mes)
                    return (null, $"Los cortes son consecutivos: el próximo corte a procesar es {siguiente:MM/yyyy}.");
            }
        }

        var movimientos = db.Transacciones.Where(t => t.UsuarioId == usuarioId && t.Estado
            && t.FechaTransaccion >= periodo.Desde && t.FechaTransaccion <= periodo.Hasta);
        var ingresos = await movimientos.Where(t => t.TipoTransaccion == TipoTransaccion.Ingreso).SumAsync(t => (decimal?)t.Monto) ?? 0m;
        var egresos = await movimientos.Where(t => t.TipoTransaccion == TipoTransaccion.Egreso).SumAsync(t => (decimal?)t.Monto) ?? 0m;

        var corte = existente ?? new Corte { UsuarioId = usuarioId, Anio = anio, Mes = mes };
        corte.FechaDesde = periodo.Desde;
        corte.FechaCorte = periodo.Hasta;
        corte.BalanceInicial = anterior?.BalanceAlCorte ?? 0m;
        corte.TotalIngresos = ingresos;
        corte.TotalEgresos = egresos;
        corte.BalanceAlCorte = corte.BalanceInicial + ingresos - egresos;
        corte.FechaProceso = DateTime.Now;

        if (existente is null) db.Cortes.Add(corte);
        await db.SaveChangesAsync();
        return (corte, null);
    }
}
