using FinanzasPersonales.Models;
using FinanzasPersonales.Services;
using FinanzasPersonales.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace FinanzasPersonales.Tests.Unitarias;

/// <summary>Proceso de corte mensual contra una base de datos SQL Server real (LocalDB temporal).</summary>
public class CorteServiceTests(BaseDeDatosFixture bd) : IClassFixture<BaseDeDatosFixture>
{
    private static DateTime F(int anio, int mes, int dia) => new(anio, mes, dia);

    private async Task<(Corte? corte, string? error)> Procesar(int usuarioId, int anio, int mes)
    {
        await using var db = bd.CrearContexto();
        return await new CorteService(db).ProcesarAsync(usuarioId, anio, mes);
    }

    [Fact]
    public async Task PrimerCorte_SumaSoloTransaccionesActivasDelPeriodo()
    {
        var u = await bd.CrearUsuarioAsync(diaCorte: 28);
        await bd.AgregarTransaccionAsync(u.Id, TipoTransaccion.Ingreso, 50_000m, F(2025, 3, 1));   // primer día del periodo
        await bd.AgregarTransaccionAsync(u.Id, TipoTransaccion.Egreso, 10_000m, F(2025, 3, 10));
        await bd.AgregarTransaccionAsync(u.Id, TipoTransaccion.Egreso, 2_500m, F(2025, 3, 28));    // último día del periodo
        await bd.AgregarTransaccionAsync(u.Id, TipoTransaccion.Egreso, 999m, F(2025, 3, 15), activa: false); // anulada
        await bd.AgregarTransaccionAsync(u.Id, TipoTransaccion.Egreso, 777m, F(2025, 3, 29));      // ya es del periodo de abril
        await bd.AgregarTransaccionAsync(u.Id, TipoTransaccion.Ingreso, 555m, F(2025, 2, 28));     // es del periodo de febrero

        var (corte, error) = await Procesar(u.Id, 2025, 3);

        Assert.Null(error);
        Assert.NotNull(corte);
        Assert.Equal(F(2025, 3, 1), corte.FechaDesde);
        Assert.Equal(F(2025, 3, 28), corte.FechaCorte);
        Assert.Equal(0m, corte.BalanceInicial);
        Assert.Equal(50_000m, corte.TotalIngresos);
        Assert.Equal(12_500m, corte.TotalEgresos);
        Assert.Equal(37_500m, corte.BalanceAlCorte);
    }

    [Fact]
    public async Task SegundoCorte_TomaComoBalanceInicialElBalanceDelCorteAnterior()
    {
        var u = await bd.CrearUsuarioAsync(diaCorte: 15);
        await bd.AgregarTransaccionAsync(u.Id, TipoTransaccion.Ingreso, 1_000m, F(2025, 1, 10));
        await bd.AgregarTransaccionAsync(u.Id, TipoTransaccion.Egreso, 300m, F(2025, 1, 12));
        await bd.AgregarTransaccionAsync(u.Id, TipoTransaccion.Ingreso, 500m, F(2025, 2, 1));
        await bd.AgregarTransaccionAsync(u.Id, TipoTransaccion.Egreso, 50m, F(2025, 2, 15));

        var (enero, _) = await Procesar(u.Id, 2025, 1);
        var (febrero, error) = await Procesar(u.Id, 2025, 2);

        Assert.Null(error);
        Assert.Equal(700m, enero!.BalanceAlCorte);
        Assert.Equal(700m, febrero!.BalanceInicial);
        Assert.Equal(1_150m, febrero.BalanceAlCorte); // 700 + 500 - 50
    }

    [Fact]
    public async Task UnPeriodoSinMovimientos_ArrastraElBalance()
    {
        var u = await bd.CrearUsuarioAsync();
        await bd.AgregarTransaccionAsync(u.Id, TipoTransaccion.Ingreso, 2_000m, F(2025, 5, 5));

        await Procesar(u.Id, 2025, 5);
        var (junio, error) = await Procesar(u.Id, 2025, 6);

        Assert.Null(error);
        Assert.Equal(2_000m, junio!.BalanceInicial);
        Assert.Equal(0m, junio.TotalIngresos);
        Assert.Equal(0m, junio.TotalEgresos);
        Assert.Equal(2_000m, junio.BalanceAlCorte);
    }

    [Fact]
    public async Task LosCortesDebenSerConsecutivos()
    {
        var u = await bd.CrearUsuarioAsync();
        await Procesar(u.Id, 2025, 3);

        var (corte, error) = await Procesar(u.Id, 2025, 5); // se salta abril

        Assert.Null(corte);
        Assert.Contains("consecutivos", error);
        Assert.Contains("04/2025", error);
    }

