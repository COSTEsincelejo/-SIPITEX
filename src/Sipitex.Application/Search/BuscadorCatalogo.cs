using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Sipitex.Application.Authorization;
using Sipitex.Application.Ayuda;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Search;

// Catálogo único de pantallas del menú. El cliente puntúa una copia filtrada por rol
// (wwwroot/js/buscador-global.js) y este mismo criterio se aplica en GET /Buscar/Sugerencias.
public static class BuscadorCatalogo
{
    public const int MaximoResultados = 6;
    public const string Crear = "crear";
    public const string Consultar = "consultar";
    public const string Editar = "editar";
    public const string Eliminar = "eliminar";

    public const string PermisoCrearOrdenes = "CrearOrdenes";
    public const string PermisoFichasTecnicas = "FichasTecnicas";
    public const string PermisoSimularMrp = "SimularMrp";

    private const int Umbral = 36;
    private const int PuntosFrase = 80;
    private const int PuntosFraseBorrosa = 42;
    private const int PuntosIntencion = 32;
    private const int PenalizacionIntencion = 22;
    private const int PuntosToken = 14;
    private const int PuntosTokenBorrosa = 8;
    private const int PuntosCobertura = 12;

    private static readonly string[] Todos = UserRoles.All;
    private static readonly string[] Admin = [UserRoles.Administrador];
    private static readonly string[] AdminInstructor = [UserRoles.Administrador, UserRoles.Instructor];
    private static readonly string[] AdminEncargado = [UserRoles.Administrador, UserRoles.EncargadoDeBodega];
    private static readonly string[] TresRoles =
    [
        UserRoles.Administrador,
        UserRoles.Instructor,
        UserRoles.EncargadoDeBodega
    ];

    private static readonly HashSet<string> Stopwords = new(StringComparer.Ordinal)
    {
        "quiero", "necesito", "favor", "porfa", "el", "la", "los", "las", "de", "del", "al",
        "a", "en", "con", "para", "por", "y", "o", "u", "un", "una", "unos", "unas",
        "mi", "mis", "su", "sus", "que", "es", "son", "lo", "me", "se", "como", "donde",
        "ir", "abrir", "entrar", "sipitex", "hacia", "sobre", "desde", "hasta"
    };

    private static readonly Dictionary<string, string> Intenciones = new(StringComparer.Ordinal)
    {
        ["crear"] = Crear,
        ["creo"] = Crear,
        ["registrar"] = Crear,
        ["agregar"] = Crear,
        ["alta"] = Crear,
        ["nuevo"] = Crear,
        ["nueva"] = Crear,
        ["nuevos"] = Crear,
        ["nuevas"] = Crear,
        ["consultar"] = Consultar,
        ["ver"] = Consultar,
        ["listar"] = Consultar,
        ["mostrar"] = Consultar,
        ["buscar"] = Consultar,
        ["revisar"] = Consultar,
        ["editar"] = Editar,
        ["modificar"] = Editar,
        ["renombrar"] = Editar,
        ["cambiar"] = Editar,
        ["actualizar"] = Editar,
        ["eliminar"] = Eliminar,
        ["desactivar"] = Eliminar,
        ["borrar"] = Eliminar,
        ["quitar"] = Eliminar,
        ["inactivar"] = Eliminar
    };

