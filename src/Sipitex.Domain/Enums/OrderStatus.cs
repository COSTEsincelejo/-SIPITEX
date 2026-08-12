namespace Sipitex.Domain.Enums;

// Ciclo de vida de una orden de producción
public enum OrderStatus
{
    Pendiente,   // creada; espera aprobación del Administrador
    EnProceso,   // aprobada y en fabricación
    Finalizada,  // ya llegó a la meta
    Cancelada    // se cerró sin completar
}
