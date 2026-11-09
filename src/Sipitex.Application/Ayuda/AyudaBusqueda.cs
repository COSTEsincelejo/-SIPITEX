using System.Globalization;
using System.Text;

namespace Sipitex.Application.Ayuda;

// Búsqueda del centro de ayuda. El catálogo es corto: alcanza con normalizar,
// quitar palabras vacías y una distancia de edición. No hace falta una librería.
public static class AyudaBusqueda
{
    public const int Maximo = 6;

    private static readonly HashSet<string> Vacias = new(StringComparer.Ordinal)
    {
        "a", "al", "con", "de", "del", "el", "en", "es", "esa", "ese", "eso",
        "esta", "este", "esto", "la", "las", "lo", "los", "me", "mi", "mis",
        "o", "para", "por", "que", "se", "si", "su", "sus", "un", "una", "y", "como"
    };

    // Verbos de intención. Si faltan en un tema no lo descartan: "crear usuario"
    // igual encuentra la guía de usuarios aunque otra guía también diga "crear".
    private static readonly HashSet<string> Amplias = new(StringComparer.Ordinal)
    {
        "crear", "creo", "nuevo", "nueva", "agregar", "registrar", "registro",
        "ver", "veo", "consultar", "funciona", "funcionan", "hacer", "hace",
        "usar", "uso", "abrir", "quiero", "solicitar", "solicito", "donde",
        "necesito", "puedo"
    };

    public static AyudaConsultaResultado Buscar(string? consulta, IEnumerable<TemaAyuda> temas)
    {
        var preparados = temas.Select((tema, indice) => Preparar(tema, indice)).ToArray();
        var tokens = Tokenizar(consulta, preparados);
        if (tokens.Count == 0)
            return AyudaConsultaResultado.Vacia;

        var hallados = new List<Hallazgo>();
        foreach (var tema in preparados)
        {
            var hallazgo = Puntuar(tokens, tema);
            if (hallazgo is not null)
                hallados.Add(hallazgo);
        }

        if (hallados.Count == 0)
            return AyudaConsultaResultado.Vacia;

        var hayExacto = hallados.Any(h => !h.Fuzzy);
        var mejor = hallados.Max(h => h.Puntos);
        var resultados = hallados
            .Where(h => !hayExacto || !h.Fuzzy)
            // Un parecido leve (por ejemplo "usurio" dentro de "usuario" en otra guía) no se ofrece como sugerencia.
            .Where(h => !h.Fuzzy || h.Puntos * 2 >= mejor)
            .OrderByDescending(h => h.Puntos)
            .ThenBy(h => h.Orden)
            .Take(Maximo)
            .Select(h => new AyudaResultado(h.Id, h.Titulo, h.Descripcion, h.Categoria, h.Tipo, h.Fuzzy))
            .ToArray();

        var sugerencia = resultados.Length > 0 && resultados.All(r => r.Sugerencia);
        return new AyudaConsultaResultado(sugerencia, resultados);
    }

    private static List<string> Tokenizar(string? consulta, TemaPreparado[] temas)
    {
        var tokens = new List<string>();
        foreach (var token in Plegar(consulta).Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Vacias.Contains(token))
                continue;
            if (token.Length < 4 && !temas.Any(t => t.Palabras.Contains(token)))
                continue;
            tokens.Add(token);
        }

