namespace Sipitex.Domain.Entities;

// Contador atómico de números de documento.
// La fila OrdenProduccion guarda el próximo correlativo visible (OP-101, OP-102...).
// Se incrementa con un UPDATE ... RETURNING: PostgreSQL bloquea esa fila hasta
// confirmar, así dos altas simultáneas no pueden tomar el mismo número.
// Si la orden no llega a guardarse, el número queda salteado y no se reutiliza.
// Los números ya emitidos (OP-001 de datos de prueba, OP-104 de producción) no se reescriben.
public class NumeracionDocumento
{
    public const string OrdenProduccion = "OrdenProduccion";

    // Primer número de una base vacía. Conserva la serie OP-101 que usaba Count + 101.
    public const int PrimerNumeroOrden = 101;

    public string Codigo { get; set; } = string.Empty;

    // Próximo número a entregar. El UPDATE lo avanza y devuelve el que se acaba de tomar.
    public int Siguiente { get; set; } = PrimerNumeroOrden;
}
