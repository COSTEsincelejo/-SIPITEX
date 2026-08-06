namespace Sipitex.Domain.Enums;

// Flujo de materiales de bodega ligado a una orden (aparte de OrderStatus de producción)
public enum OrderMaterialsStatus
{
    NoAplica,                // Sin materiales asociados — producción libre (comportamiento legacy)
    PendienteRevisionBodega, // Hay requisitos; bodega aún no valida/entrega
    MaterialesValidados,     // Bodega revisó disponibilidad
    EntregaParcial,          // Se entregó solo parte de lo requerido
    ListaParaProduccion      // Todo lo requerido fue entregado
}