        return tokens;
    }

    private static Hallazgo? Puntuar(List<string> tokens, TemaPreparado tema)
    {
        var puntos = 0;
        var fuzzy = false;
        foreach (var token in tokens)
        {
            var mejor = Mejor(token, tema.Palabras);
            if (mejor.Puntos == 0)
            {
                if (!EsAmplia(token))
                    return null;
                continue;
            }

            puntos += mejor.Puntos;
            if (mejor.Fuzzy)
                fuzzy = true;
        }

        if (puntos == 0)
            return null;

        puntos += Bonificacion(tokens, tema);
        var opcion = Elegir(tokens, tema);
        return new Hallazgo(tema.Tema.Id, opcion.Titulo, opcion.Descripcion, tema.Tema.Categoria, opcion.Tipo, tema.Orden, puntos, fuzzy);
    }

    private static int Bonificacion(List<string> tokens, TemaPreparado tema)
    {
        var extra = 0;
        var frase = string.Join(' ', tokens);
        if (frase.Length >= 10 && tema.Texto.Contains(frase, StringComparison.Ordinal))
            extra += 14;

        foreach (var intencion in tema.Intenciones)
        {
            if (Cubre(intencion, tokens))
                extra += 24;
        }

        return extra;
    }

    private static bool Cubre(string intencion, List<string> tokens)
    {
        var partes = intencion.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !Vacias.Contains(p))
            .ToArray();
        var amplias = partes.Where(EsAmplia).ToArray();
        var contenido = partes.Where(p => !EsAmplia(p)).ToArray();
        var especificos = tokens.Where(t => !EsAmplia(t)).ToArray();
        if (contenido.Length == 0 || especificos.Length == 0)
            return false;
        // "solicitar materiales" no debe ganar si la persona solo escribió "materiales".
        if (amplias.Any(a => !tokens.Any(t => SeParecen(a, t))))
            return false;

        return contenido.All(p => especificos.Any(t => SeParecen(p, t)))
            && especificos.All(t => contenido.Any(p => SeParecen(p, t)));
    }

    private static bool SeParecen(string a, string b)
    {
        if (a == b)
            return true;
        var raizA = Raiz(a);
        var raizB = Raiz(b);
        if (raizA.Length >= 4 && raizA == raizB)
            return true;
        if (a.Length < 5 || b.Length < 5 || Math.Abs(a.Length - b.Length) > Tope(a))
            return false;
        return PrefijoComun(a, b) >= 2 && Distancia(a, b) <= Tope(a);
    }

    private static Opcion Elegir(List<string> tokens, TemaPreparado tema)
    {
        var mejor = tema.Opciones[0];
        var mejorPuntos = PuntosOpcion(tokens, mejor);
        for (var i = 1; i < tema.Opciones.Length; i++)
        {
            var puntos = PuntosOpcion(tokens, tema.Opciones[i]);
            if (puntos > mejorPuntos)
            {
                mejorPuntos = puntos;
                mejor = tema.Opciones[i];
            }
        }

        return mejor;
    }

    private static int PuntosOpcion(List<string> tokens, Opcion opcion)
    {
        var puntos = 0;
        foreach (var token in tokens)
        {
            if (opcion.Palabras.Contains(token) || opcion.Palabras.Any(p => Raiz(p).Length >= 4 && Raiz(p) == Raiz(token)))
                puntos += 2;
        }

        if (opcion.Frases.Any(f => Cubre(f, tokens)))
            puntos += 5;
        return puntos;
    }

    private static (int Puntos, bool Fuzzy) Mejor(string token, HashSet<string> palabras)
    {
        if (palabras.Contains(token))
            return (12, false);

        var raiz = Raiz(token);
        if (raiz.Length >= 4 && palabras.Any(p => Raiz(p) == raiz))
            return (10, false);

        if (token.Length >= 4)
        {
            foreach (var palabra in palabras)
            {
                if (palabra.Length < 4 || Math.Abs(palabra.Length - token.Length) > 3)
                    continue;
                if (palabra.StartsWith(token, StringComparison.Ordinal) || token.StartsWith(palabra, StringComparison.Ordinal))
                    return (7, false);
            }
        }

        if (token.Length < 5)
            return (0, false);

        var tope = Tope(token);
        foreach (var palabra in palabras)
        {
            if (palabra.Length < 5 || Math.Abs(palabra.Length - token.Length) > tope)
                continue;
            if (PrefijoComun(token, palabra) < 2)
                continue;
            if (Distancia(token, palabra) <= tope)
                return (5, true);
        }

        return (0, false);
    }

    private static int Tope(string token) => token.Length >= 8 ? 2 : 1;

    private static bool EsAmplia(string token) =>
        Amplias.Contains(token) || Amplias.Contains(Raiz(token));

    private static TemaPreparado Preparar(TemaAyuda tema, int orden)
    {
        var intenciones = tema.Intenciones
            .Concat(tema.Variantes.SelectMany(v => v.Frases))
            .Select(Plegar)
            .Where(f => f.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var texto = Plegar(string.Join(' ', new[]
        {
            tema.Pregunta,
            tema.Titulo,
            tema.Resumen,
            tema.Categoria,
            tema.QueEs,
            tema.ParaQueSirve,
            tema.DescripcionBusqueda
        }.Concat(tema.PalabrasClave).Concat(intenciones).Concat(tema.Variantes.Select(v => v.Titulo + " " + v.Descripcion))));

        var opciones = new List<Opcion>
        {
            OpcionDe(tema.Pregunta, tema.DescripcionBusqueda.Length > 0 ? tema.DescripcionBusqueda : tema.Resumen, tema.Tipo, tema.Intenciones)
        };
        opciones.AddRange(tema.Variantes.Select(v => OpcionDe(v.Titulo, v.Descripcion, v.Tipo, v.Frases)));

        return new TemaPreparado(tema, orden, texto, PalabrasDe(texto), intenciones, opciones.ToArray());
    }

    private static Opcion OpcionDe(string titulo, string descripcion, string tipo, string[] frases)
    {
        var plegadas = frases.Select(Plegar).Where(f => f.Length > 0).ToArray();
        var texto = Plegar(titulo + " " + descripcion + " " + string.Join(' ', plegadas));
        return new Opcion(titulo, descripcion, tipo, PalabrasDe(texto), plegadas);
    }

    private static HashSet<string> PalabrasDe(string texto) =>
        texto.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);

    internal static string Plegar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return "";

        var chars = texto.Normalize(NormalizationForm.FormD).ToLowerInvariant().ToCharArray();
        var limpio = new char[chars.Length];
        var n = 0;
        var espacio = false;
        foreach (var c in chars)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            var letra = c is >= 'a' and <= 'z' || c is >= '0' and <= '9';
            if (!letra)
            {
                espacio = n > 0;
                continue;
            }

            if (espacio)
            {
                limpio[n++] = ' ';
                espacio = false;
            }

            limpio[n++] = c;
        }

        return new string(limpio, 0, n);
    }

    private static string Raiz(string palabra)
    {
        if (palabra.Length > 4 && palabra.EndsWith("es", StringComparison.Ordinal))
            return palabra[..^2];
        if (palabra.Length > 4 && palabra.EndsWith('s'))
            return palabra[..^1];
        return palabra;
    }

    private static int PrefijoComun(string a, string b)
    {
        var n = Math.Min(a.Length, b.Length);
        var i = 0;
        while (i < n && a[i] == b[i])
            i++;
        return i;
    }

    private static int Distancia(string a, string b)
    {
        var tope = Tope(a.Length >= b.Length ? a : b);
        if (Math.Abs(a.Length - b.Length) > tope)
            return tope + 1;

        var anterior = new int[b.Length + 1];
        var actual = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
            anterior[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            actual[0] = i;
            var minimo = actual[0];
            for (var j = 1; j <= b.Length; j++)
            {
                var costo = a[i - 1] == b[j - 1] ? 0 : 1;
                actual[j] = Math.Min(Math.Min(actual[j - 1] + 1, anterior[j] + 1), anterior[j - 1] + costo);
                if (actual[j] < minimo)
                    minimo = actual[j];
            }

            if (minimo > tope)
                return tope + 1;
            (anterior, actual) = (actual, anterior);
        }

        return anterior[b.Length];
    }

    private sealed record TemaPreparado(
        TemaAyuda Tema,
        int Orden,
        string Texto,
        HashSet<string> Palabras,
        string[] Intenciones,
        Opcion[] Opciones);

    private sealed record Opcion(string Titulo, string Descripcion, string Tipo, HashSet<string> Palabras, string[] Frases);

    private sealed record Hallazgo(
        string Id,
        string Titulo,
        string Descripcion,
        string Categoria,
        string Tipo,
        int Orden,
        int Puntos,
        bool Fuzzy);
}

public sealed record AyudaResultado(
    string Id,
    string Titulo,
    string Descripcion,
    string Categoria,
    string Tipo,
    bool Sugerencia);

public sealed record AyudaConsultaResultado(bool Sugerencia, IReadOnlyList<AyudaResultado> Resultados)
{
    public static AyudaConsultaResultado Vacia { get; } = new(false, Array.Empty<AyudaResultado>());
}
