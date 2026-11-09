using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Material de planta de inventario (tela, hilo, botones, etc.)
public class Material
{
    // PK
    public int Id { get; set; }

    public const int CodigoAlmacenMaxLength = 40;
    public const int ReferenciaMaxLength = 80;
    public const int CategoriaMaxLength = 80;
    public const int SubcategoriaMaxLength = 80;
    public const int DescripcionMaxLength = 1000;
    public const int ProveedorMaxLength = 120;

    // Código interno autogenerado al crear (tipo mat123456...). Sigue siendo único; no se reemplaza por CodigoAlmacen.
    public string Code { get; set; } = string.Empty;

    // Número de referencia del almacén principal. Puede repetirse entre materiales.
    public string? CodigoAlmacen { get; set; }

    // Referencia del ítem en la plantilla CMTC (distinta del código interno).
    public string? Referencia { get; set; }

    // Nombre para mostrar en pantallas y reportes
    public string Name { get; set; } = string.Empty;

    // Clasificación libre de la plantilla (telas, hilos, avíos...).
    public string? Categoria { get; set; }

    public string? Subcategoria { get; set; }

    public string? Descripcion { get; set; }

    public string? Proveedor { get; set; }

    // Consumible por defecto: los materiales ya cargados no cambian de tipo al migrar.
    public TipoElemento TipoElemento { get; set; } = TipoElemento.Consumible;

    // Unidad de medida: Metros, Unidades o Kg
    public MaterialUnit Unit { get; set; }

    // Cantidad actual en planta de inventario (puede tener decimales)
    public decimal Stock { get; set; }

    // Token de concurrencia optimista. Cada guardado de la fila lo incrementa.
    // Dos operaciones que leyeron el mismo stock no pueden pisarse: la segunda
    // falla y pide reintentar, en lugar de dejar un saldo a medias.
    public long RowVersion { get; set; }

    // Umbral de reposición. <= 0 significa que no hay mínimo: el nivel es Sin mínimo definido y no alerta.
    public decimal MinStock { get; set; }

    // Estado físico del material; arranca en Bueno
    public MaterialStatus Status { get; set; } = MaterialStatus.Bueno;

    // Fecha de la última entrada/ajuste de stock (solo día, sin hora)
    public DateOnly LastEntryDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    // Último precio unitario de compra (catálogo). No se usa para costear prendas ya producidas.
    public decimal CostoAdquisicion { get; set; }

    // Costo promedio ponderado vigente. Se recalcula al registrar una compra con el stock que ya tiene costo.
    // Las unidades de una importación inicial sin costo (StockSinCosto) no entran al promedio.
    public decimal CostoPromedioPonderado { get; set; }

    // Unidades físicas que entraron sin costo (importación inicial). No diluyen el promedio.
    // Las salidas las consumen primero, vía DescontarUnidadesSinCosto.
    public decimal StockSinCosto { get; set; }

    public void DescontarUnidadesSinCosto(decimal cantidad)
    {
        if (cantidad <= 0 || StockSinCosto <= 0)
            return;
        StockSinCosto = cantidad >= StockSinCosto ? 0 : StockSinCosto - cantidad;
    }

    // FK obligatoria a la plantaInventario dueña de este material (default 1 = PlantaInventario 1, alineado al backfill)
    public int PlantaInventarioId { get; set; } = 1;

    // Navegación a la plantaInventario
    public PlantaInventario PlantaInventario { get; set; } = null!;

    // Relación 1-N: en qué ítems del BOM aparece este material
    public ICollection<BomItem> BomItems { get; set; } = [];

    // Relación 1-N: solicitudes de este material hechas por producción
    public ICollection<MaterialRequest> Requests { get; set; } = [];

    // Historial de movimientos de stock de este material
    public ICollection<StockMovement> StockMovements { get; set; } = [];

    public ICollection<ConsumoMaterial> Consumos { get; set; } = [];

    // Ficha técnica de este material. Distinta de la ficha de prenda (BomProduct).
    public FichaTecnicaMaterial? FichaTecnica { get; set; }
}
