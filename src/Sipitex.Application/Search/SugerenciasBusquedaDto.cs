namespace Sipitex.Application.Search;

public sealed record SugerenciasBusquedaDto(
    string Entendi,
    IReadOnlyList<GrupoSugerenciaDto> Grupos,
    IReadOnlyList<string> Ejemplos)
{
    // Acciones del catálogo, ya filtradas por rol. El cliente puntúa el mismo catálogo
    // sin esperar a la red; este campo deja el contrato listo para quien llame al endpoint.
    public IReadOnlyList<AccionSugeridaDto> Acciones { get; init; } = [];

    public static SugerenciasBusquedaDto Vacias() =>
        new("", [], []);

    public static SugerenciasBusquedaDto SinEntender() =>
        new("", [], ConsultaInterpretada.Ejemplos);
}

public sealed record AccionSugeridaDto(
    string Titulo,
    string Descripcion,
    string Url,
    string Icono,
    string Categoria,
    int Puntaje);

public sealed record GrupoSugerenciaDto(string Tipo, IReadOnlyList<ItemSugerenciaDto> Items);

public sealed record ItemSugerenciaDto(string Texto, string Url, string Destino, string Icono);
