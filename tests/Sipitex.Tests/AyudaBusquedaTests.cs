using System.Security.Claims;
using Sipitex.Application.Ayuda;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class AyudaBusquedaTests
{
    [Theory]
    [InlineData("crear un usuario", "usuarios", "¿Cómo creo un usuario y sus permisos?", false)]
    [InlineData("nuevo usuario", "usuarios", "¿Cómo creo un usuario y sus permisos?", false)]
    [InlineData("agregar usuario", "usuarios", "¿Cómo creo un usuario y sus permisos?", false)]
    [InlineData("crear usurio", "usuarios", "¿Cómo creo un usuario y sus permisos?", true)]
    [InlineData("como funciona el MRP", "mrp", "¿Cómo funciona el MRP?", false)]
    [InlineData("requerimientos de material", "mrp", "¿Cómo funciona el MRP?", false)]
    [InlineData("materiales", "mrp", "¿Cómo funciona el MRP?", false)]
    [InlineData("como se hace una ficha", "mrp", "¿Cómo registro una ficha técnica?", false)]
    [InlineData("como se aze una ficha", "mrp", "¿Cómo registro una ficha técnica?", false)]
    [InlineData("ficha tecnica", "mrp", "¿Cómo registro una ficha técnica?", false)]
    [InlineData("ordenes de produccion", "ordenes", "¿Cómo creo una orden de producción?", false)]
    [InlineData("OP", "ordenes", "¿Cómo creo una orden de producción?", false)]
    [InlineData("como veo los alumnos", "aprendices", "¿Cómo veo los aprendices, las fichas y los grupos?", false)]
    [InlineData("estudiantes", "aprendices", "¿Cómo veo los aprendices, las fichas y los grupos?", false)]
    [InlineData("que es una planta de inventario", "plantas", "¿Qué es una planta de inventario?", false)]
    [InlineData("como solicitar materiales", "solicitudes", "¿Cómo solicito materiales?", false)]
    [InlineData("reingreso", "reingreso", "¿Cómo hago un reingreso desde una etapa?", false)]
    [InlineData("acta de ingreso", "actas", "¿Cómo hago un acta de ingreso o de egreso?", false)]
    [InlineData("movimientos de stock", "movimientos", "¿Cómo veo los movimientos de stock?", false)]
    [InlineData("trazabilidad", "trazabilidad", "¿Cómo funciona la trazabilidad de una prenda?", false)]
    [InlineData("reportes", "reportes", "¿Cómo veo los reportes y las estadísticas?", false)]
    [InlineData("primeros pasos", "primeros-pasos", "¿Por dónde empiezo en SIPITEX?", false)]
    public void Buscar_EntiendeLaIntencion(string consulta, string id, string titulo, bool sugerencia)
    {
        var resultado = AyudaBusqueda.Buscar(consulta, AyudaCatalogo.Visibles(Principal(UserRoles.Administrador)));

        Assert.NotEmpty(resultado.Resultados);
        Assert.Equal(id, resultado.Resultados[0].Id);
        Assert.Equal(titulo, resultado.Resultados[0].Titulo);
        Assert.Equal(sugerencia, resultado.Sugerencia);
        if (consulta == "crear usurio")
            Assert.DoesNotContain(resultado.Resultados, r => r.Id == "movimientos");
        Assert.False(string.IsNullOrWhiteSpace(resultado.Resultados[0].Descripcion));
        Assert.False(string.IsNullOrWhiteSpace(resultado.Resultados[0].Categoria));
        Assert.Contains(resultado.Resultados[0].Tipo, new[] { "Paso a paso", "Explicación" });
    }

    [Fact]
    public void Buscar_SinCoincidencia_NoInventaResultados()
    {
        var resultado = AyudaBusqueda.Buscar("xyzqwerty", AyudaCatalogo.Visibles(Principal(UserRoles.Administrador)));

        Assert.Empty(resultado.Resultados);
        Assert.False(resultado.Sugerencia);
    }

    [Fact]
    public void Instructor_NoRecibeLaGuiaDeUsuariosAunqueEscribaMal()
    {
        var resultado = AyudaBusqueda.Buscar("crear usurio", AyudaCatalogo.Visibles(Principal(UserRoles.Instructor)));

        Assert.DoesNotContain(resultado.Resultados, r => r.Id == "usuarios");
    }

    [Fact]
    public void Encargado_NoRecibeLaGuiaDeAprendices()
    {
        var resultado = AyudaBusqueda.Buscar("como veo los alumnos", AyudaCatalogo.Visibles(Principal(UserRoles.EncargadoDeBodega)));

        Assert.DoesNotContain(resultado.Resultados, r => r.Id == "aprendices");
    }

    private static ClaimsPrincipal Principal(string rol)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, rol),
            new Claim(ClaimTypes.Name, "Usuario")
        ], "Test");
        return new ClaimsPrincipal(identity);
    }
}
