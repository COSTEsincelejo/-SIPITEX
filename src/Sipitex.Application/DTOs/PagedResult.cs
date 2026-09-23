using Sipitex.Application.Helpers;

namespace Sipitex.Application.DTOs;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = Paging.DefaultPageSize;
    public int TotalCount { get; init; }
    public int TotalPages => Paging.TotalPages(TotalCount, PageSize);
}

public sealed class MaterialPageDto
{
    public IReadOnlyList<MaterialDto> Items { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = Paging.DefaultPageSize;
    public int TotalCount { get; init; }
    public int TotalSinFiltro { get; init; }
    public int TotalPages => Paging.TotalPages(TotalCount, PageSize);
}

public sealed record PlantaStockConteoDto(int PlantaInventarioId, int Materiales, int Bajo, int Critico);
