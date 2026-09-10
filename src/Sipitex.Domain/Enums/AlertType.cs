namespace Sipitex.Domain.Enums;

// Tipos de alerta del sistema.
// Les puse número fijo por si EF los guarda como int y después reordeno el enum.
public enum AlertType
{
    StockBajo = 1,           // material por debajo del mínimo
    SolicitudPendiente = 2,  // hay pedidos a plantaInventario sin resolver (MaterialRequest legacy)
    OrdenPorVencer = 3,      // plazo ≤ 7 días
    ReprocesoCalidad = 4,    // hubo reprocesos recientes
    OrdenAtrasada = 5,       // poco avance y plazo cerca
    SolicitudMaterialNueva = 6,    // nueva SolicitudMaterial (flujo Ficha) → EncargadoDeBodega
    SolicitudMaterialResuelta = 7  // SolicitudMaterial resuelta → Solicitante
}
