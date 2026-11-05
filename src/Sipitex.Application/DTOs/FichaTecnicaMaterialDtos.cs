namespace Sipitex.Application.DTOs;

public sealed class FichaTecnicaMaterialDetalle
{
    public int MaterialId { get; init; }
    public int PlantaInventarioId { get; init; }
    public string Planta { get; init; } = string.Empty;
    public string Codigo { get; init; } = string.Empty;
    public string? CodigoAlmacen { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Referencia { get; init; }
    public string Unidad { get; init; } = string.Empty;
    public bool TieneFicha { get; init; }
    public FichaTecnicaMaterialDatos Ficha { get; init; } = new();
}

public sealed class FichaTecnicaMaterialDatos
{
    public string? DescripcionTecnica { get; init; }
    public string? TipoMaterial { get; init; }
    public string? Composicion { get; init; }
    public string? GramajeGm2 { get; init; }
    public string? AnchoCm { get; init; }
    public string? Colores { get; init; }
    public string? Acabados { get; init; }
    public string? EspecificacionesUso { get; init; }
    public string? ProcesoFabricacion { get; init; }
    public string? NormasCertificaciones { get; init; }
    public string? EnlaceFichaOriginal { get; init; }
    public string? Observaciones { get; init; }
}

public sealed class GuardarFichaTecnicaMaterialDto
{
    public int MaterialId { get; init; }
    public string? DescripcionTecnica { get; init; }
    public string? TipoMaterial { get; init; }
    public string? Composicion { get; init; }
    public string? GramajeGm2 { get; init; }
    public string? AnchoCm { get; init; }
    public string? Colores { get; init; }
    public string? Acabados { get; init; }
    public string? EspecificacionesUso { get; init; }
    public string? ProcesoFabricacion { get; init; }
    public string? NormasCertificaciones { get; init; }
    public string? EnlaceFichaOriginal { get; init; }
    public string? Observaciones { get; init; }
}

public sealed class FichaTecnicaMaterialResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> Errores { get; init; } = [];

    public static FichaTecnicaMaterialResult Ok(string message) => new() { Success = true, Message = message };

    public static FichaTecnicaMaterialResult Fail(string message, IReadOnlyList<string>? errores = null) =>
        new()
        {
            Success = false,
            Message = message,
            Errores = errores is { Count: > 0 } ? errores : [message]
        };
}
