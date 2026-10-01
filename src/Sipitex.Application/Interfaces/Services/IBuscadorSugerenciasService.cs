using Sipitex.Application.Search;

namespace Sipitex.Application.Interfaces.Services;

public interface IBuscadorSugerenciasService
{
    Task<SugerenciasBusquedaDto> SugerirAsync(
        string consulta,
        AlcanceBusqueda alcance,
        CancellationToken cancellationToken = default);
}