    private static readonly JsonSerializerOptions JsonOpciones = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Default
    };

    private static readonly AccionCatalogo[] Catalogo = Construir();
    private static readonly AccionPreparada[] Preparadas = Catalogo.Select(Preparar).ToArray();
    // Temas de /Ayuda. Se puntúan con el mismo criterio, pero no desplazan la pantalla del menú.
    private static readonly AccionPreparada[] AyudaPreparadas =
        AyudaCatalogo.AccionesBusquedaSinFiltro().Select(Preparar).ToArray();

    public static IReadOnlyList<AccionCatalogo> Visibles(ClaimsPrincipal user) =>
        Catalogo.Where(a => Permitida(a, user)).ToArray();

    public static IReadOnlyList<AccionSugeridaDto> Frecuentes(ClaimsPrincipal user, int maximo = MaximoResultados) =>
        Visibles(user)
            .Where(a => a.Prioridad > 0)
            .OrderBy(a => a.Prioridad)
            .ThenBy(a => a.Titulo, StringComparer.Ordinal)
            .Take(maximo)
            .Select(a => ADto(a, 0))
            .ToArray();

    public static IReadOnlyList<AccionSugeridaDto> Buscar(string? consulta, ClaimsPrincipal user, int maximo = MaximoResultados)
    {
        var lectura = Leer(consulta);
        if (!lectura.Valida)
            return [];

        var visibles = new HashSet<string>(Visibles(user).Select(a => a.Url + "|" + a.Titulo), StringComparer.Ordinal);
        var hallazgos = new List<AccionSugeridaDto>();
        foreach (var preparada in Preparadas)
        {
            var clave = preparada.Accion.Url + "|" + preparada.Accion.Titulo;
            if (!visibles.Contains(clave))
                continue;

            var puntaje = Puntuar(lectura, preparada);
            if (puntaje < Umbral)
                continue;

            hallazgos.Add(ADto(preparada.Accion, puntaje));
        }

        var pantallas = hallazgos
            .OrderByDescending(a => a.Puntaje)
            .ThenBy(a => PrioridadOrden(a.Titulo))
            .ThenBy(a => a.Titulo, StringComparer.Ordinal)
            .Take(maximo)
            .ToList();

        return ReservarTemaDeAyuda(pantallas, lectura, user, maximo);
    }

    // El mejor tema de ayuda entra en el segundo puesto (o en el primero si no hay pantalla).
    private static IReadOnlyList<AccionSugeridaDto> ReservarTemaDeAyuda(
        List<AccionSugeridaDto> pantallas,
        Lectura lectura,
        ClaimsPrincipal user,
        int maximo)
    {
        AccionSugeridaDto? mejor = null;
        foreach (var preparada in AyudaPreparadas)
        {
            if (!preparada.Accion.RolesPermitidos.Any(user.IsInRole))
                continue;

            var puntaje = Puntuar(lectura, preparada);
            if (puntaje < Umbral)
                continue;
            if (mejor is not null && (puntaje < mejor.Puntaje
                || (puntaje == mejor.Puntaje && string.Compare(preparada.Accion.Titulo, mejor.Titulo, StringComparison.Ordinal) >= 0)))
                continue;

            mejor = ADto(preparada.Accion, puntaje);
        }

        if (mejor is null || pantallas.Any(p => p.Url == mejor.Url))
            return pantallas;

        var indice = pantallas.Count == 0 ? 0 : 1;
        pantallas.Insert(indice, mejor);
        if (pantallas.Count > maximo)
            pantallas.RemoveAt(pantallas.Count - 1);
        return pantallas;
    }

    // JSON ya recortado al rol, para incrustarlo en el layout y puntuar en el navegador.
    public static string JsonVisibles(ClaimsPrincipal user)
    {
        var lista = Visibles(user)
            .Concat(AyudaCatalogo.AccionesBusquedaSinFiltro().Where(a => a.RolesPermitidos.Any(user.IsInRole)))
            .Select(a => new
        {
            a.Titulo,
            a.Descripcion,
            a.Url,
            a.Icono,
            a.Categoria,
            Intencion = a.Intencion ?? "",
            Palabras = a.PalabrasClave.Concat(a.Sinonimos).Append(a.Titulo).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            a.Prioridad,
            EsAyuda = a.Categoria == "Ayuda"
        });
        var json = JsonSerializer.Serialize(lista, JsonOpciones);
        return json.Replace("<", "\\u003c", StringComparison.Ordinal);
    }

    private static int PrioridadOrden(string titulo)
    {
        foreach (var accion in Catalogo)
        {
            if (string.Equals(accion.Titulo, titulo, StringComparison.Ordinal))
                return accion.Prioridad == 0 ? 1000 : accion.Prioridad;
        }

        return 1000;
    }

    private static AccionSugeridaDto ADto(AccionCatalogo accion, int puntaje) =>
        new(accion.Titulo, accion.Descripcion, accion.Url, accion.Icono, accion.Categoria, puntaje);

    private static bool Permitida(AccionCatalogo accion, ClaimsPrincipal user)
    {
        if (!accion.RolesPermitidos.Any(user.IsInRole))
            return false;

        return accion.Permiso switch
        {
            PermisoCrearOrdenes => PermissionRules.PuedeCrearOrdenes(user),
            PermisoFichasTecnicas => PermissionRules.PuedeGestionarFichasTecnicas(user),
            PermisoSimularMrp => PermissionRules.PuedeSimularMrp(user),
            _ => true
        };
    }

    private static Lectura Leer(string? consulta)
    {
        var normalizada = Normalizar(consulta);
        if (normalizada.Count(char.IsLetterOrDigit) < 2)
            return Lectura.Invalida;

        var tokens = normalizada.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string? intencion = null;
        var utiles = new List<string>();
        foreach (var token in tokens)
        {
            if (intencion is null && Intenciones.TryGetValue(token, out var detectada))
            {
                intencion = detectada;
                continue;
            }

            if (Stopwords.Contains(token))
                continue;

            utiles.Add(Raiz(token));
        }

        var clave = string.Join(' ', utiles);
        if (clave.Length == 0 && intencion is null)
            return Lectura.Invalida;

        return new Lectura(true, intencion, clave, utiles);
    }

    private static int Puntuar(Lectura lectura, AccionPreparada preparada)
    {
        var puntaje = 0;
        var fraseExacta = false;
        if (lectura.Clave.Length > 0)
        {
            foreach (var frase in preparada.Frases)
            {
                if (frase == lectura.Clave)
                {
                    puntaje += PuntosFrase;
                    fraseExacta = true;
                    break;
                }
            }

            if (!fraseExacta && !lectura.Clave.Contains(' '))
            {
                var mejor = preparada.Frases
                    .Where(f => !f.Contains(' '))
                    .Select(f => Distancia(lectura.Clave, f))
                    .DefaultIfEmpty(99)
                    .Min();
                if (mejor <= Tope(lectura.Clave))
                    puntaje += PuntosFraseBorrosa;
            }
        }

        var cubiertos = 0;
        foreach (var token in lectura.Tokens)
        {
            var mejor = 99;
            foreach (var candidato in preparada.Tokens)
            {
                if (candidato == token)
                {
                    mejor = 0;
                    break;
                }

                mejor = Math.Min(mejor, Distancia(token, candidato));
            }

            if (mejor == 0)
            {
                puntaje += PuntosToken;
                cubiertos++;
            }
            else if (mejor <= Tope(token))
            {
                puntaje += PuntosTokenBorrosa;
                cubiertos++;
            }
        }

        if (lectura.Tokens.Count > 0 && cubiertos == lectura.Tokens.Count)
            puntaje += PuntosCobertura;

        // Un token que es, él solo, una frase del catálogo ("op", "stock", "orden") suma aparte.
        foreach (var token in lectura.Tokens)
        {
            if (preparada.Frases.Contains(token))
                puntaje += 24;
        }

        if (lectura.Intencion is not null && preparada.Accion.Intencion == lectura.Intencion)
            puntaje += PuntosIntencion;
        else if (lectura.Intencion is not null && preparada.Accion.Intencion is not null)
            puntaje -= PenalizacionIntencion;

        // Si el usuario solo escribió el verbo ("crear", "ver"), priorizamos esa intención.
        if (lectura.Tokens.Count == 0 && lectura.Intencion is not null && preparada.Accion.Intencion == lectura.Intencion)
            puntaje += PuntosFrase;

        return puntaje;
    }

    internal static string Normalizar(string? texto)
    {
        var baseTexto = InterpreteConsulta.Normalizar(texto).Replace('-', ' ');
        return baseTexto;
    }

    private static string Raiz(string token)
    {
        if (token.Length > 4 && token.EndsWith("es", StringComparison.Ordinal))
            return token[..^2];
        if (token.Length > 3 && token.EndsWith('s'))
            return token[..^1];
        return token;
    }

    private static int Tope(string token)
    {
        if (token.Length >= 8)
            return 2;
        if (token.Length >= 5)
            return 1;
        return 0;
    }

    private static int Distancia(string a, string b)
    {
        var tope = Math.Max(Tope(a), Tope(b));
        if (tope == 0 || Math.Abs(a.Length - b.Length) > tope)
            return tope + 1;

        var anterior = new int[b.Length + 1];
        var actual = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
            anterior[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            actual[0] = i;
            var minimoFila = actual[0];
            for (var j = 1; j <= b.Length; j++)
            {
                var costo = a[i - 1] == b[j - 1] ? 0 : 1;
                actual[j] = Math.Min(
                    Math.Min(actual[j - 1] + 1, anterior[j] + 1),
                    anterior[j - 1] + costo);
                if (actual[j] < minimoFila)
                    minimoFila = actual[j];
            }

            if (minimoFila > tope)
                return tope + 1;
            (anterior, actual) = (actual, anterior);
        }

        return anterior[b.Length];
    }

    private static AccionPreparada Preparar(AccionCatalogo accion)
    {
        var frases = new HashSet<string>(StringComparer.Ordinal);
        AgregarFrase(frases, accion.Titulo);
        foreach (var frase in accion.PalabrasClave)
            AgregarFrase(frases, frase);
        foreach (var frase in accion.Sinonimos)
            AgregarFrase(frases, frase);

        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var frase in frases)
        {
            foreach (var token in frase.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                tokens.Add(token);
        }

        return new AccionPreparada(accion, frases, tokens);
    }

    private static void AgregarFrase(HashSet<string> destino, string texto)
    {
        var normalizada = Normalizar(texto);
        if (normalizada.Length == 0)
            return;

        var partes = normalizada.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => !Stopwords.Contains(t) && !Intenciones.ContainsKey(t))
            .Select(Raiz);
        var clave = string.Join(' ', partes);
        if (clave.Length > 0)
            destino.Add(clave);

        // Conserva la frase con el verbo para que "crear planta" coincida tal cual.
        var conVerbo = string.Join(' ',
            normalizada.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => !Stopwords.Contains(t))
                .Select(Raiz));
        if (conVerbo.Length > 0)
            destino.Add(conVerbo);
    }

    private static AccionCatalogo[] Construir() =>
    [
        A("Órdenes de producción", "Consultar órdenes, su avance y el flujo por etapas",
            "/Ordenes", "fa-clipboard-list", "Operación", TresRoles,
            ["ordenes", "orden de produccion", "op", "produccion"],
            ["orden", "pedido de produccion", "ordenes de produccion"],
            Consultar, prioridad: 1),
        A("Crear orden de producción", "Generar una orden y su cálculo MRP",
            "/Ordenes#CreateOrder_ProductName", "fa-clipboard-list", "Operación", AdminInstructor,
            ["crear orden", "nueva orden", "orden de produccion"],
            ["generar orden", "alta de orden", "op nueva"],
            Crear, PermisoCrearOrdenes),
        A("MRP / Materiales", "Fichas técnicas (BOM) y requerimiento de materiales",
            "/Mrp", "fa-diagram-project", "Operación", TresRoles,
            ["mrp", "materiales", "bom", "ficha tecnica"],
            ["requerimiento", "lista de materiales", "explosion de materiales"],
            Consultar, prioridad: 2),
        A("Nueva ficha técnica", "Crear el BOM de un producto",
            "/Mrp/Create#Form_ProductName", "fa-diagram-project", "Operación", TresRoles,
            ["nueva ficha tecnica", "crear ficha tecnica", "crear bom"],
            ["alta de producto", "nuevo producto mrp"],
            Crear, PermisoFichasTecnicas),
        A("Simular MRP", "Calcular el requerimiento neto de un producto",
            "/Mrp#Simulation_ProductName", "fa-diagram-project", "Operación", TresRoles,
            ["simular mrp", "simulacion", "requerimiento neto"],
            ["calcular materiales", "explosion"],
            Consultar, PermisoSimularMrp),
        A("Inventario por planta", "Consultar insumos, stock y el resumen de cada planta",
            "/PlantasInventario", "fa-warehouse", "Operación", TresRoles,
            ["inventario por planta", "inventario", "stock", "bodega", "plantas de inventario"],
            ["almacen", "existencias", "ver stock", "inventario bodega", "insumos", "inventario por bodega", "planta", "catalogo de plantas"],
            Consultar, prioridad: 3),
        A("Crear planta de inventario", "Registrar una planta y enfocar el nombre",
            "/PlantasInventario#Nombre", "fa-warehouse", "Operación", Admin,
            ["crear planta", "nueva planta", "planta"],
            ["alta de planta", "agregar bodega", "nuevo almacen"],
            Crear),
        A("Editar planta de inventario", "Renombrar una planta desde el catálogo",
            "/PlantasInventario", "fa-warehouse", "Operación", Admin,
            ["editar planta", "renombrar planta"],
            ["cambiar nombre planta", "modificar bodega"],
            Editar),
        A("Desactivar planta de inventario", "Eliminar o desactivar una planta del catálogo",
            "/PlantasInventario", "fa-warehouse", "Operación", Admin,
            ["eliminar planta", "desactivar planta"],
            ["borrar planta", "inactivar bodega"],
            Eliminar),
        A("Reactivar planta de inventario", "Volver a activar una planta sin reasignar stock",
            "/PlantasInventario#gestion-plantas", "fa-warehouse", "Operación", Admin,
            ["reactivar planta", "activar planta"],
            ["mostrar inactivas", "planta inactiva"],
            Editar),
        A("Solicitudes de materiales", "Cola de solicitudes para aprobar o rechazar",
            "/PlantasInventarioSolicitudes", "fa-truck-ramp-box", "Operación", TresRoles,
            ["solicitudes de materiales", "solicitudes", "cola de solicitudes"],
            ["pedidos de materiales", "aprobar solicitud", "solicitudes pendientes"],
            Consultar, prioridad: 5),
        A("Materiales de órdenes", "Revisar y entregar insumos asociados a una orden",
            "/PlantasInventarioOrdenes", "fa-clipboard-check", "Operación", TresRoles,
            ["materiales de ordenes", "materiales de una orden", "entrega de materiales"],
            ["materiales orden", "insumos de la orden", "bom de la orden"],
            Consultar, prioridad: 6),
        A("Reingreso desde etapas", "Devolver material desde una etapa al inventario",
            "/PlantasInventarioOrdenes/Reingreso", "fa-rotate-left", "Operación", AdminEncargado,
            ["reingreso", "reingreso desde etapas"],
            ["devolucion", "regreso de material", "reintegro"],
            Consultar, prioridad: 8),
        A("Movimientos de stock", "Historial de entradas y salidas de inventario",
            "/PlantasInventario/Movimientos", "fa-clock-rotate-left", "Operación", AdminEncargado,
            ["movimientos", "movimientos de stock", "historial de stock"],
            ["kardex", "entradas y salidas", "movimiento"],
            Consultar, prioridad: 9),
        A("Fichas y producción", "Grupos SENA, sesiones y avance de producción",
            "/Fichas", "fa-people-group", "Operación", AdminInstructor,
            ["fichas", "fichas y produccion", "grupos sena"],
            ["sesiones", "produccion de fichas", "grupo de aprendices"],
            Consultar, prioridad: 7),
        A("Nueva ficha de grupo", "Abrir una ficha de producción para un grupo",
            "/Fichas#CreateFicha_NumeroGrupo", "fa-people-group", "Operación", AdminInstructor,
            ["nueva ficha", "crear ficha", "ficha de grupo"],
            ["registrar grupo", "alta de ficha"],
            Crear),
        A("Mis solicitudes", "Solicitudes de materiales hechas por el instructor",
            "/SolicitudesMaterial", "fa-clipboard-list", "Operación", AdminInstructor,
            ["mis solicitudes", "solicitudes del instructor"],
            ["pedidos que hice", "estado de mis pedidos"],
            Consultar),
        A("Solicitar insumos", "Nueva solicitud por descripción, sin código de catálogo",
            "/SolicitudesMaterial/SolicitarInsumos", "fa-comment-dots", "Operación", AdminInstructor,
            ["solicitar insumos", "nueva solicitud", "solicitud de materiales"],
            ["pedir materiales", "pedido libre", "nueva solicitud de materiales"],
            Crear, prioridad: 10),
        A("Control de calidad", "Registrar y consultar inspecciones de calidad",
            "/Calidad", "fa-clipboard-check", "Operación", AdminInstructor,
            ["calidad", "control de calidad"],
            ["inspeccion", "revisiones de calidad"],
            Consultar, prioridad: 16),
        A("Grupos de confección", "Grupos que confeccionan prendas de una orden",
            "/GruposConfeccion", "fa-people-group", "Operación", AdminInstructor,
            ["grupos de confeccion", "confeccion"],
            ["grupo de costura", "equipo de confeccion"],
            Consultar, prioridad: 18),
        A("Consumo de materiales", "Registrar el material consumido en producción",
            "/Consumos", "fa-scissors", "Operación", AdminInstructor,
            ["consumo", "consumo de materiales"],
            ["material gastado", "consumo de tela"],
            Consultar, prioridad: 19),
        A("Actas de ingreso y egreso", "Actas firmadas de entrada y salida de prendas",
            "/Actas", "fa-file-signature", "Operación", TresRoles,
            ["actas", "actas de ingreso", "ingreso y egreso"],
            ["acta", "documento de entrega"],
            Consultar, prioridad: 11),
        A("Nueva acta", "Crear un acta de ingreso o egreso",
            "/Actas/Create", "fa-file-signature", "Operación", TresRoles,
            ["nueva acta", "crear acta"],
            ["registrar acta", "alta de acta"],
            Crear),
        A("Trazabilidad", "Códigos de prenda y su recorrido",
            "/Trazabilidad", "fa-barcode", "Operación", TresRoles,
            ["trazabilidad", "codigo de prenda", "barcode"],
            ["seguimiento de prenda", "rastreo"],
            Consultar, prioridad: 12),
        A("Costeo de prendas", "Costo de materiales y mano de obra por prenda",
            "/Costos", "fa-coins", "Análisis", AdminInstructor,
            ["costos", "costeo", "costeo de prendas"],
            ["precio de la prenda", "costo de produccion"],
            Consultar, prioridad: 17),
        A("Estadísticas", "Tableros de producción, inventario y calidad",
            "/Estadisticas", "fa-chart-line", "Análisis", Todos,
            ["estadisticas", "indicadores", "tablero"],
            ["graficas", "dashboard de estadisticas"],
            Consultar, prioridad: 14),
        A("Reportes", "Exportar inventario, órdenes, calidad y actividad",
            "/Reportes", "fa-file-export", "Análisis", Todos,
            ["reportes", "exportar", "pdf", "excel"],
            ["informe", "descargar reporte"],
            Consultar, prioridad: 13),
        A("Alertas", "Avisos de stock y solicitudes",
            "/Alertas", "fa-bell", "Análisis", Todos,
            ["alertas", "notificaciones", "avisos"],
            ["campana", "alertas de stock"],
            Consultar, prioridad: 15),
        A("Auditoría", "Registro de actividad de los usuarios",
            "/Auditoria", "fa-clipboard-list", "Administración", Admin,
            ["auditoria", "registro de actividad"],
            ["log", "bitacora", "historial de acciones"],
            Consultar, prioridad: 20),
        A("Usuarios", "Cuentas, roles y acceso al sistema",
            "/Account/Users", "fa-users-gear", "Administración", Admin,
            ["usuarios", "cuentas", "roles"],
            ["administrar usuarios", "permisos"],
            Consultar, prioridad: 21),
        A("Crear usuario", "Dar de alta una cuenta y enfocar el nombre",
            "/Account/CreateUser#Nombre", "fa-users-gear", "Administración", Admin,
            ["crear usuario", "nuevo usuario", "usuario"],
            ["alta de usuario", "nueva cuenta"],
            Crear),
        A("Mi perfil", "Nombre, correo, foto y contraseña de la sesión",
            "/Account/Profile", "fa-user", "Administración", Todos,
            ["perfil", "mi perfil", "mi cuenta"],
            ["contraseña", "foto", "datos personales"],
            Consultar, prioridad: 22),
        A("Inicio", "Panel principal después de entrar",
            "/Home", "fa-house", "Análisis", Todos,
            ["inicio", "panel", "home"],
            ["portada", "pagina principal"],
            Consultar)
    ];

    private static AccionCatalogo A(
        string titulo,
        string descripcion,
        string url,
        string icono,
        string categoria,
        string[] roles,
        string[] palabras,
        string[] sinonimos,
        string? intencion = null,
        string? permiso = null,
        int prioridad = 0) =>
        new()
        {
            Titulo = titulo,
            Descripcion = descripcion,
            Url = url,
            Icono = icono,
            Categoria = categoria,
            RolesPermitidos = roles,
            PalabrasClave = palabras,
            Sinonimos = sinonimos,
            Intencion = intencion,
            Permiso = permiso,
            Prioridad = prioridad
        };

    private readonly record struct Lectura(bool Valida, string? Intencion, string Clave, List<string> Tokens)
    {
        public static Lectura Invalida => new(false, null, "", []);
    }

    private readonly record struct AccionPreparada(
        AccionCatalogo Accion,
        HashSet<string> Frases,
        HashSet<string> Tokens);
}

public sealed class AccionCatalogo
{
    public required string Titulo { get; init; }
    public required string Descripcion { get; init; }
    public required string Url { get; init; }
    public required string Icono { get; init; }
    public required string Categoria { get; init; }
    public string[] PalabrasClave { get; init; } = [];
    public string[] Sinonimos { get; init; } = [];
    public string[] RolesPermitidos { get; init; } = [];
    public string? Intencion { get; init; }
    public string? Permiso { get; init; }
    public int Prioridad { get; init; }
}
