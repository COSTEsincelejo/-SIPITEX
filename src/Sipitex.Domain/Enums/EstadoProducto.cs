namespace Sipitex.Domain.Enums;

// Ciclo de vida del producto en proceso (no confundir con OrderStatus ni con etapas MES).
public enum EstadoProducto
{
    MateriaPrima = 0,
    Corte = 1,
    Confeccion = 2,
    Calidad = 3,
    ProductoTerminado = 4,
    VentaEntrega = 5
}
