using System.Net;
using FinanzasPersonales.Models;
using FinanzasPersonales.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using static FinanzasPersonales.Tests.Infraestructura.AplicacionDePrueba;

namespace FinanzasPersonales.Tests.Integracion;

/// <summary>Registro de transacciones, aislamiento entre usuarios, aviso de límite, consulta y corte mensual.</summary>
public class TransaccionesYCorteTests(AplicacionDePrueba app) : IClassFixture<AplicacionDePrueba>
{
    private static readonly string Hoy = DateTime.Today.ToString("yyyy-MM-dd");

    private static Dictionary<string, string> Egreso(string monto, string tipoPago = "1", string? tarjeta = null,
        string? fecha = null, string? comentario = null, string? usuarioId = null)
    {
        var d = new Dictionary<string, string>
        {
            ["Id"] = "0", ["Estado"] = "true", ["TipoTransaccion"] = "Egreso", ["EgresoId"] = "2",
            ["TipoPagoId"] = tipoPago, ["Monto"] = monto, ["FechaTransaccion"] = fecha ?? Hoy
        };
        if (tarjeta is not null) d["NoTarjeta"] = tarjeta;
        if (comentario is not null) d["Comentario"] = comentario;
        if (usuarioId is not null) d["UsuarioId"] = usuarioId;
        return d;
    }

    private static Task<HttpResponseMessage> Guardar(HttpClient c, Dictionary<string, string> campos) =>
        c.EnviarFormularioAsync("/Transacciones/Create", "/Transacciones/Save", campos);

    private async Task<int> IdDe(string email)
    {
        await using var db = app.CrearContexto();
        return (await db.Usuarios.SingleAsync(u => u.Email == email)).Id;
    }

    [Fact]
    public async Task Usuario_RegistraUnEgreso_YApareceEnSuListado()
    {
        var (cliente, email) = await app.RegistrarUsuarioAsync();
        var r = await Guardar(cliente, Egreso("1234.50", comentario: "Compra de prueba"));

        var html = await cliente.SeguirAsync(r);
        Assert.Contains("guardada correctamente", html);
        Assert.Contains("RD$1,234.50", html);

        await using var db = app.CrearContexto();
        var t = await db.Transacciones.SingleAsync(x => x.Comentario == "Compra de prueba");
        Assert.Equal(await IdDe(email), t.UsuarioId);
        Assert.Equal(1234.50m, t.Monto);
        Assert.Null(t.IngresoId);
        Assert.True((DateTime.Now - t.FechaRegistro).TotalMinutes < 5); // la fecha de registro la pone el sistema
    }

    [Fact]
    public async Task Usuario_NoPuedeRegistrarTransaccionesANombreDeOtro()
    {
        var (cliente, email) = await app.RegistrarUsuarioAsync();
        await Guardar(cliente, Egreso("50", comentario: "Intento suplantar", usuarioId: "1"));

        await using var db = app.CrearContexto();
        var t = await db.Transacciones.SingleAsync(x => x.Comentario == "Intento suplantar");
        Assert.Equal(await IdDe(email), t.UsuarioId); // se ignora el UsuarioId enviado
    }

    [Fact]
    public async Task Usuario_NoVeNiModificaTransaccionesNiCortesDeOtros()
    {
        var (cliente, _) = await app.RegistrarUsuarioAsync();

        // Las transacciones 1-5 son del usuario demo.
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/Transacciones/Edit/1")).StatusCode);
        var anular = await cliente.EnviarFormularioAsync("/Transacciones", "/Transacciones/CambiarEstado/1", new Dictionary<string, string>());
        Assert.Equal(HttpStatusCode.NotFound, anular.StatusCode);

        var listado = await cliente.GetHtmlAsync("/Transacciones");
        Assert.DoesNotContain("Salario Base Unapec", listado);

        var consulta = await cliente.GetHtmlAsync("/Consulta?buscar=true&Filtro.UsuarioId=1");
        Assert.DoesNotContain("Salario Base Unapec", consulta); // el filtro de usuario se ignora para no-admin
        Assert.Contains("0 resultado(s)", consulta);

        await using var db = app.CrearContexto();
        Assert.True((await db.Transacciones.FindAsync(1))!.Estado);
    }

