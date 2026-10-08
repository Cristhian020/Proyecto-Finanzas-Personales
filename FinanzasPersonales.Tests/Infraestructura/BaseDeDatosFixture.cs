using FinanzasPersonales.Data;
using FinanzasPersonales.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanzasPersonales.Tests.Infraestructura;

/// <summary>Base de datos migrada (con datos semilla) compartida por las pruebas de servicios.</summary>
public class BaseDeDatosFixture : IAsyncLifetime
{
    public string Cadena { get; } = BaseDeDatosDePrueba.NuevaCadenaConexion();

    public FinanzasContext CrearContexto() => BaseDeDatosDePrueba.CrearContexto(Cadena);

    public async Task InitializeAsync()
    {
        await using var db = CrearContexto();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => BaseDeDatosDePrueba.EliminarAsync(Cadena);

    /// <summary>Crea un usuario nuevo para que cada prueba trabaje con datos aislados.</summary>
    public async Task<Usuario> CrearUsuarioAsync(int diaCorte = 28, decimal limite = 0m)
    {
        await using var db = CrearContexto();
        var u = new Usuario
        {
            Nombre = "Usuario de prueba",
            Email = Datos.NuevoEmail(),
            Cedula = Datos.NuevaCedula(),
            DiaCorte = diaCorte,
            LimiteEgresos = limite,
            PasswordHash = "x"
        };
        db.Usuarios.Add(u);
        await db.SaveChangesAsync();
        return u;
    }

    /// <summary>Registra una transacción. Ingresos usan el ingreso semilla 1, egresos el egreso semilla 1.</summary>
    public async Task<Transaccion> AgregarTransaccionAsync(int usuarioId, TipoTransaccion tipo, decimal monto, DateTime fecha, bool activa = true)
    {
        await using var db = CrearContexto();
        var t = new Transaccion
        {
            UsuarioId = usuarioId,
            TipoTransaccion = tipo,
            IngresoId = tipo == TipoTransaccion.Ingreso ? 1 : null,
            EgresoId = tipo == TipoTransaccion.Egreso ? 1 : null,
            TipoPagoId = 1,
            FechaTransaccion = fecha,
            Monto = monto,
            Estado = activa
        };
        db.Transacciones.Add(t);
        await db.SaveChangesAsync();
        return t;
    }
}
