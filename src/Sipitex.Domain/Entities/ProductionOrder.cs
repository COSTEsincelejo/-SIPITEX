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

    // Cliente opcional (informativo)
    public string? ClientName { get; set; }

    // Meta total de unidades pedidas
    public int TotalQuantity { get; set; }

    // Avance acumulado (se suma al registrar sesiones/producción)
    public int ProducedQuantity { get; set; }

    // Estado actual; nace Pendiente hasta aprobación del Administrador
    public OrderStatus Status { get; set; } = OrderStatus.Pendiente;

    // Flujo de materiales de bodega (NoAplica si la orden no asocia insumos)
    public OrderMaterialsStatus MaterialsStatus { get; set; } = OrderMaterialsStatus.NoAplica;

    // Etapa actual del flujo MES (null si aún no hay etapas)
    public int? CurrentStageId { get; set; }
    public ProductionOrderStage? CurrentStage { get; set; }

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

    // Materiales opcionales a entregar desde bodega antes de producir
    public ICollection<ProductionOrderMaterialRequirement> MaterialRequirements { get; set; } = [];

    // Flujo MES: etapas, movimientos e historial
    public ICollection<ProductionOrderStage> Stages { get; set; } = [];
    public ICollection<ProductionOrderStageMovement> StageMovements { get; set; } = [];
    public ICollection<ProductionOrderHistoryEntry> HistoryEntries { get; set; } = [];

    // Auditoría de ediciones de campos (gap #2)
    public ICollection<OrderChangeLog> ChangeLogs { get; set; } = [];
}
