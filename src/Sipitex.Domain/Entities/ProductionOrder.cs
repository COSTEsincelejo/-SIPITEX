using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Orden de producción: qué se va a fabricar y cuánto.
public class ProductionOrder
{
    public int Id { get; set; }

    // Número de orden, tipo OP-2026-001
    public string OrderNumber { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    // Cantidad total que se pidió producir
    public int TotalQuantity { get; set; }

    // Lo que ya se ha producido (se va sumando con las sesiones)
    public int ProducedQuantity { get; set; }

    // Arranca en proceso; después puede pasar a finalizada o cancelada
    public OrderStatus Status { get; set; } = OrderStatus.EnProceso;

    // Fecha límite de entrega
    public DateOnly Deadline { get; set; }

    // Solicitudes de material asociadas a esta orden
    public ICollection<MaterialRequest> MaterialRequests { get; set; } = [];

    // Inspecciones de calidad de esta orden
    public ICollection<QualityRecord> QualityRecords { get; set; } = [];

    // Fichas que trabajan en esta orden
    public ICollection<Ficha> Fichas { get; set; } = [];
}
