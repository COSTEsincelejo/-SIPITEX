using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Search;

// Interpreta la consulta en español, sin base de datos ni librerías externas.
public sealed class InterpreteConsulta
{
    private static readonly Dictionary<string, string> ErroresComunes = new(StringComparer.Ordinal)
    {
        ["critco"] = "critico",
        ["crtico"] = "critico",
        ["pendinte"] = "pendiente",
        ["pendintes"] = "pendientes",
        ["solictud"] = "solicitud",
        ["solictudes"] = "solicitudes",
        ["agotdo"] = "agotado",
        ["minmo"] = "minimo",
        ["reingeso"] = "reingreso",
        ["bdega"] = "bodega",
        ["movientos"] = "movimientos",
        ["confecion"] = "confeccion"
    };

    private static readonly string[] Stopwords = ["de", "en", "el", "la", "los", "las", "con", "del", "al", "y", "un", "una"];

    public ConsultaInterpretada Interpretar(string? consulta)
    {
        var normalizada = Corregir(Normalizar(consulta));
        if (ContarAlfanumericos(normalizada) < 2)
            return ConsultaInterpretada.Vacia();

        if (EsSinSentido(normalizada))
            return ConsultaInterpretada.NoReconocida(normalizada);

        var work = Espaciar(normalizada);
        var navegacion = TomarNavegacion(ref work);
        work = Espaciar(work);
        var ficha = Tomar(ref work, @"ficha tecnica\s+([a-z0-9][a-z0-9 .-]{0,40})");
        work = Espaciar(work);
        var filtroSolicitud = TomarFiltroSolicitud(ref work);
        work = Espaciar(work);
        var numeroSolicitud = TomarEntero(ref work, @"sol\s*-?\s*0*(\d+)")
            ?? TomarEntero(ref work, @"solicitud(?:es)?\s+(?:numero\s+)?0*(\d+)");
        work = Espaciar(work);
        var persona = TomarPersona(ref work);
        work = Espaciar(work);
        var numeroPlanta = TomarEntero(ref work, @"(?:planta(?:\s+de\s+inventario)?|bodega)\s+(\d+)");
        work = Espaciar(work);
        var (nivel, agotados) = TomarNivel(ref work);
        work = Espaciar(work);
        var numeroOrden = TomarEntero(ref work, @"op\s*-?\s*0*(\d+)")
            ?? TomarEntero(ref work, @"orden(?:es)?(?:\s+de\s+produccion)?\s+0*(\d+)");
        var restante = LimpiarRestante(work);

        var quierePersonas = persona is not null;
        var reconocida = navegacion is not null
            || ficha is not null
            || filtroSolicitud is not null
            || numeroSolicitud is not null
            || quierePersonas
            || numeroPlanta is not null
            || nivel is not null
            || numeroOrden is not null
            || restante.Length >= 2;

        if (!reconocida)
            return ConsultaInterpretada.NoReconocida(normalizada);

        var resultado = new ConsultaInterpretada
        {
            ConsultaValida = true,
            Reconocida = true,
            Normalizada = normalizada,
            NumeroOrden = numeroOrden,
            NumeroPlanta = numeroPlanta,
            NivelStock = nivel,
            Agotados = agotados,
            TextoRestante = restante,
            QuierePersonas = quierePersonas,
            TextoPersona = persona ?? "",
            FiltroSolicitud = filtroSolicitud,
            NumeroSolicitud = numeroSolicitud,
            TextoFichaTecnica = ficha ?? "",
            Navegacion = navegacion
        };

        return new ConsultaInterpretada
        {
            ConsultaValida = resultado.ConsultaValida,
            Reconocida = true,
            Normalizada = normalizada,
            Frase = ArmarFrase(resultado),
            NumeroOrden = numeroOrden,
            NumeroPlanta = numeroPlanta,
            NivelStock = nivel,
            Agotados = agotados,
            TextoRestante = restante,
            QuierePersonas = quierePersonas,
            TextoPersona = persona ?? "",
            FiltroSolicitud = filtroSolicitud,
            NumeroSolicitud = numeroSolicitud,
            TextoFichaTecnica = ficha ?? "",
            Navegacion = navegacion
        };
    }

    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return "";

