using FinanzasPersonales.Validation;

namespace FinanzasPersonales.Tests.Unitarias;

public class DocumentoIdentidadTests
{
    [Theory]
    [InlineData("00100000009")]
    [InlineData("00100000017")]
    [InlineData("00100000025")]
    public void CedulaConDigitoVerificadorCorrecto_EsValida(string cedula) =>
        Assert.True(DocumentoIdentidad.CedulaValida(cedula));

    [Theory]
    [InlineData("00100000001")] // dígito verificador incorrecto
    [InlineData("00100000018")] // dígito verificador incorrecto
    [InlineData("0010000000")]  // 10 dígitos
    [InlineData("001000000099")] // 12 dígitos
    [InlineData("")]
    public void CedulaIncorrecta_NoEsValida(string cedula) =>
        Assert.False(DocumentoIdentidad.CedulaValida(cedula));

    [Fact]
    public void CedulasGeneradasParaPruebas_SonValidas()
    {
        for (int i = 0; i < 50; i++)
            Assert.True(DocumentoIdentidad.CedulaValida(Tests.Infraestructura.Datos.NuevaCedula()));
    }

    [Theory]
    [InlineData("101000007")]
    public void RncConDigitoVerificadorCorrecto_EsValido(string rnc) =>
        Assert.True(DocumentoIdentidad.RncValido(rnc));

    [Theory]
    [InlineData("101000001")]
    [InlineData("10100000")]
    [InlineData("00100000009")] // una cédula no es un RNC
    public void RncIncorrecto_NoEsValido(string rnc) =>
        Assert.False(DocumentoIdentidad.RncValido(rnc));

    [Theory]
    [InlineData("001-0000000-9", "00100000009")]
    [InlineData(" 001 0000000 9 ", "00100000009")]
    [InlineData(null, "")]
    public void Limpiar_DejaSoloLosDigitos(string? entrada, string esperado) =>
        Assert.Equal(esperado, DocumentoIdentidad.Limpiar(entrada));

    [Theory]
    [InlineData("00100000009", "001-0000000-9")]
    [InlineData("101000007", "1-01-00000-7")]
    [InlineData("123", "123")]
    public void Formatear_AplicaGuionesSegunLongitud(string valor, string esperado) =>
        Assert.Equal(esperado, DocumentoIdentidad.Formatear(valor));
}
