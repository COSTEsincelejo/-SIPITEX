using System.Globalization;
using System.Text;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// El taller avanza por etapas (Trazo, Corte, Confección, Control de Calidad, Terminado).
// El ciclo de la prenda solo tiene los valores de EstadoProducto: no hay Trazo ni Cancelado.
// Trazo se muestra como Materia prima hasta entrar a Corte. Cancelar la orden cambia
// OrderStatus y no este ciclo. Venta/entrega no es una etapa del taller.
public static class EstadoProductoEtapa
{
    public static bool TryForStage(string? stageName, out EstadoProducto estado)
    {
        switch (Normalize(stageName))
        {
            case "trazo":
                estado = EstadoProducto.MateriaPrima;
                return true;
            case "corte":
                estado = EstadoProducto.Corte;
                return true;
            case "confeccion":
                estado = EstadoProducto.Confeccion;
                return true;
            case "control de calidad":
            case "calidad":
                estado = EstadoProducto.Calidad;
                return true;
            case "terminado":
                estado = EstadoProducto.ProductoTerminado;
                return true;
            default:
                estado = default;
                return false;
        }
    }

    private static string Normalize(string? stageName)
    {
        if (string.IsNullOrWhiteSpace(stageName))
            return string.Empty;

        var formD = stageName.Trim().Normalize(NormalizationForm.FormD);
        var chars = formD.Where(c =>
            CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return string.Concat(chars).ToLowerInvariant();
    }
}
