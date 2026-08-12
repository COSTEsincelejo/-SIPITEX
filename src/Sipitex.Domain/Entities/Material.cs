using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Material de bodega (tela, hilo, botones, etc.)
public class Material
{
    // PK
    public int Id { get; set; }

    // Código interno autogenerado al crear (tipo mat123456...)
    public string Code { get; set; } = string.Empty;

    // Nombre para mostrar en pantallas y reportes
    public string Name { get; set; } = string.Empty;

    // Unidad de medida: Metros, Unidades o Kg
    public MaterialUnit Unit { get; set; }

    // Cantidad actual en bodega (puede tener decimales)
    public decimal Stock { get; set; }

    // Umbral mínimo; si Stock < MinStock dispara alerta de stock bajo
    public decimal MinStock { get; set; }

    // Estado físico del material; arranca en Bueno
    public MaterialStatus Status { get; set; } = MaterialStatus.Bueno;

    // Fecha de la última entrada/ajuste de stock (solo día, sin hora)
    public DateOnly LastEntryDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    // Relación 1-N: en qué ítems del BOM aparece este material
    public ICollection<BomItem> BomItems { get; set; } = [];

    // Relación 1-N: solicitudes de este material hechas por producción
    public ICollection<MaterialRequest> Requests { get; set; } = [];

    // Historial de movimientos de stock de este material
    public ICollection<StockMovement> StockMovements { get; set; } = [];
}