        var descompuesto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(c) || c is ' ' or '-' or '@' or '.')
                sb.Append(c);
            else
                sb.Append(' ');
        }

        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }

    private static string Corregir(string normalizada)
    {
        if (normalizada.Length == 0)
            return normalizada;

        var partes = normalizada.Split(' ');
        for (var i = 0; i < partes.Length; i++)
        {
            if (ErroresComunes.TryGetValue(partes[i], out var bien))
                partes[i] = bien;
        }

        return string.Join(' ', partes);
    }

    private static int ContarAlfanumericos(string texto) =>
        texto.Count(char.IsLetterOrDigit);

    private static bool EsSinSentido(string texto)
    {
        if (texto.IndexOfAny(['@']) >= 0)
            return false;

        var letras = texto.Count(char.IsLetter);
        var digitos = texto.Count(char.IsDigit);
        if (letras == 0 && digitos == 0)
            return true;

        if (digitos == 0 && letras > 0 && !Regex.IsMatch(texto, "[aeiou]"))
            return true;

        return false;
    }

    private static NavegacionBusqueda? TomarNavegacion(ref string work)
    {
        if (ContieneFrase(work, "movimientos de stock") || ContieneFrase(work, "movimiento de stock")
            || ContieneFrase(work, "historial de stock") || ContieneFrase(work, "movimientos")
            || ContieneFrase(work, "movimiento") || ContieneFrase(work, "historial"))
        {
            Quitar(ref work, "movimientos de stock", "movimiento de stock", "historial de stock", "movimientos", "movimiento", "historial");
            return NavegacionBusqueda.Movimientos;
        }

        if (ContieneFrase(work, "reingreso desde etapas") || ContieneFrase(work, "reingreso"))
        {
            Quitar(ref work, "reingreso desde etapas", "reingreso");
            return NavegacionBusqueda.Reingreso;
        }

        if (ContieneFrase(work, "grupos de confeccion") || ContieneFrase(work, "grupo de confeccion")
            || ContieneFrase(work, "grupos") || ContieneFrase(work, "grupo"))
        {
            Quitar(ref work, "grupos de confeccion", "grupo de confeccion", "grupos", "grupo");
            return NavegacionBusqueda.Grupos;
        }

        return null;
    }

    private static FiltroSolicitudBusqueda? TomarFiltroSolicitud(ref string work)
    {
        if (Regex.IsMatch(work, @"solicitud(?:es)?\s+pendientes?"))
        {
            work = Regex.Replace(work, @"solicitud(?:es)?\s+pendientes?", " ");
            return FiltroSolicitudBusqueda.Pendientes;
        }

        if (Regex.IsMatch(work, @"solicitud(?:es)?\s+aprobadas?"))
        {
            work = Regex.Replace(work, @"solicitud(?:es)?\s+aprobadas?", " ");
            return FiltroSolicitudBusqueda.Aprobadas;
        }

        if (Regex.IsMatch(work, @"solicitud(?:es)?\s+rechazadas?"))
        {
            work = Regex.Replace(work, @"solicitud(?:es)?\s+rechazadas?", " ");
            return FiltroSolicitudBusqueda.Rechazadas;
        }

        return null;
    }

    private static string? TomarPersona(ref string work)
    {
        var correo = Regex.Match(work, @"[a-z0-9._]+@[a-z0-9.]+");
        if (correo.Success)
        {
            work = work.Replace(correo.Value, " ");
            return correo.Value;
        }

        var persona = Regex.Match(work, @"(?:instructor|usuario|usuaria)\s+([a-z0-9][a-z0-9 .-]{0,40})");
        if (!persona.Success)
            return null;

        work = work.Remove(persona.Index, persona.Length);
        return persona.Groups[1].Value.Trim();
    }

    private static (StockNivel? Nivel, bool Agotados) TomarNivel(ref string work)
    {
        if (ContieneFrase(work, "sin minimo definido") || ContieneFrase(work, "sin minimo"))
        {
            Quitar(ref work, "sin minimo definido", "sin minimo");
            return (StockNivel.SinMinimo, false);
        }

        if (ContieneFrase(work, "agotados") || ContieneFrase(work, "agotado"))
        {
            Quitar(ref work, "agotados", "agotado");
            return (StockNivel.Critico, true);
        }

        if (ContieneFrase(work, "stock critico") || ContieneFrase(work, "nivel critico")
            || ContieneFrase(work, "criticos") || ContieneFrase(work, "critico"))
        {
            Quitar(ref work, "stock critico", "nivel critico", "criticos", "critico");
            return (StockNivel.Critico, false);
        }

        if (ContieneFrase(work, "stock bajo") || ContieneFrase(work, "nivel bajo")
            || ContieneFrase(work, "bajo minimo") || ContieneFrase(work, "bajos"))
        {
            Quitar(ref work, "stock bajo", "nivel bajo", "bajo minimo", "bajos");
            return (StockNivel.Bajo, false);
        }

        return (null, false);
    }

    private static int? TomarEntero(ref string work, string patron)
    {
        var match = Regex.Match(work, patron);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var numero))
            return null;

        work = work.Remove(match.Index, match.Length);
        return numero;
    }

    private static string? Tomar(ref string work, string patron)
    {
        var match = Regex.Match(work, patron);
        if (!match.Success)
            return null;

        work = work.Remove(match.Index, match.Length);
        return match.Groups[1].Value.Trim();
    }

    private static string Espaciar(string work) =>
        " " + Regex.Replace(work.Trim(), @"\s+", " ") + " ";

    private static bool ContieneFrase(string work, string frase) =>
        Espaciar(work).Contains(" " + frase + " ", StringComparison.Ordinal);

    private static void Quitar(ref string work, params string[] frases)
    {
        foreach (var frase in frases)
            work = work.Replace(" " + frase + " ", " ");
        work = Regex.Replace(work, @"\s+", " ");
    }

    private static string LimpiarRestante(string work)
    {
        var partes = Regex.Replace(work, @"\s+", " ").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var utiles = partes.Where(p => !Stopwords.Contains(p) && p is not ("stock" or "nivel" or "material" or "materiales" or "insumo" or "insumos")).ToArray();
        return string.Join(' ', utiles);
    }

    private static string ArmarFrase(ConsultaInterpretada c)
    {
        if (c.Navegacion == NavegacionBusqueda.Movimientos)
            return "Entendí: movimientos de stock" + Sufijo(c);
        if (c.Navegacion == NavegacionBusqueda.Reingreso)
            return "Entendí: reingreso desde etapas" + Sufijo(c);
        if (c.Navegacion == NavegacionBusqueda.Grupos)
            return "Entendí: grupos de confección" + Sufijo(c);
        if (c.QuierePersonas)
            return "Entendí: personas";
        if (c.NumeroSolicitud is int sol)
            return "Entendí: solicitud " + sol;
        if (c.FiltroSolicitud == FiltroSolicitudBusqueda.Pendientes)
            return "Entendí: solicitudes pendientes";
        if (c.FiltroSolicitud == FiltroSolicitudBusqueda.Aprobadas)
            return "Entendí: solicitudes aprobadas";
        if (c.FiltroSolicitud == FiltroSolicitudBusqueda.Rechazadas)
            return "Entendí: solicitudes rechazadas";
        if (!string.IsNullOrEmpty(c.TextoFichaTecnica))
            return "Entendí: ficha técnica " + c.TextoFichaTecnica;
        if (c.NumeroOrden is int orden && c.NivelStock is null && c.NumeroPlanta is null && c.TextoRestante.Length == 0)
            return "Entendí: orden de producción " + orden;

        var nivel = c.NivelStock switch
        {
            StockNivel.Critico when c.Agotados => "materiales agotados",
            StockNivel.Critico => "materiales con stock crítico",
            StockNivel.Bajo => "materiales con stock bajo",
            StockNivel.SinMinimo => "materiales sin mínimo definido",
            _ => null
        };

        if (nivel is not null && c.NumeroPlanta is int planta)
        {
            var extra = c.TextoRestante.Length > 0 ? " · " + c.TextoRestante : "";
            return "Entendí: " + nivel + " en la planta de inventario " + planta + extra;
        }

        if (nivel is not null)
        {
            var extra = c.TextoRestante.Length > 0 ? " · " + c.TextoRestante : "";
            return "Entendí: " + nivel + extra;
        }

        if (c.NumeroPlanta is int soloPlanta && c.TextoRestante.Length == 0)
            return "Entendí: inventario de la planta de inventario " + soloPlanta;

        if (c.NumeroPlanta is int plantaNombre && c.TextoRestante.Length > 0)
            return "Entendí: " + c.TextoRestante + " en la planta de inventario " + plantaNombre;

        if (c.NumeroOrden is int otraOrden)
            return "Entendí: orden de producción " + otraOrden;

        if (c.TextoRestante.Length > 0)
            return "Entendí: " + c.TextoRestante;

        return "Entendí: búsqueda";
    }

    private static string Sufijo(ConsultaInterpretada c)
    {
        var extra = "";
        if (c.NumeroPlanta is int planta)
            extra += " en la planta de inventario " + planta;
        if (c.TextoRestante.Length > 0)
            extra += " · " + c.TextoRestante;
        return extra;
    }
}
