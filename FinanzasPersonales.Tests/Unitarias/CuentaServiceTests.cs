using FinanzasPersonales.Models;
using FinanzasPersonales.Services;
using FinanzasPersonales.Tests.Infraestructura;

namespace FinanzasPersonales.Tests.Unitarias;

public class CuentaServiceTests(BaseDeDatosFixture bd) : IClassFixture<BaseDeDatosFixture>
{
    [Fact]
    public void Password_SeGuardaConHashYSeVerifica()
    {
        using var db = bd.CrearContexto();
        var servicio = new CuentaService(db);
        var u = new Usuario();

        servicio.EstablecerPassword(u, "MiClave123");

        Assert.NotNull(u.PasswordHash);
        Assert.DoesNotContain("MiClave123", u.PasswordHash);
        Assert.True(servicio.VerificarPassword(u, "MiClave123"));
        Assert.False(servicio.VerificarPassword(u, "miclave123"));
        Assert.False(servicio.VerificarPassword(new Usuario(), "MiClave123")); // sin hash
    }

    [Fact]
    public void MismaPassword_GeneraHashesDistintos()
    {
        using var db = bd.CrearContexto();
        var servicio = new CuentaService(db);
        var a = new Usuario();
        var b = new Usuario();
        servicio.EstablecerPassword(a, "Igual123");
        servicio.EstablecerPassword(b, "Igual123");
        Assert.NotEqual(a.PasswordHash, b.PasswordHash); // se usa sal aleatoria
    }

    private async Task<Usuario> CrearConPassword(string password, bool activo = true)
    {
        await using var db = bd.CrearContexto();
        var u = new Usuario { Nombre = "X", Email = Datos.NuevoEmail(), Cedula = Datos.NuevaCedula(), Estado = activo };
        new CuentaService(db).EstablecerPassword(u, password);
        db.Usuarios.Add(u);
        await db.SaveChangesAsync();
        return u;
    }

    [Fact]
    public async Task Credenciales_CorrectasDevuelvenElUsuario_SinImportarMayusculasDelCorreo()
    {
        var u = await CrearConPassword("Clave123");
        await using var db = bd.CrearContexto();

        var encontrado = await new CuentaService(db).ValidarCredencialesAsync("  " + u.Email.ToUpper() + " ", "Clave123");

        Assert.Equal(u.Id, encontrado?.Id);
    }

    [Fact]
    public async Task Credenciales_Incorrectas_DevuelvenNull()
    {
        var u = await CrearConPassword("Clave123");
        await using var db = bd.CrearContexto();
        var servicio = new CuentaService(db);

        Assert.Null(await servicio.ValidarCredencialesAsync(u.Email, "otra"));
        Assert.Null(await servicio.ValidarCredencialesAsync("nadie@test.local", "Clave123"));
    }

    [Fact]
    public async Task CuentaInactiva_NoPuedeIniciarSesion()
    {
        var u = await CrearConPassword("Clave123", activo: false);
        await using var db = bd.CrearContexto();

        Assert.Null(await new CuentaService(db).ValidarCredencialesAsync(u.Email, "Clave123"));
    }

    [Fact]
    public async Task CuentasSembradas_RecibenSuPasswordInicial()
    {
        await using var db = bd.CrearContexto();
        var servicio = new CuentaService(db);
        await servicio.InicializarCuentasAsync();

        Assert.NotNull(await servicio.ValidarCredencialesAsync("admin@finanzas.com", "Admin123!"));
        Assert.NotNull(await servicio.ValidarCredencialesAsync("demo@finanzas.com", "Demo123!"));
    }
}
