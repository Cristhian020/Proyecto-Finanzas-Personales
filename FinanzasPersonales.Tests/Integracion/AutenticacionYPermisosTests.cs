using System.Net;
using FinanzasPersonales.Models;
using FinanzasPersonales.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using static FinanzasPersonales.Tests.Infraestructura.AplicacionDePrueba;

namespace FinanzasPersonales.Tests.Integracion;

/// <summary>Inicio de sesión, registro, roles y protección de las páginas.</summary>
public class AutenticacionYPermisosTests(AplicacionDePrueba app) : IClassFixture<AplicacionDePrueba>
{
    [Theory]
    [InlineData("/")]
    [InlineData("/Transacciones")]
    [InlineData("/Cortes/Procesar")]
    [InlineData("/Usuarios")]
    public async Task SinSesion_RedirigeAlLogin(string url)
    {
        var r = await app.NuevoCliente().GetAsync(url);
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Contains("/Cuenta/Login", r.Destino());
    }

    [Theory]
    [InlineData("/Cuenta/Login")]
    [InlineData("/Cuenta/Registro")]
    public async Task PaginasDeLoginYRegistro_SonPublicas(string url)
    {
        var r = await app.NuevoCliente().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Login_ConPasswordIncorrecta_MuestraError()
    {
        var r = await app.NuevoCliente().IniciarSesionAsync(DemoEmail, "incorrecta");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("Correo o contraseña incorrectos", await r.LeerAsync());
    }

    [Fact]
    public async Task Login_Usuario_VePanelPersonalSinMenuDeAdministracion()
    {
        var cliente = await app.NuevoCliente().ConSesionAsync(DemoEmail, DemoPassword);
        var html = await cliente.GetHtmlAsync("/");

        Assert.Contains("Tu periodo actual", html);
        Assert.Contains("Usuario Demo", html);
        Assert.DoesNotContain("Administración", html);
    }

    [Fact]
    public async Task Login_Administrador_VePanelGlobalYMenuDeAdministracion()
    {
        var cliente = await app.NuevoCliente().ConSesionAsync(AdminEmail, AdminPassword);
        var html = await cliente.GetHtmlAsync("/");

        Assert.Contains("Resumen general del mes", html);
        Assert.Contains("Administración", html);
        Assert.Contains("Usuarios activos", html);
    }

    [Theory]
    [InlineData("/Usuarios")]
    [InlineData("/Usuarios/Create")]
    [InlineData("/Usuarios/Edit/1")]
    [InlineData("/Egresos")]
    [InlineData("/Egresos/Create")]
    [InlineData("/Ingresos")]
    [InlineData("/TiposEgreso")]
    [InlineData("/TiposIngreso")]
    [InlineData("/RenglonesEgreso")]
    [InlineData("/TiposPago/Edit/1")]
    public async Task Usuario_NoPuedeEntrarASeccionesDeAdministrador(string url)
    {
        var cliente = await app.NuevoCliente().ConSesionAsync(DemoEmail, DemoPassword);
        var r = await cliente.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Contains("/Cuenta/AccesoDenegado", r.Destino());
    }

    [Fact]
    public async Task Usuario_NoPuedeModificarCatalogosEnviandoElFormularioDirecto()
    {
        var cliente = await app.NuevoCliente().ConSesionAsync(DemoEmail, DemoPassword);
        var r = await cliente.EnviarFormularioAsync("/Transacciones/Create", "/TiposPago/Save",
            new Dictionary<string, string> { ["Id"] = "0", ["Descripcion"] = "Hackeo", ["Estado"] = "true" });

        Assert.Contains("/Cuenta/AccesoDenegado", r.Destino());
        await using var db = app.CrearContexto();
        Assert.False(await db.TiposPago.AnyAsync(t => t.Descripcion == "Hackeo"));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/TiposEgreso")]
    [InlineData("/TiposEgreso/Create")]
    [InlineData("/TiposEgreso/Edit/1")]
    [InlineData("/TiposIngreso")]
    [InlineData("/RenglonesEgreso")]
    [InlineData("/TiposPago")]
    [InlineData("/Egresos")]
    [InlineData("/Egresos/Create")]
    [InlineData("/Egresos/Edit/1")]
    [InlineData("/Ingresos")]
    [InlineData("/Ingresos/Create")]
    [InlineData("/Ingresos/Edit/1")]
    [InlineData("/Usuarios")]
    [InlineData("/Usuarios/Create")]
    [InlineData("/Usuarios/Edit/1")]
    [InlineData("/Transacciones")]
    [InlineData("/Transacciones/Create")]
    [InlineData("/Consulta")]
    [InlineData("/Consulta?buscar=true")]
    [InlineData("/Cortes")]
    [InlineData("/Cortes/Procesar")]
    [InlineData("/Cortes/Reporte")]
    [InlineData("/Cuenta/Perfil")]
    [InlineData("/Cuenta/CambiarPassword")]
    public async Task Administrador_PuedeAbrirTodasLasPaginas(string url)
    {
        var cliente = await app.NuevoCliente().ConSesionAsync(AdminEmail, AdminPassword);
        var r = await cliente.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Transacciones")]
    [InlineData("/Transacciones/Create")]
    [InlineData("/Consulta?buscar=true")]
    [InlineData("/Cortes")]
    [InlineData("/Cortes/Procesar")]
    [InlineData("/Cortes/Reporte")]
    [InlineData("/Cuenta/Perfil")]
    [InlineData("/Cuenta/CambiarPassword")]
    public async Task Usuario_PuedeAbrirSusPaginas(string url)
    {
        var cliente = await app.NuevoCliente().ConSesionAsync(DemoEmail, DemoPassword);
        var r = await cliente.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task FormularioSinTokenAntiforgery_EsRechazado()
    {
        var cliente = await app.NuevoCliente().ConSesionAsync(DemoEmail, DemoPassword);
        var r = await cliente.PostAsync("/Transacciones/Save", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["TipoTransaccion"] = "Egreso", ["EgresoId"] = "1", ["TipoPagoId"] = "1", ["Monto"] = "10",
            ["FechaTransaccion"] = DateTime.Today.ToString("yyyy-MM-dd")
        }));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Registro_CreaCuentaConRolUsuarioEIniciaSesion()
    {
        var (cliente, email) = await app.RegistrarUsuarioAsync(limite: 5_000m, diaCorte: 15);

        var html = await cliente.GetHtmlAsync("/");
        Assert.Contains("Tu cuenta fue creada", html);

        await using var db = app.CrearContexto();
        var u = await db.Usuarios.SingleAsync(x => x.Email == email);
        Assert.Equal(Rol.Usuario, u.Rol);
        Assert.Equal(15, u.DiaCorte);
        Assert.Equal(5_000m, u.LimiteEgresos);
        Assert.NotNull(u.PasswordHash);
        Assert.DoesNotContain("Prueba123!", u.PasswordHash);
    }

    [Fact]
    public async Task Registro_NoPermiteElegirRolAdministrador()
    {
        var cliente = app.NuevoCliente();
        var email = Datos.NuevoEmail();
        await cliente.EnviarFormularioAsync("/Cuenta/Registro", "/Cuenta/Registro", new Dictionary<string, string>
        {
            ["Nombre"] = "Intruso", ["Email"] = email, ["TipoPersona"] = "Fisica", ["Cedula"] = Datos.NuevaCedula(),
            ["LimiteEgresos"] = "0", ["DiaCorte"] = "28", ["Password"] = "Prueba123!", ["ConfirmarPassword"] = "Prueba123!",
            ["Rol"] = "Administrador"
        });

        await using var db = app.CrearContexto();
        Assert.Equal(Rol.Usuario, (await db.Usuarios.SingleAsync(x => x.Email == email)).Rol);
    }

    [Fact]
    public async Task Registro_RechazaCedulaInvalidaYCorreoDuplicado()
    {
        var r = await app.NuevoCliente().EnviarFormularioAsync("/Cuenta/Registro", "/Cuenta/Registro", new Dictionary<string, string>
        {
            ["Nombre"] = "Repetido", ["Email"] = DemoEmail, ["TipoPersona"] = "Fisica", ["Cedula"] = "001-0000000-1",
            ["LimiteEgresos"] = "0", ["DiaCorte"] = "28", ["Password"] = "Prueba123!", ["ConfirmarPassword"] = "Prueba123!"
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var html = await r.LeerAsync();
        Assert.Contains("Ya existe una cuenta con ese correo", html);
        Assert.Contains("La cédula no es válida", html);
    }

    [Fact]
    public async Task CerrarSesion_TerminaLaSesion()
    {
        var cliente = await app.NuevoCliente().ConSesionAsync(DemoEmail, DemoPassword);
        var r = await cliente.EnviarFormularioAsync("/", "/Cuenta/Logout", new Dictionary<string, string>());
        Assert.Contains("/Cuenta/Login", r.Destino());

        var despues = await cliente.GetAsync("/");
        Assert.Contains("/Cuenta/Login", despues.Destino());
    }

    [Fact]
    public async Task CambiarPassword_ExigeLaActualYPermiteEntrarConLaNueva()
    {
        var (cliente, email) = await app.RegistrarUsuarioAsync();

        var mal = await cliente.EnviarFormularioAsync("/Cuenta/CambiarPassword", "/Cuenta/CambiarPassword",
            new Dictionary<string, string> { ["Actual"] = "equivocada", ["Nueva"] = "Nueva456!", ["Confirmar"] = "Nueva456!" });
        Assert.Contains("La contraseña actual no es correcta", await mal.LeerAsync());

        var bien = await cliente.EnviarFormularioAsync("/Cuenta/CambiarPassword", "/Cuenta/CambiarPassword",
            new Dictionary<string, string> { ["Actual"] = "Prueba123!", ["Nueva"] = "Nueva456!", ["Confirmar"] = "Nueva456!" });
        Assert.Equal(HttpStatusCode.Redirect, bien.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await app.NuevoCliente().IniciarSesionAsync(email, "Prueba123!")).StatusCode); // la vieja ya no sirve
        Assert.Equal(HttpStatusCode.Redirect, (await app.NuevoCliente().IniciarSesionAsync(email, "Nueva456!")).StatusCode);
    }

    [Fact]
    public async Task Administrador_NoPuedeInactivarSuPropiaCuenta()
    {
        var cliente = await app.NuevoCliente().ConSesionAsync(AdminEmail, AdminPassword);
        var r = await cliente.EnviarFormularioAsync("/Usuarios", "/Usuarios/CambiarEstado/2", new Dictionary<string, string>());

        Assert.Contains("No puede inactivar su propia cuenta", await cliente.SeguirAsync(r));
        await using var db = app.CrearContexto();
        Assert.True((await db.Usuarios.FindAsync(2))!.Estado);
    }

    [Fact]
    public async Task UsuarioInactivadoPorElAdministrador_PierdeLaSesionYNoPuedeEntrar()
    {
        var (usuario, email) = await app.RegistrarUsuarioAsync();
        Assert.Equal(HttpStatusCode.OK, (await usuario.GetAsync("/")).StatusCode);

        await using (var db = app.CrearContexto())
        {
            var id = (await db.Usuarios.SingleAsync(u => u.Email == email)).Id;
            var admin = await app.NuevoCliente().ConSesionAsync(AdminEmail, AdminPassword);
            await admin.EnviarFormularioAsync("/Usuarios", $"/Usuarios/CambiarEstado/{id}", new Dictionary<string, string>());
        }

        var r = await usuario.GetAsync("/");
        Assert.Contains("/Cuenta/Login", r.Destino());
        Assert.Equal(HttpStatusCode.OK, (await app.NuevoCliente().IniciarSesionAsync(email, "Prueba123!")).StatusCode);
    }

    [Fact]
    public async Task Administrador_PuedeCrearOtroAdministrador()
    {
        var admin = await app.NuevoCliente().ConSesionAsync(AdminEmail, AdminPassword);
        var email = Datos.NuevoEmail();
        var r = await admin.EnviarFormularioAsync("/Usuarios/Create", "/Usuarios/Save", new Dictionary<string, string>
        {
            ["Id"] = "0", ["Nombre"] = "Segundo Admin", ["Email"] = email, ["Cedula"] = Datos.NuevaCedula(),
            ["TipoPersona"] = "Fisica", ["LimiteEgresos"] = "0", ["DiaCorte"] = "28", ["Rol"] = "Administrador",
            ["NuevaPassword"] = "Admin456!", ["Estado"] = "true"
        });
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);

        var nuevo = await app.NuevoCliente().ConSesionAsync(email, "Admin456!");
        Assert.Equal(HttpStatusCode.OK, (await nuevo.GetAsync("/Usuarios")).StatusCode);
    }
}
