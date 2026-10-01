using Sipitex.Domain.Enums;

namespace Sipitex.Application.Search;

public enum NavegacionBusqueda
{
    Movimientos,
    Reingreso,
    Grupos
}

public enum FiltroSolicitudBusqueda
{
    Pendientes,
    Aprobadas,
    Rechazadas
}

// Resultado del intérprete. No consulta la base: solo dice qué quiso decir la persona.
public sealed class ConsultaInterpretada
{
    public static readonly string[] Ejemplos = ["OP-001", "stock crítico planta 1", "camisa"];

    public bool ConsultaValida { get; init; }
    public bool Reconocida { get; init; }
    public string Normalizada { get; init; } = "";
    public string Frase { get; init; } = "";
    public int? NumeroOrden { get; init; }
    public int? NumeroPlanta { get; init; }
    public StockNivel? NivelStock { get; init; }
    public bool Agotados { get; init; }
    public string TextoRestante { get; init; } = "";
    public bool QuierePersonas { get; init; }
    public string TextoPersona { get; init; } = "";
    public FiltroSolicitudBusqueda? FiltroSolicitud { get; init; }
    public int? NumeroSolicitud { get; init; }
    public string TextoFichaTecnica { get; init; } = "";
    public NavegacionBusqueda? Navegacion { get; init; }

    public static ConsultaInterpretada Vacia() => new()
    {
        ConsultaValida = false,
        Reconocida = false
    };

    public static ConsultaInterpretada NoReconocida(string normalizada) => new()
    {
        ConsultaValida = true,
        Reconocida = false,
        Normalizada = normalizada
    };
}
