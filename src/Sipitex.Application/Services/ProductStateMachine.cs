using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Transiciones del producto en proceso. Adelante solo de a una etapa; el retroceso
// exige justificación (p. ej. Calidad → Confección si la prenda quedó Malo).
public class ProductStateMachine : IProductStateMachine
{
    public static readonly EstadoProducto[] Sequence =
    [
        EstadoProducto.MateriaPrima,
        EstadoProducto.Corte,
        EstadoProducto.Confeccion,
        EstadoProducto.Calidad,
        EstadoProducto.ProductoTerminado,
        EstadoProducto.VentaEntrega
    ];

    public bool CanTransition(
        EstadoProducto from,
        EstadoProducto to,
        bool justificacionRetroceso,
        out string? error)
    {
        if (from == to)
        {
            error = "El producto ya está en ese estado.";
            return false;
        }

        var fromIdx = Array.IndexOf(Sequence, from);
        var toIdx = Array.IndexOf(Sequence, to);
        if (fromIdx < 0 || toIdx < 0)
        {
            error = "Estado de producto no reconocido.";
            return false;
        }

        if (toIdx == fromIdx + 1)
        {
            error = null;
            return true;
        }

        if (toIdx < fromIdx)
        {
            if (!justificacionRetroceso)
            {
                error = "El retroceso de etapa requiere una justificación (por ejemplo clasificación Malo).";
                return false;
            }

            error = null;
            return true;
        }

        error = $"No se puede saltar de {from} a {to}. Avance por las etapas intermedias.";
        return false;
    }

    public IReadOnlyList<EstadoProducto> AllowedTransitions(EstadoProducto from, bool allowBackward)
    {
        var list = new List<EstadoProducto>();
        var fromIdx = Array.IndexOf(Sequence, from);
        if (fromIdx < 0)
            return list;

        if (fromIdx + 1 < Sequence.Length)
            list.Add(Sequence[fromIdx + 1]);

        if (allowBackward)
        {
            for (var i = 0; i < fromIdx; i++)
                list.Add(Sequence[i]);
        }

        return list;
    }
}
