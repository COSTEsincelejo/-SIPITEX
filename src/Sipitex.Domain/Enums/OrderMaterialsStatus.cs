namespace Sipitex.Domain.Enums;

// Flujo de materiales de planta de inventario ligado a una orden (aparte de OrderStatus de producción)
public enum OrderMaterialsStatus
{
    // No hay líneas de entrega de planta (ProductionOrderMaterialRequirement).
    // No es un nivel de stock y no significa que falte el BOM: la receta congelada
    // puede existir y se muestra en el MRP. La orden produce en modo libre hasta que se asignen insumos.
    NoAplica,
    PendienteRevisionPlantaInventario, // Hay requisitos; plantaInventario aún no valida/entrega
    MaterialesValidados,     // PlantaInventario revisó disponibilidad
    EntregaParcial,          // Se entregó solo parte de lo requerido
    ListaParaProduccion      // Todo lo requerido fue entregado
}
