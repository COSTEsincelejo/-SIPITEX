using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Material de inventario (tela, hilo, etc.). Es lo que maneja el bodeguero.
public class Material
{
    public int Id { get; set; }

    // Código interno, tipo MAT-001
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // Unidad en que se mide: metros, unidades o kg
    public MaterialUnit Unit { get; set; }

    public decimal Stock { get; set; }

    // Si el stock baja de esto, debería saltar alerta de stock bajo
    public decimal MinStock { get; set; }

    // Estado físico del material (bueno, regular, deteriorado)
    public MaterialStatus Status { get; set; } = MaterialStatus.Bueno;

    // Última vez que entró material a bodega
    public DateOnly LastEntryDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    // Navegación: en qué BOMs aparece este material
    public ICollection<BomItem> BomItems { get; set; } = [];

    // Solicitudes de este material hechas por producción
    public ICollection<MaterialRequest> Requests { get; set; } = [];
}
