namespace Sipitex.Application.Interfaces.Services;

// PlantasInventario del request actual para Global Query Filters.
// null = sin restricción (Admin, Instructor, anónimo, seeds/jobs).
// lista vacía = EncargadoDeBodega autenticado sin ninguna plantaInventario asignada (no ve filas; no es null a propósito).
// lista con ids = EncargadoDeBodega restringido a esas plantasInventario (IN).
public interface ICurrentPlantaInventarioAccessor
{
    IReadOnlyList<int>? PlantaInventarioIds { get; }
}

public static class PlantaInventarioClaimTypes
{
    // Múltiples claims del mismo tipo (uno por plantaInventario asignada). Menos invasivo que un CSV.
    public const string PlantaInventarioId = "planta_inventario_id";
}

public sealed class NullCurrentPlantaInventarioAccessor : ICurrentPlantaInventarioAccessor
{
    public static NullCurrentPlantaInventarioAccessor Instance { get; } = new();

    public IReadOnlyList<int>? PlantaInventarioIds => null;
}

public sealed class FixedCurrentPlantaInventarioAccessor(IReadOnlyList<int>? plantaInventarioIds) : ICurrentPlantaInventarioAccessor
{
    public IReadOnlyList<int>? PlantaInventarioIds { get; } = plantaInventarioIds;
}
