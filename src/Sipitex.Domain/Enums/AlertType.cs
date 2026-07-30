namespace Sipitex.Domain.Enums;

// Tipos de alerta del sistema. Les puse número fijo por si EF Core los guarda como int
// y no quiero que se desordenen si cambio el orden del enum después.
public enum AlertType
{
    StockBajo = 1,
    SolicitudPendiente = 2,
    OrdenPorVencer = 3,
    ReprocesoCalidad = 4,
    OrdenAtrasada = 5
}
