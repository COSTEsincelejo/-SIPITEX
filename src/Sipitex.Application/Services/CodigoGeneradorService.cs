using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Application.Services;

// Consecutivos SOL-#### y ENT-#### a partir del último código persistido
public class CodigoGeneradorService : ICodigoGeneradorService
{
    private const string PrefijoSolicitud = "SOL-";
    private const string PrefijoEntrega = "ENT-";
    private const int AnchoNumerico = 4;

    private readonly ISolicitudMaterialRepository _solicitudRepository;

    public CodigoGeneradorService(ISolicitudMaterialRepository solicitudRepository) =>
        _solicitudRepository = solicitudRepository;

    public async Task<string> GenerarCodigoSolicitudMaterialAsync(CancellationToken cancellationToken = default)
    {
        var ultimo = await _solicitudRepository.GetLastCodigoSolicitudAsync(cancellationToken);
        return SiguienteCodigo(PrefijoSolicitud, ultimo);
    }

    public async Task<string> GenerarCodigoEntregaMaterialAsync(CancellationToken cancellationToken = default)
    {
        var ultimo = await _solicitudRepository.GetLastCodigoEntregaAsync(cancellationToken);
        return SiguienteCodigo(PrefijoEntrega, ultimo);
    }

    // Calcula el siguiente consecutivo; si no hay previo o no parsea, arranca en 1
    public static string SiguienteCodigo(string prefijo, string? ultimoCodigo)
    {
        var siguiente = 1;
        if (!string.IsNullOrWhiteSpace(ultimoCodigo)
            && ultimoCodigo.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(ultimoCodigo.AsSpan(prefijo.Length), out var n)
            && n >= 0)
        {
            siguiente = n + 1;
        }

        return $"{prefijo}{siguiente.ToString().PadLeft(AnchoNumerico, '0')}";
    }
}
