namespace Sipitex.Application.Interfaces.Services;

// Genera consecutivos de negocio (no hardcodear en controladores)
public interface ICodigoGeneradorService
{
    // Siguiente código SOL-####
    Task<string> GenerarCodigoSolicitudMaterialAsync(CancellationToken cancellationToken = default);

    // Siguiente código ENT-####
    Task<string> GenerarCodigoEntregaMaterialAsync(CancellationToken cancellationToken = default);
}
