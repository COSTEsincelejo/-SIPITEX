using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Simula y confirma la carga de la hoja Inventario_Materiales. La simulación no escribe.
public interface IInventarioImportService
{
    Task<InventarioImportResult> SimularAsync(Stream contenido, CancellationToken cancellationToken = default);

    Task<InventarioImportResult> ConfirmarAsync(
        Stream contenido,
        int actorUserId,
        CancellationToken cancellationToken = default);
}