    [Fact]
    public async Task SoloSePuedeReprocesarElUltimoCorte_YActualizaLosTotales()
    {
        var u = await bd.CrearUsuarioAsync();
        await bd.AgregarTransaccionAsync(u.Id, TipoTransaccion.Ingreso, 100m, F(2025, 7, 10));
        await Procesar(u.Id, 2025, 7);
        var (agosto, _) = await Procesar(u.Id, 2025, 8);

        var (_, errorJulio) = await Procesar(u.Id, 2025, 7);
        Assert.Contains("último corte", errorJulio);

        // Reprocesar agosto después de un nuevo movimiento recalcula el mismo corte.
        await bd.AgregarTransaccionAsync(u.Id, TipoTransaccion.Egreso, 40m, F(2025, 8, 20));
        var (agosto2, error) = await Procesar(u.Id, 2025, 8);

        Assert.Null(error);
        Assert.Equal(agosto!.Id, agosto2!.Id);
        Assert.Equal(40m, agosto2.TotalEgresos);
        Assert.Equal(60m, agosto2.BalanceAlCorte);

        await using var db = bd.CrearContexto();
        Assert.Equal(2, await db.Cortes.CountAsync(c => c.UsuarioId == u.Id));
    }

    [Fact]
    public async Task NoSePuedeCortarUnPeriodoQueNoHaTerminado()
    {
        var u = await bd.CrearUsuarioAsync(diaCorte: 28);
        var futuro = DateTime.Today.AddMonths(1);

        var (corte, error) = await Procesar(u.Id, futuro.Year, futuro.Month);

        Assert.Null(corte);
        Assert.Contains("aún no ha terminado", error);
    }

    [Fact]
    public async Task UsuarioInexistente_DevuelveError()
    {
        var (corte, error) = await Procesar(999_999, 2025, 1);
        Assert.Null(corte);
        Assert.Equal("El usuario no existe.", error);
    }

    [Fact]
    public async Task DespuesDelCorte_LasFechasDelPeriodoQuedanCerradas()
    {
        var u = await bd.CrearUsuarioAsync(diaCorte: 28);
        await using var db = bd.CrearContexto();
        var servicio = new CorteService(db);

        Assert.False(await servicio.FechaEstaCerradaAsync(u.Id, F(2025, 3, 10)));

        await servicio.ProcesarAsync(u.Id, 2025, 3);

        Assert.True(await servicio.FechaEstaCerradaAsync(u.Id, F(2025, 3, 10)));
        Assert.True(await servicio.FechaEstaCerradaAsync(u.Id, F(2025, 3, 28)));
        Assert.False(await servicio.FechaEstaCerradaAsync(u.Id, F(2025, 3, 29)));
    }

    [Fact]
    public async Task EgresosDelPeriodo_SumaSoloEgresosActivosDelPeriodo()
    {
        var u = await bd.CrearUsuarioAsync(diaCorte: 10, limite: 1_000m);
        await bd.AgregarTransaccionAsync(u.Id, TipoTransaccion.Egreso, 400m, F(2025, 4, 11));
        await bd.AgregarTransaccionAsync(u.Id, TipoTransaccion.Egreso, 700m, F(2025, 5, 10));
        await bd.AgregarTransaccionAsync(u.Id, TipoTransaccion.Egreso, 900m, F(2025, 5, 1), activa: false);
        await bd.AgregarTransaccionAsync(u.Id, TipoTransaccion.Ingreso, 5_000m, F(2025, 5, 1));
        await bd.AgregarTransaccionAsync(u.Id, TipoTransaccion.Egreso, 50m, F(2025, 5, 11)); // periodo siguiente

        await using var db = bd.CrearContexto();
        var (total, periodo) = await new CorteService(db).EgresosDelPeriodoAsync(u, F(2025, 5, 3));

        Assert.Equal(1_100m, total);
        Assert.Equal((2025, 5), (periodo.Anio, periodo.Mes));
    }

    [Fact]
    public async Task BaseDeDatos_RechazaTransaccionConGastoEIngresoALaVez()
    {
        var u = await bd.CrearUsuarioAsync();
        await using var db = bd.CrearContexto();
        db.Transacciones.Add(new Transaccion
        {
            UsuarioId = u.Id, TipoTransaccion = TipoTransaccion.Egreso, EgresoId = 1, IngresoId = 1,
            TipoPagoId = 1, Monto = 10m, FechaTransaccion = F(2025, 1, 1)
        });

        // La restricción CHECK de la tabla impide guardar ambos conceptos.
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
