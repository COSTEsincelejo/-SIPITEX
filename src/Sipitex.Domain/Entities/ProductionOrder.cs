using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Orden de producción: qué fabricar, cuánto y hasta cuándo
public class ProductionOrder
{
    // PK
    public int Id { get; set; }

    // Número visible tipo OP-101 (único en BD)
    public string OrderNumber { get; set; } = string.Empty;

    // Producto a fabricar (debe existir en el BOM: Camisa / Pantalón)
    public string ProductName { get; set; } = string.Empty;

    // Meta total de unidades pedidas
    public int TotalQuantity { get; set; }

    // Avance acumulado (se suma al registrar sesiones/producción)
    public int ProducedQuantity { get; set; }

    // Estado actual; arranca EnProceso
    public OrderStatus Status { get; set; } = OrderStatus.EnProceso;

    // Fecha límite de entrega (solo día)
    public DateOnly Deadline { get; set; }

    // Solicitudes de material ligadas a esta orden
    public ICollection<MaterialRequest> MaterialRequests { get; set; } = [];

    // Inspecciones de calidad de esta orden
    public ICollection<QualityRecord> QualityRecords { get; set; } = [];

    // Fichas que están trabajando esta orden
    public ICollection<Ficha> Fichas { get; set; } = [];

    // Receta congelada al crear la orden (independiente del BOM vigente)
    public ICollection<ProductionOrderBomSnapshot> BomSnapshots { get; set; } = [];
}
