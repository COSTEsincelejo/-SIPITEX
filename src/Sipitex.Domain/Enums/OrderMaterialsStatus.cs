namespace Sipitex.Domain.Enums;

// Flujo de materiales de planta de inventario ligado a una orden (aparte de OrderStatus de producción)
public enum OrderMaterialsStatus
{
    NoAplica,                // Sin materiales asociados — producción libre (comportamiento legacy)
    PendienteRevisionPlantaInventario, // Hay requisitos; plantaInventario aún no valida/entrega
    MaterialesValidados,     // PlantaInventario revisó disponibilidad
    EntregaParcial,          // Se entregó solo parte de lo requerido
    ListaParaProduccion      // Todo lo requerido fue entregado
}
