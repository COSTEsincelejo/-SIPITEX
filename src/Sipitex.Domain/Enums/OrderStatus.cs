namespace Sipitex.Domain.Enums;

// Ciclo de vida de una orden de producción
public enum OrderStatus
{
    Pendiente,   // creada pero aún no arranca
    EnProceso,   // se está fabricando
    Finalizada,  // ya llegó a la meta
    Cancelada    // se cerró sin completar
}
