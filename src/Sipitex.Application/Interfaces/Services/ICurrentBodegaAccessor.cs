namespace Sipitex.Application.Interfaces.Services;

// Bodega del request actual para Global Query Filters.
// null = sin restricción (Admin, Instructor, anónimo, seeds/jobs).
// 0 = Bodeguero autenticado sin bodega asignada (no ve filas; no es null a propósito).
public interface ICurrentBodegaAccessor
{
    int? BodegaId { get; }
}

public static class BodegaClaimTypes
{
    public const string BodegaId = "bodega_id";
}

public sealed class NullCurrentBodegaAccessor : ICurrentBodegaAccessor
{
    public static NullCurrentBodegaAccessor Instance { get; } = new();

    public int? BodegaId => null;
}

public sealed class FixedCurrentBodegaAccessor(int? bodegaId) : ICurrentBodegaAccessor
{
    public int? BodegaId { get; } = bodegaId;
}
