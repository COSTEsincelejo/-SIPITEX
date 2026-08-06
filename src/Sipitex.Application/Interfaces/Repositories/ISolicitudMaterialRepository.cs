using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Persistencia del flujo SolicitudMaterial (paralelo a MaterialRequest)
public interface ISolicitudMaterialRepository
{
    Task AddAsync(SolicitudMaterial solicitud, CancellationToken cancellationToken = default);

    Task<SolicitudMaterial?> GetByIdWithDetallesAsync(int id, CancellationToken cancellationToken = default);

    // Detalle con solicitud, todos los ítems y material (para aprobar con stock)
    Task<DetalleSolicitudMaterial?> GetDetalleByIdAsync(int detalleId, CancellationToken cancellationToken = default);

    void Update(SolicitudMaterial solicitud);

    Task AddEntregaAsync(EntregaMaterial entrega, CancellationToken cancellationToken = default);

    // Último código SOL-#### / ENT-#### (orden lexicográfico = numérico con padding fijo)
    Task<string?> GetLastCodigoSolicitudAsync(CancellationToken cancellationToken = default);

    Task<string?> GetLastCodigoEntregaAsync(CancellationToken cancellationToken = default);
}
