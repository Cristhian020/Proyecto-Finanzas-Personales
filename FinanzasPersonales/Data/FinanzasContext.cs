using FinanzasPersonales.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanzasPersonales.Data;

public class FinanzasContext(DbContextOptions<FinanzasContext> options) : DbContext(options)
{
    public DbSet<TipoEgreso> TiposEgreso => Set<TipoEgreso>();
    public DbSet<TipoIngreso> TiposIngreso => Set<TipoIngreso>();
    public DbSet<RenglonEgreso> RenglonesEgreso => Set<RenglonEgreso>();
    public DbSet<TipoPago> TiposPago => Set<TipoPago>();
    public DbSet<Egreso> Egresos => Set<Egreso>();
    public DbSet<Ingreso> Ingresos => Set<Ingreso>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Transaccion> Transacciones => Set<Transaccion>();
    public DbSet<Corte> Cortes => Set<Corte>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Nunca se borra en cascada: los registros se inactivan (Estado = false).
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;

        modelBuilder.Entity<TipoPago>().Ignore(t => t.EsTarjeta);
        modelBuilder.Entity<Transaccion>().Ignore(t => t.Concepto);

        modelBuilder.Entity<Usuario>().HasIndex(u => u.Cedula).IsUnique();
        modelBuilder.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Corte>().HasIndex(c => new { c.UsuarioId, c.Anio, c.Mes }).IsUnique();
        modelBuilder.Entity<Transaccion>().HasIndex(t => new { t.UsuarioId, t.FechaTransaccion });

        modelBuilder.Entity<Transaccion>().ToTable(t => t.HasCheckConstraint(
            "CK_Transaccion_Concepto",
            "([TipoTransaccion] = 2 AND [EgresoId] IS NOT NULL AND [IngresoId] IS NULL) OR " +
            "([TipoTransaccion] = 1 AND [IngresoId] IS NOT NULL AND [EgresoId] IS NULL)"));

        Seed(modelBuilder);
    }

    private static void Seed(ModelBuilder mb)
    {
        mb.Entity<TipoEgreso>().HasData(
            new TipoEgreso { Id = 1, Descripcion = "Gasto" },
            new TipoEgreso { Id = 2, Descripcion = "Inversión" },
            new TipoEgreso { Id = 3, Descripcion = "Costo" });

        mb.Entity<TipoIngreso>().HasData(
            new TipoIngreso { Id = 1, Descripcion = "Salario Base" },
            new TipoIngreso { Id = 2, Descripcion = "Horas Extras" },
            new TipoIngreso { Id = 3, Descripcion = "Comisiones" },
            new TipoIngreso { Id = 4, Descripcion = "Bonificación" });

        mb.Entity<RenglonEgreso>().HasData(
            new RenglonEgreso { Id = 1, Descripcion = "Comida" },
            new RenglonEgreso { Id = 2, Descripcion = "Combustible" },
            new RenglonEgreso { Id = 3, Descripcion = "Recreación" },
            new RenglonEgreso { Id = 4, Descripcion = "Servicios" });

        mb.Entity<TipoPago>().HasData(
            new TipoPago { Id = 1, Descripcion = "Efectivo" },
            new TipoPago { Id = 2, Descripcion = "Tarjeta de Crédito" },
            new TipoPago { Id = 3, Descripcion = "Tarjeta de Débito" },
            new TipoPago { Id = 4, Descripcion = "Cheque" },
            new TipoPago { Id = 5, Descripcion = "Transferencia" });

        mb.Entity<Egreso>().HasData(
            new Egreso { Id = 1, Descripcion = "Compra Supermercado", TipoEgresoId = 1, RenglonEgresoId = 1, TipoPagoDefectoId = 2 },
            new Egreso { Id = 2, Descripcion = "Compra colmado", TipoEgresoId = 1, RenglonEgresoId = 1, TipoPagoDefectoId = 1 },
            new Egreso { Id = 3, Descripcion = "Recarga combustible", TipoEgresoId = 1, RenglonEgresoId = 2, TipoPagoDefectoId = 3 },
            new Egreso { Id = 4, Descripcion = "Salida al cine", TipoEgresoId = 1, RenglonEgresoId = 3, TipoPagoDefectoId = 2 });

        mb.Entity<Ingreso>().HasData(
            new Ingreso { Id = 1, Descripcion = "Salario Base Unapec", TipoIngresoId = 1, Institucion = "Universidad APEC" },
            new Ingreso { Id = 2, Descripcion = "Salario Base Consultora AXP", TipoIngresoId = 1, Institucion = "Consultora AXP" },
            new Ingreso { Id = 3, Descripcion = "Bonificación Consultora", TipoIngresoId = 4, Institucion = "Consultora AXP" });

        mb.Entity<Usuario>().HasData(
            // Las contraseñas iniciales se asignan al arrancar (CuentaService.InicializarCuentasAsync):
            // admin@finanzas.com / Admin123!  y  demo@finanzas.com / Demo123!
            new Usuario { Id = 1, Nombre = "Usuario Demo", Email = "demo@finanzas.com", Rol = Rol.Usuario, Cedula = "00100000009", LimiteEgresos = 25000m, TipoPersona = TipoPersona.Fisica, DiaCorte = 28 },
            new Usuario { Id = 2, Nombre = "Administrador", Email = "admin@finanzas.com", Rol = Rol.Administrador, Cedula = "00100000017", LimiteEgresos = 0m, TipoPersona = TipoPersona.Fisica, DiaCorte = 28 });

        var registro = new DateTime(2026, 9, 30, 12, 0, 0);
        mb.Entity<Transaccion>().HasData(
            new Transaccion { Id = 1, TipoTransaccion = TipoTransaccion.Ingreso, UsuarioId = 1, IngresoId = 1, TipoPagoId = 5, FechaTransaccion = new DateTime(2026, 9, 1), FechaRegistro = registro, Monto = 45000m },
            new Transaccion { Id = 2, TipoTransaccion = TipoTransaccion.Ingreso, UsuarioId = 1, IngresoId = 2, TipoPagoId = 5, FechaTransaccion = new DateTime(2026, 9, 15), FechaRegistro = registro, Monto = 30000m },
            new Transaccion { Id = 3, TipoTransaccion = TipoTransaccion.Egreso, UsuarioId = 1, EgresoId = 1, TipoPagoId = 2, FechaTransaccion = new DateTime(2026, 9, 5), FechaRegistro = registro, Monto = 8500m, NoTarjeta = "4321" },
            new Transaccion { Id = 4, TipoTransaccion = TipoTransaccion.Egreso, UsuarioId = 1, EgresoId = 3, TipoPagoId = 3, FechaTransaccion = new DateTime(2026, 9, 10), FechaRegistro = registro, Monto = 3200m, NoTarjeta = "8765" },
            new Transaccion { Id = 5, TipoTransaccion = TipoTransaccion.Egreso, UsuarioId = 1, EgresoId = 2, TipoPagoId = 1, FechaTransaccion = new DateTime(2026, 9, 20), FechaRegistro = registro, Monto = 1250m, Comentario = "Compra semanal" });
    }
}
