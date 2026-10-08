using System.ComponentModel.DataAnnotations;
using FinanzasPersonales.Models;

namespace FinanzasPersonales.Tests.Unitarias;

/// <summary>Reglas de validación de los modelos (las mismas que aplica MVC al recibir un formulario).</summary>
public class ModelosTests
{
    private static List<ValidationResult> Validar(object modelo)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(modelo, new ValidationContext(modelo), resultados, validateAllProperties: true);
        return resultados;
    }

    private static bool TieneError(List<ValidationResult> r, string campo) =>
        r.Any(x => x.MemberNames.Contains(campo));

    private static Transaccion EgresoValido() => new()
    {
        TipoTransaccion = TipoTransaccion.Egreso,
        UsuarioId = 1,
        EgresoId = 1,
        TipoPagoId = 1,
        Monto = 100m,
        FechaTransaccion = DateTime.Today
    };

    [Fact]
    public void TransaccionCompleta_EsValida() => Assert.Empty(Validar(EgresoValido()));

    [Fact]
    public void Egreso_SinGasto_NoEsValido()
    {
        var t = EgresoValido();
        t.EgresoId = null;
        Assert.True(TieneError(Validar(t), nameof(Transaccion.EgresoId)));
    }

    [Fact]
    public void Ingreso_SinIngreso_NoEsValido()
    {
        var t = EgresoValido();
        t.TipoTransaccion = TipoTransaccion.Ingreso;
        t.EgresoId = null;
        Assert.True(TieneError(Validar(t), nameof(Transaccion.IngresoId)));
    }

    [Fact]
    public void Transaccion_ConFechaFutura_NoEsValida()
    {
        var t = EgresoValido();
        t.FechaTransaccion = DateTime.Today.AddDays(1);
        Assert.True(TieneError(Validar(t), nameof(Transaccion.FechaTransaccion)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Transaccion_ConMontoCeroONegativo_NoEsValida(decimal monto)
    {
        var t = EgresoValido();
        t.Monto = monto;
        Assert.True(TieneError(Validar(t), nameof(Transaccion.Monto)));
    }

    [Theory]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("abcd")]
    public void Tarjeta_DebenSerExactamente4Digitos(string tarjeta)
    {
        var t = EgresoValido();
        t.NoTarjeta = tarjeta;
        Assert.True(TieneError(Validar(t), nameof(Transaccion.NoTarjeta)));
    }

    private static Usuario UsuarioValido() => new()
    {
        Nombre = "Ana",
        Email = "ana@test.local",
        Cedula = "001-0000000-9",
        TipoPersona = TipoPersona.Fisica,
        DiaCorte = 15,
        LimiteEgresos = 1000m
    };

    [Fact]
    public void UsuarioCompleto_EsValido() => Assert.Empty(Validar(UsuarioValido()));

    [Fact]
    public void PersonaFisica_ConCedulaInvalida_NoEsValida()
    {
        var u = UsuarioValido();
        u.Cedula = "001-0000000-1";
        Assert.True(TieneError(Validar(u), nameof(Usuario.Cedula)));
    }

    [Fact]
    public void PersonaJuridica_ExigeRnc()
    {
        var u = UsuarioValido();
        u.TipoPersona = TipoPersona.Juridica;
        Assert.True(TieneError(Validar(u), nameof(Usuario.Cedula))); // una cédula no sirve como RNC

        u.Cedula = "1-01-00000-7";
        Assert.Empty(Validar(u));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(29)]
    [InlineData(31)]
    public void DiaDeCorte_DebeEstarEntre1y28(int dia)
    {
        var u = UsuarioValido();
        u.DiaCorte = dia;
        Assert.True(TieneError(Validar(u), nameof(Usuario.DiaCorte)));
    }

    [Fact]
    public void Usuario_ConCorreoInvalido_NoEsValido()
    {
        var u = UsuarioValido();
        u.Email = "no-es-correo";
        Assert.True(TieneError(Validar(u), nameof(Usuario.Email)));
    }

    [Fact]
    public void Registro_ConContrasenasDistintas_NoEsValido()
    {
        var vm = new RegistroViewModel
        {
            Nombre = "Ana", Email = "ana@test.local", Cedula = "00100000009", DiaCorte = 28,
            Password = "Prueba123!", ConfirmarPassword = "Otra123!"
        };
        Assert.True(TieneError(Validar(vm), nameof(RegistroViewModel.ConfirmarPassword)));
    }

    [Fact]
    public void Registro_ConContrasenaCorta_NoEsValido()
    {
        var vm = new RegistroViewModel
        {
            Nombre = "Ana", Email = "ana@test.local", Cedula = "00100000009", DiaCorte = 28,
            Password = "123", ConfirmarPassword = "123"
        };
        Assert.True(TieneError(Validar(vm), nameof(RegistroViewModel.Password)));
    }

    [Theory]
    [InlineData("Tarjeta de Crédito", true)]
    [InlineData("TARJETA de débito", true)]
    [InlineData("Efectivo", false)]
    [InlineData("Cheque", false)]
    public void TipoPago_DetectaSiEsTarjeta(string descripcion, bool esTarjeta) =>
        Assert.Equal(esTarjeta, new TipoPago { Descripcion = descripcion }.EsTarjeta);
}
