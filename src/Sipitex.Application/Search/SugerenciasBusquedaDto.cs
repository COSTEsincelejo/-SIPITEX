namespace Sipitex.Application.Search;

public sealed record SugerenciasBusquedaDto(
    string Entendi,
    IReadOnlyList<GrupoSugerenciaDto> Grupos,
    IReadOnlyList<string> Ejemplos)
{
    public static SugerenciasBusquedaDto Vacias() =>
        new("", [], []);

    public static SugerenciasBusquedaDto SinEntender() =>
        new("", [], ConsultaInterpretada.Ejemplos);
}

public sealed record GrupoSugerenciaDto(string Tipo, IReadOnlyList<ItemSugerenciaDto> Items);

public sealed record ItemSugerenciaDto(string Texto, string Url, string Destino, string Icono);
