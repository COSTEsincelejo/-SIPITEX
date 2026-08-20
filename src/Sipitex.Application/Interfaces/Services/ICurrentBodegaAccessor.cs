namespace Sipitex.Application.Interfaces.Services;

// Bodegas del request actual para Global Query Filters.
// null = sin restricción (Admin, Instructor, anónimo, seeds/jobs).
// lista vacía = Bodeguero autenticado sin ninguna bodega asignada (no ve filas; no es null a propósito).
// lista con ids = Bodeguero restringido a esas bodegas (IN).
public interface ICurrentBodegaAccessor
{
    IReadOnlyList<int>? BodegaIds { get; }
}

public static class BodegaClaimTypes
{
    // Múltiples claims del mismo tipo (uno por bodega asignada). Menos invasivo que un CSV.
    public const string BodegaId = "bodega_id";
}

public sealed class NullCurrentBodegaAccessor : ICurrentBodegaAccessor
{
    public static NullCurrentBodegaAccessor Instance { get; } = new();

    public IReadOnlyList<int>? BodegaIds => null;
}

public sealed class FixedCurrentBodegaAccessor(IReadOnlyList<int>? bodegaIds) : ICurrentBodegaAccessor
{
    public IReadOnlyList<int>? BodegaIds { get; } = bodegaIds;
}