    [Fact]
    public async Task Administrador_VeLasTransaccionesDeTodos()
    {
        var (usuario, _) = await app.RegistrarUsuarioAsync();
        await Guardar(usuario, Egreso("75", comentario: "Visible para admin"));

        var admin = await app.NuevoCliente().ConSesionAsync(AdminEmail, AdminPassword);
        var consulta = await admin.GetHtmlAsync("/Consulta?buscar=true");
        Assert.Contains("Salario Base Unapec", consulta); // del demo
        Assert.Contains("Visible para admin", consulta);   // del usuario nuevo
    }

    [Fact]
    public async Task PagoConTarjeta_ExigeLosUltimos4Digitos_YSoloGuardaEsos()
    {
        var (cliente, _) = await app.RegistrarUsuarioAsync();

        var sinTarjeta = await Guardar(cliente, Egreso("100", tipoPago: "2"));
        Assert.Equal(HttpStatusCode.OK, sinTarjeta.StatusCode);
        Assert.Contains("Indique los últimos 4 dígitos de la tarjeta", await sinTarjeta.LeerAsync());

        var conTarjeta = await Guardar(cliente, Egreso("100", tipoPago: "2", tarjeta: "4321", comentario: "Con tarjeta"));
        Assert.Equal(HttpStatusCode.Redirect, conTarjeta.StatusCode);

        // En efectivo no se guarda número de tarjeta aunque se envíe.
        await Guardar(cliente, Egreso("100", tipoPago: "1", tarjeta: "9999", comentario: "En efectivo"));

        await using var db = app.CrearContexto();
        Assert.Equal("4321", (await db.Transacciones.SingleAsync(t => t.Comentario == "Con tarjeta")).NoTarjeta);
        Assert.Null((await db.Transacciones.SingleAsync(t => t.Comentario == "En efectivo")).NoTarjeta);
    }

    [Fact]
    public async Task EgresoQueSuperaElLimiteMensual_MuestraAviso()
    {
        var (cliente, _) = await app.RegistrarUsuarioAsync(limite: 1_000m);

        var dentro = await cliente.SeguirAsync(await Guardar(cliente, Egreso("600")));
        Assert.DoesNotContain("¡Atención!", dentro);

        var fuera = await cliente.SeguirAsync(await Guardar(cliente, Egreso("500")));
        Assert.Contains("¡Atención!", fuera);
        Assert.Contains("RD$1,100.00", fuera);     // total gastado
        Assert.Contains("por RD$100.00", fuera);   // exceso

        var panel = await cliente.GetHtmlAsync("/");
        Assert.Contains("Superaste tu límite", panel);
    }

