using FinanzasPersonales.Services;

namespace FinanzasPersonales.Tests.Unitarias;

/// <summary>Cálculo del periodo de corte según el día de corte del usuario.</summary>
public class PeriodoTests
{
    [Fact]
    public void PeriodoDeSeptiembre_ConCorteDia28_VaDel29DeAgostoAl28DeSeptiembre()
    {
        var p = CorteService.PeriodoDe(28, 2026, 9);
        Assert.Equal(new DateTime(2026, 8, 29), p.Desde);
        Assert.Equal(new DateTime(2026, 9, 28), p.Hasta);
    }

    [Fact]
    public void PeriodoDeEnero_CruzaElCambioDeAnio()
    {
        var p = CorteService.PeriodoDe(15, 2026, 1);
        Assert.Equal(new DateTime(2025, 12, 16), p.Desde);
        Assert.Equal(new DateTime(2026, 1, 15), p.Hasta);
    }

    [Fact]
    public void PeriodoDeMarzo_ConCorteDia28_EmpiezaEl1DeMarzo()
    {
        // Febrero 2025 tiene 28 días: el periodo siguiente arranca el 1 de marzo.
        var p = CorteService.PeriodoDe(28, 2025, 3);
        Assert.Equal(new DateTime(2025, 3, 1), p.Desde);
        Assert.Equal(new DateTime(2025, 3, 28), p.Hasta);
    }

    [Theory]
    [InlineData(28, "2026-10-08", 2026, 10)] // antes del día de corte: periodo del mismo mes
    [InlineData(28, "2026-10-28", 2026, 10)] // el mismo día de corte pertenece al mes
    [InlineData(28, "2026-09-29", 2026, 10)] // después del corte: periodo del mes siguiente
    [InlineData(15, "2026-12-20", 2027, 1)]  // diciembre después del corte pasa a enero del año siguiente
    [InlineData(1, "2026-03-01", 2026, 3)]
    [InlineData(1, "2026-03-02", 2026, 4)]
    public void PeriodoQueContiene_AsignaLaFechaAlMesCorrecto(int dia, string fecha, int anio, int mes)
    {
        var f = DateTime.Parse(fecha);
        var p = CorteService.PeriodoQueContiene(dia, f);
        Assert.Equal((anio, mes), (p.Anio, p.Mes));
        Assert.InRange(f, p.Desde, p.Hasta);
    }

    [Fact]
    public void PeriodosConsecutivos_NoSeSolapanNiDejanHuecos()
    {
        for (int dia = 1; dia <= 28; dia++)
        {
            var anterior = CorteService.PeriodoDe(dia, 2026, 1);
            for (int m = 2; m <= 12; m++)
            {
                var actual = CorteService.PeriodoDe(dia, 2026, m);
                Assert.Equal(anterior.Hasta.AddDays(1), actual.Desde);
                anterior = actual;
            }
        }
    }
}
