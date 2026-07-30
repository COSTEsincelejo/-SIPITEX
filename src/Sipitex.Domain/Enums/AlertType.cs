namespace Sipitex.Domain.Enums;

// Tipos de alerta del sistema.
// Les puse número fijo por si EF los guarda como int y después reordeno el enum.
public enum AlertType
{
    StockBajo = 1,           // material por debajo del mínimo
    SolicitudPendiente = 2,  // hay pedidos a bodega sin resolver
    OrdenPorVencer = 3,      // plazo ≤ 7 días
    ReprocesoCalidad = 4,    // hubo reprocesos recientes
    OrdenAtrasada = 5        // poco avance y plazo cerca
}
