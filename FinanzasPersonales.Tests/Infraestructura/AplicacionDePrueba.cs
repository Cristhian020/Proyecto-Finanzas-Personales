using System.Net;
using System.Text.RegularExpressions;
using FinanzasPersonales.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinanzasPersonales.Tests.Infraestructura;

/// <summary>
/// Arranca la aplicación completa en memoria (mismo Program.cs, middleware, autenticación y vistas)
/// apuntando a una base de datos LocalDB temporal.
/// </summary>
public class AplicacionDePrueba : WebApplicationFactory<Program>, IAsyncLifetime
{
    public string Cadena { get; } = BaseDeDatosDePrueba.NuevaCadenaConexion();

    public const string AdminEmail = "admin@finanzas.com";
    public const string AdminPassword = "Admin123!";
    public const string DemoEmail = "demo@finanzas.com";
    public const string DemoPassword = "Demo123!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<FinanzasContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<FinanzasContext>>();
            services.AddDbContext<FinanzasContext>(o => o.UseSqlServer(Cadena));
        });
    }

    public HttpClient NuevoCliente() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    public FinanzasContext CrearContexto() => BaseDeDatosDePrueba.CrearContexto(Cadena);

    public Task InitializeAsync()
    {
        _ = Server; // fuerza el arranque: aplica migraciones y crea las cuentas demo
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await BaseDeDatosDePrueba.EliminarAsync(Cadena);
    }
}

public static partial class ClienteExtensiones
{
    [GeneratedRegex(@"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""")]
    private static partial Regex TokenRegex();

    public static async Task<string> ObtenerTokenAsync(this HttpClient cliente, string urlFormulario)
    {
        var html = await cliente.GetStringAsync(urlFormulario);
        var m = TokenRegex().Match(html);
        Assert.True(m.Success, $"No se encontró el token antiforgery en {urlFormulario}");
        return WebUtility.HtmlDecode(m.Groups[1].Value);
    }

    /// <summary>Envía un formulario incluyendo el token antiforgery obtenido de la página del formulario.</summary>
    public static async Task<HttpResponseMessage> EnviarFormularioAsync(this HttpClient cliente, string urlFormulario,
        string urlAccion, IDictionary<string, string> campos)
    {
        var token = await cliente.ObtenerTokenAsync(urlFormulario);
        var datos = new Dictionary<string, string>(campos) { ["__RequestVerificationToken"] = token };
        return await cliente.PostAsync(urlAccion, new FormUrlEncodedContent(datos));
    }

    public static async Task<HttpResponseMessage> IniciarSesionAsync(this HttpClient cliente, string email, string password)
    {
        var r = await cliente.EnviarFormularioAsync("/Cuenta/Login", "/Cuenta/Login",
            new Dictionary<string, string> { ["Email"] = email, ["Password"] = password });
        return r;
    }

    public static async Task<HttpClient> ConSesionAsync(this HttpClient cliente, string email, string password)
    {
        var r = await cliente.IniciarSesionAsync(email, password);
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        return cliente;
    }

    /// <summary>Descarga una página y devuelve su HTML decodificado (Razor codifica tildes y eñes).</summary>
    public static async Task<string> GetHtmlAsync(this HttpClient cliente, string url) =>
        WebUtility.HtmlDecode(await cliente.GetStringAsync(url));

    /// <summary>Lee el HTML de una respuesta, decodificado.</summary>
    public static async Task<string> LeerAsync(this HttpResponseMessage respuesta) =>
        WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

    /// <summary>Sigue una redirección y devuelve el HTML (decodificado) de la página destino.</summary>
    public static async Task<string> SeguirAsync(this HttpClient cliente, HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        return WebUtility.HtmlDecode(await cliente.GetStringAsync(respuesta.Headers.Location));
    }

    public static string Destino(this HttpResponseMessage r) => r.Headers.Location?.OriginalString ?? "";

    /// <summary>Registra una cuenta nueva (rol Usuario) y deja la sesión iniciada.</summary>
    public static async Task<(HttpClient Cliente, string Email)> RegistrarUsuarioAsync(this AplicacionDePrueba app, decimal limite = 0m, int diaCorte = 28)
    {
        var cliente = app.NuevoCliente();
        var email = Datos.NuevoEmail();
        var r = await cliente.EnviarFormularioAsync("/Cuenta/Registro", "/Cuenta/Registro", new Dictionary<string, string>
        {
            ["Nombre"] = "Persona Prueba",
            ["Email"] = email,
            ["TipoPersona"] = "Fisica",
            ["Cedula"] = Datos.NuevaCedula(),
            ["LimiteEgresos"] = limite.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["DiaCorte"] = diaCorte.ToString(),
            ["Password"] = "Prueba123!",
            ["ConfirmarPassword"] = "Prueba123!"
        });
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        return (cliente, email);
    }
}
