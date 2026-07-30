namespace Sipitex.Domain.Enums;

// Estado de una solicitud de material a bodega
public enum RequestStatus
{
    Pendiente,  // esperando a que bodega la vea
    Aprobada,   // se descontó stock
    Rechazada   // no se entregó nada
}
