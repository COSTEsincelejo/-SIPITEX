namespace Sipitex.Domain;

// La fila de Material cambió entre la lectura y el guardado (otro movimiento de stock).
public class ConflictoConcurrenciaStockException : Exception
{
    public const string Mensaje =
        "El stock de este material fue modificado por otra operación. Revise la cantidad disponible e intente de nuevo.";

    public ConflictoConcurrenciaStockException()
        : base(Mensaje)
    {
    }
}
