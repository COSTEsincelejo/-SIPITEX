using Sipitex.Domain.Enums;

namespace Sipitex.Application.DTOs;

public static class InventarioImportTextos
{
    public const string Hoja = "Inventario_Materiales";
    public const long MaxBytes = 5 * 1024 * 1024;
    public const string ReferenciaMovimiento = "Importación inicial";
    public const string SinCodigo = "Sin código";
    public const string CodigoRepetido = "Código repetido";
    public const string SinUnidad = "Sin unidad";
    public const string Omitido =
        "Ya existe un material con el mismo código, nombre y planta. Se omite.";
}

public sealed class InventarioImportResult
{
    public bool ArchivoValido { get; init; } = true;
    public string? ErrorArchivo { get; init; }
    public IReadOnlyList<InventarioImportFilaValida> Validas { get; init; } = [];
    public IReadOnlyList<InventarioImportFilaError> Errores { get; init; } = [];
    public IReadOnlyList<InventarioImportAdvertencia> Advertencias { get; init; } = [];
    public IReadOnlyList<InventarioImportOmitida> Omitidas { get; init; } = [];
    public int Importados { get; init; }
    public bool Persistido { get; init; }

    public static InventarioImportResult Invalido(string error) => new()
    {
        ArchivoValido = false,
        ErrorArchivo = error
    };
}

public sealed class InventarioImportFilaValida
{
    public int Fila { get; init; }
    public string? CodigoAlmacen { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Categoria { get; init; }
    public string? Subcategoria { get; init; }
    public string? Referencia { get; init; }
    public string? Descripcion { get; init; }
    public string? Proveedor { get; init; }
    public decimal Stock { get; init; }
    public decimal MinStock { get; init; }
    public MaterialUnit Unidad { get; init; }
    public DateOnly FechaIngreso { get; init; }
    public string Planta { get; init; } = string.Empty;
    public bool PlantaNueva { get; init; }
    public TipoElemento TipoElemento { get; init; }
    public IReadOnlyList<string> Advertencias { get; init; } = [];
}

public sealed class InventarioImportFilaError
{
    public int Fila { get; init; }
    public string? Nombre { get; init; }
    public string Mensaje { get; init; } = string.Empty;
}

public sealed class InventarioImportAdvertencia
{
    public int Fila { get; init; }
    public string? Nombre { get; init; }
    public string Mensaje { get; init; } = string.Empty;
}

public sealed class InventarioImportOmitida
{
    public int Fila { get; init; }
    public string? CodigoAlmacen { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Planta { get; init; } = string.Empty;
    public string Mensaje { get; init; } = InventarioImportTextos.Omitido;
}
