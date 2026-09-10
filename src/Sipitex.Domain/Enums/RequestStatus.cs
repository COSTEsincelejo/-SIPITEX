namespace Sipitex.Domain.Enums;

// Estado de una solicitud de material a plantaInventario
public enum RequestStatus
{
    Pendiente,  // esperando a que plantaInventario la vea
    Aprobada,   // se descontó stock
    Rechazada   // no se entregó nada
}
