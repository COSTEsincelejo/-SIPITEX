namespace Sipitex.Application.Search;

// Lo que el rol puede ver. El servicio no lee la cookie: el controlador arma este alcance.
public sealed class AlcanceBusqueda
{
    public string Rol { get; init; } = "";
    public int? UserId { get; init; }
    public string? Nombre { get; init; }

    // Null = sin recorte (administrador o instructor). Lista = plantas del encargado, vacía = ninguna.
    public IReadOnlyList<int>? PlantaInventarioIds { get; init; }

    public bool PuedeVerInventario { get; init; }
    public bool PuedeVerUsuarios { get; init; }
    public bool PuedeVerGrupos { get; init; }
    public bool PuedeVerMovimientos { get; init; }
    public bool PuedeVerReingreso { get; init; }
    public bool PuedeVerTodasLasFichasTecnicas { get; init; }
    public bool EsInstructor { get; init; }

    public bool RestringePlantas => PlantaInventarioIds is not null;
}
