namespace Sipitex.Domain.Enums;

// Tipos de movimiento del ledger de inventario (bodega)
public enum StockMovementType
{
    Entrada,              // Alta de material / ingreso inicial
    Salida,               // Entrega a producción (descuenta stock)
    Ajuste,               // Ajuste manual de existencias
    AprobacionSolicitud   // Aprobación de solicitud (legacy o multi-ítem)
}