    [Fact]
    public async Task FechaFutura_EsRechazada()
    {
        var (cliente, _) = await app.RegistrarUsuarioAsync();
        var r = await Guardar(cliente, Egreso("10", fecha: DateTime.Today.AddDays(3).ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("no puede ser futura", await r.LeerAsync());
    }

    [Fact]
    public async Task EgresoSinGasto_MuestraErrorYNoSeGuarda()
    {
        var (cliente, email) = await app.RegistrarUsuarioAsync();
        var campos = Egreso("10");
        campos.Remove("EgresoId");

        var r = await Guardar(cliente, campos);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("Seleccione el gasto", await r.LeerAsync());
        await using var db = app.CrearContexto();
        var id = await IdDe(email);
        Assert.False(await db.Transacciones.AnyAsync(t => t.UsuarioId == id));
    }

    [Fact]
    public async Task TransaccionAnulada_NoCuentaEnLaConsulta()
    {
        var (cliente, _) = await app.RegistrarUsuarioAsync();
        await Guardar(cliente, Egreso("300", comentario: "Para anular"));

        int id;
        await using (var db = app.CrearContexto())
            id = (await db.Transacciones.SingleAsync(t => t.Comentario == "Para anular")).Id;

        var r = await cliente.EnviarFormularioAsync("/Transacciones", $"/Transacciones/CambiarEstado/{id}", new Dictionary<string, string>());
        Assert.Contains("anulada", await cliente.SeguirAsync(r));

        Assert.Contains("0 resultado(s)", await cliente.GetHtmlAsync("/Consulta?buscar=true"));
        Assert.Contains("1 resultado(s)", await cliente.GetHtmlAsync("/Consulta?buscar=true&Filtro.IncluirAnuladas=true"));
    }

    [Fact]
    public async Task Consulta_FiltraPorTipoYRenglon()
    {
        var admin = await app.NuevoCliente().ConSesionAsync(AdminEmail, AdminPassword);

        // Demo (semilla): egresos de comida = Supermercado 8,500 + Colmado 1,250.
        var comida = await admin.GetHtmlAsync("/Consulta?buscar=true&Filtro.UsuarioId=1&Filtro.TipoTransaccion=2&Filtro.RenglonEgresoId=1");
        Assert.Contains("2 resultado(s)", comida);
        Assert.Contains("RD$9,750.00", comida);

        var ingresos = await admin.GetHtmlAsync("/Consulta?buscar=true&Filtro.UsuarioId=1&Filtro.TipoTransaccion=1");
        Assert.Contains("2 resultado(s)", ingresos);
        Assert.Contains("RD$75,000.00", ingresos);
    }

    [Fact]
    public async Task CorteMensual_CalculaElBalance_YBloqueaLasTransaccionesDelPeriodo()
    {
        var demo = await app.NuevoCliente().ConSesionAsync(DemoEmail, DemoPassword);

        // El usuario demo procesa su corte de septiembre 2026 (datos semilla).
        var r = await demo.EnviarFormularioAsync("/Cortes/Procesar", "/Cortes/Procesar",
            new Dictionary<string, string> { ["Anio"] = "2026", ["Mes"] = "9" });
        var detalle = await demo.SeguirAsync(r);

        Assert.Contains("Corte de Septiembre 2026", detalle);
        Assert.Contains("RD$75,000.00", detalle); // ingresos
        Assert.Contains("RD$12,950.00", detalle); // egresos
        Assert.Contains("RD$62,050.00", detalle); // balance al corte

        await using (var db = app.CrearContexto())
        {
            var corte = await db.Cortes.SingleAsync(c => c.UsuarioId == 1 && c.Anio == 2026 && c.Mes == 9);
            Assert.Equal(62_050m, corte.BalanceAlCorte);
        }

        // Las transacciones del periodo ya no se pueden editar, anular ni agregar.
        var editar = await demo.GetAsync("/Transacciones/Edit/3");
        Assert.Contains("ya cortado", await demo.SeguirAsync(editar));

        var anular = await demo.EnviarFormularioAsync("/Transacciones", "/Transacciones/CambiarEstado/3", new Dictionary<string, string>());
        Assert.Contains("ya cortado", await demo.SeguirAsync(anular));

        var nueva = await Guardar(demo, Egreso("10", fecha: "2026-09-15"));
        Assert.Equal(HttpStatusCode.OK, nueva.StatusCode);
        Assert.Contains("periodo ya cortado", await nueva.LeerAsync());

        // Aparece en el listado y en el reporte del usuario.
        Assert.Contains("RD$62,050.00", await demo.GetHtmlAsync("/Cortes"));
        Assert.Contains("RD$62,050.00", await demo.GetHtmlAsync("/Cortes/Reporte"));
    }

    [Fact]
    public async Task Usuario_SoloProcesaSuPropioCorte()
    {
        var (cliente, email) = await app.RegistrarUsuarioAsync();
        var r = await cliente.EnviarFormularioAsync("/Cortes/Procesar", "/Cortes/Procesar",
            new Dictionary<string, string> { ["UsuarioId"] = "1", ["Anio"] = "2025", ["Mes"] = "1" });
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);

        await using var db = app.CrearContexto();
        var corte = await db.Cortes.SingleAsync(c => c.Anio == 2025 && c.Mes == 1);
        Assert.Equal(await IdDe(email), corte.UsuarioId);
        Assert.False(await db.Cortes.AnyAsync(c => c.UsuarioId == 1 && c.Anio == 2025));
    }

    [Fact]
    public async Task Corte_DePeriodoNoTerminado_MuestraError()
    {
        var (cliente, _) = await app.RegistrarUsuarioAsync();
        var futuro = DateTime.Today.AddMonths(2);
        var r = await cliente.EnviarFormularioAsync("/Cortes/Procesar", "/Cortes/Procesar",
            new Dictionary<string, string> { ["Anio"] = futuro.Year.ToString(), ["Mes"] = futuro.Month.ToString() });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("aún no ha terminado", await r.LeerAsync());
    }

    [Fact]
    public async Task Administrador_GestionaCatalogos()
    {
        var admin = await app.NuevoCliente().ConSesionAsync(AdminEmail, AdminPassword);

        var crear = await admin.EnviarFormularioAsync("/TiposPago/Create", "/TiposPago/Save",
            new Dictionary<string, string> { ["Id"] = "0", ["Descripcion"] = "Criptomoneda", ["Estado"] = "true" });
        Assert.Contains("Criptomoneda", await admin.SeguirAsync(crear));

        var duplicado = await admin.EnviarFormularioAsync("/TiposPago/Create", "/TiposPago/Save",
            new Dictionary<string, string> { ["Id"] = "0", ["Descripcion"] = "Criptomoneda", ["Estado"] = "true" });
        Assert.Contains("Ya existe un registro con esa descripción", await duplicado.LeerAsync());

        int id;
        await using (var db = app.CrearContexto())
            id = (await db.TiposPago.SingleAsync(t => t.Descripcion == "Criptomoneda")).Id;

        await admin.EnviarFormularioAsync("/TiposPago", $"/TiposPago/CambiarEstado/{id}", new Dictionary<string, string>());

        await using (var db = app.CrearContexto())
            Assert.False((await db.TiposPago.FindAsync(id))!.Estado);

        // Un tipo de pago inactivo ya no se ofrece al registrar transacciones.
        Assert.DoesNotContain("Criptomoneda", await admin.GetHtmlAsync("/Transacciones/Create"));
    }

    [Fact]
    public async Task Administrador_RegistraEgresoParaOtroUsuario()
    {
        var (_, email) = await app.RegistrarUsuarioAsync();
        var id = await IdDe(email);

        var admin = await app.NuevoCliente().ConSesionAsync(AdminEmail, AdminPassword);
        var r = await Guardar(admin, Egreso("80", comentario: "Registrado por admin", usuarioId: id.ToString()));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);

        await using var db = app.CrearContexto();
        Assert.Equal(id, (await db.Transacciones.SingleAsync(t => t.Comentario == "Registrado por admin")).UsuarioId);
    }

    [Fact]
    public async Task TipoPagoPorDefecto_SeDevuelveParaElGastoElegido()
    {
        var cliente = await app.NuevoCliente().ConSesionAsync(DemoEmail, DemoPassword);
        // "Compra Supermercado" (1) usa Tarjeta de Crédito (2); "Compra colmado" (2) usa Efectivo (1).
        Assert.Equal("2", await cliente.GetHtmlAsync("/Transacciones/TipoPagoDefecto?egresoId=1"));
        Assert.Equal("1", await cliente.GetHtmlAsync("/Transacciones/TipoPagoDefecto?egresoId=2"));
    }
}
