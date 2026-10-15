using Sipitex.Application.Seguridad;

namespace Sipitex.Tests;

public class RedactorDatosSensiblesTests
{
    [Theory]
    [InlineData("Password")]
    [InlineData("PasswordHash")]
    [InlineData("Token")]
    [InlineData("Codigo")]
    [InlineData("VerificationCode")]
    public void PropiedadesDeClave_SonSensibles(string nombre)
    {
        Assert.True(RedactorDatosSensibles.EsPropiedadSensible(nombre));
    }

    [Fact]
    public void NombreDeUsuario_NoEsSensible()
    {
        Assert.False(RedactorDatosSensibles.EsPropiedadSensible("Nombre"));
    }

    [Fact]
    public void Limpiar_OcultaClaveTokenYCodigo()
    {
        var texto = "password=Secreta1 token: abcdef codigo=123456 Nombre=Ana";
        var limpio = RedactorDatosSensibles.Limpiar(texto);

        Assert.DoesNotContain("Secreta1", limpio);
        Assert.DoesNotContain("abcdef", limpio);
        Assert.DoesNotContain("123456", limpio);
        Assert.Contains("Nombre=Ana", limpio);
        Assert.Contains("password=[omitido]", limpio);
    }
}
