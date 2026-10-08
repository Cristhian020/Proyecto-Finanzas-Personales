using FinanzasPersonales.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FinanzasPersonales.Tests.Infraestructura;

/// <summary>
/// Cada clase de pruebas usa su propia base de datos LocalDB temporal,
/// creada con las migraciones reales y eliminada al terminar.
/// </summary>
public static class BaseDeDatosDePrueba
{
    public static string NuevaCadenaConexion() =>
        $@"Server=(localdb)\MSSQLLocalDB;Database=FinanzasTests_{Guid.NewGuid():N};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public static FinanzasContext CrearContexto(string cadena) =>
        new(new DbContextOptionsBuilder<FinanzasContext>().UseSqlServer(cadena).Options);

    public static async Task EliminarAsync(string cadena)
    {
        SqlConnection.ClearAllPools();
        await using var db = CrearContexto(cadena);
        await db.Database.EnsureDeletedAsync();
    }
}

/// <summary>Genera cédulas válidas (con dígito verificador) y únicas para las pruebas.</summary>
public static class Datos
{
    private static int contador = 500_000_000;

    public static string NuevaCedula()
    {
        var prefijo = "0" + Interlocked.Increment(ref contador).ToString("D9");
        int suma = 0;
        for (int i = 0; i < 10; i++)
        {
            int prod = (prefijo[i] - '0') * (i % 2 == 0 ? 1 : 2);
            suma += prod >= 10 ? prod - 9 : prod;
        }
        return prefijo + (10 - suma % 10) % 10;
    }

    public static string NuevoEmail() => $"prueba_{Guid.NewGuid():N}@test.local";
}
