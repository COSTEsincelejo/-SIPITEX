namespace Sipitex.Application.Ayuda;

// Títulos en forma de pregunta, sinónimos e intenciones. No entran al buscador
// global: solo los usa la búsqueda de esta página, para no cambiar el orden
// de las pantallas del menú.
public static class AyudaDidactica
{
    public static FichaDidactica? De(string id) =>
        Tabla.TryGetValue(id, out var ficha) ? ficha : null;

    private static readonly Dictionary<string, FichaDidactica> Tabla = new(StringComparer.Ordinal)
    {
        ["primeros-pasos"] = new(
            "¿Por dónde empiezo en SIPITEX?",
            "Explicación",
            "Sirve para entrar, reconocer el menú y saber qué ve cada rol antes de registrar producción o inventario.",
            ["primeros pasos", "iniciar sesion", "entrar al sistema", "como empiezo"],
            []),

        ["ordenes"] = new(
            "¿Cómo creo una orden de producción?",
            "Paso a paso",
            "Junta un producto con ficha técnica, una cantidad y una fecha límite. Al crearla, el sistema calcula los materiales.",
            ["crear orden", "ordenes de produccion", "orden de produccion", "op", "generar orden"],
            [
                new VarianteAyuda(
                    "¿Cómo funcionan las órdenes de producción?",
                    "La orden pasa de pendiente de aprobación a en producción, finalizada o cancelada. Las etapas avanzan de una en una.",
                    "Explicación",
                    ["como funciona una orden", "como funcionan las ordenes", "que es una orden", "estados de la orden"])
            ]),

        ["mrp"] = new(
            "¿Cómo funciona el MRP?",
            "Explicación",
            "El MRP calcula los materiales a partir de la ficha técnica (la receta) y de la cantidad que va a fabricar.",
            ["mrp", "requerimientos de material", "requerimiento de materiales", "materiales", "bom", "receta"],
            [
                new VarianteAyuda(
                    "¿Cómo registro una ficha técnica?",
                    "La ficha técnica es la receta del producto: qué materiales lleva y en qué cantidad. Se crea en MRP / Materiales.",
                    "Paso a paso",
                    ["ficha", "ficha tecnica", "como hacer ficha", "como se hace una ficha", "como registrar ficha", "hacer una ficha"])
            ]),

        ["plantas"] = new(
            "¿Qué es una planta de inventario?",
            "Explicación",
            "Es el lugar donde se guarda el stock. Cada material vive dentro de una planta, y la planta 1 no se puede eliminar.",
            ["planta de inventario", "que es una planta", "que es una planta de inventario", "bodega"],
            [
                new VarianteAyuda(
                    "¿Cómo registro una planta de inventario?",
                    "El administrador la crea en Plantas de inventario, con el botón Administrar plantas.",
                    "Paso a paso",
                    ["crear planta", "registrar planta", "administrar plantas", "nueva planta"])
            ]),

        ["inventario"] = new(
            "¿Cómo consulto el stock de una planta?",
            "Paso a paso",
            "Muestra las cantidades de la planta y marca Bajo o Crítico cuando el stock no llega al mínimo.",
            ["consultar stock", "stock bajo", "stock critico", "inventario", "existencias"],
            []),

        ["importar-excel"] = new(
            "¿Cómo importo el inventario desde Excel?",
            "Paso a paso",
            "El administrador simula la hoja Inventario_Materiales y, si los datos cuadran, confirma la carga.",
            ["importar inventario", "excel", "plantilla de inventario"],
            []),

        ["ficha-tecnica-material"] = new(
            "¿Cómo completo la ficha técnica de un material?",
            "Paso a paso",
            "Guarda composición, gramaje y normas del insumo. No es la receta de la prenda: esa se arma en MRP.",
            ["ficha tecnica de material", "ficha del material", "gramaje", "composicion del material"],
            []),

        ["solicitudes"] = new(
            "¿Cómo solicito materiales?",
            "Paso a paso",
            "El taller arma el pedido en Solicitar insumos y la planta lo atiende en Solicitudes de materiales.",
            ["solicitar materiales", "como solicitar materiales", "pedir insumos", "solicitud de materiales"],
            []),

        ["reingreso"] = new(
            "¿Cómo hago un reingreso desde una etapa?",
            "Paso a paso",
            "Devuelve material o producto terminado desde Trazo, Corte, Confección, Calidad o Terminado hacia el inventario.",
            ["reingreso", "reingresar", "devolver material", "reingreso desde etapas"],
            []),

        ["movimientos"] = new(
            "¿Cómo veo los movimientos de stock?",
            "Paso a paso",
            "Cada entrada, salida o ajuste queda en una lista con fecha, usuario y cantidad. Desde aquí no se modifica el inventario.",
            ["movimientos de stock", "movimiento de stock", "historial de stock", "entradas y salidas"],
            []),

        ["aprendices"] = new(
            "¿Cómo veo los aprendices, las fichas y los grupos?",
            "Paso a paso",
            "SIPITEX no guarda el nombre de cada aprendiz. El grupo se reconoce por el número de ficha y por los grupos de confección.",
            ["aprendices", "alumnos", "estudiantes", "ver alumnos", "como veo los alumnos", "fichas de grupo", "grupos de confeccion", "numero de grupo"],
            []),

        ["actas"] = new(
            "¿Cómo hago un acta de ingreso o de egreso?",
            "Paso a paso",
            "Deja por escrito una entrada o una salida, con quien entrega, quien recibe y las dos firmas.",
            ["acta de ingreso", "acta de egreso", "actas", "nueva acta", "acta"],
            []),

        ["trazabilidad"] = new(
            "¿Cómo funciona la trazabilidad de una prenda?",
            "Explicación",
            "Cada prenda puede llevar un código único ligado a la orden, a los materiales y a la calidad.",
            ["trazabilidad", "rastrear prenda", "codigo unico"],
            [
                new VarianteAyuda(
                    "¿Cómo genero y consulto el código de una prenda?",
                    "En Trazabilidad se busca el código, se generan los que faltan (hasta 200 por lote) y se puede imprimir la etiqueta.",
                    "Paso a paso",
                    ["generar codigo", "consultar codigo", "imprimir etiqueta", "qr"])
            ]),

        ["costeo"] = new(
            "¿Cómo calculo el costo de una prenda?",
            "Paso a paso",
            "Usa los materiales consumidos y el costo que quedó guardado al consumir. La mano de obra no entra en ese total.",
            ["costeo", "costo de prenda", "como costear"],
            []),

        ["reportes"] = new(
            "¿Cómo veo los reportes y las estadísticas?",
            "Paso a paso",
            "Estadísticas resume el taller. Reportes descarga PDF o Excel. Alertas avisa el stock bajo y los pendientes.",
            ["reportes", "estadisticas", "alertas", "descargar reporte"],
            []),

        ["usuarios"] = new(
            "¿Cómo creo un usuario y sus permisos?",
            "Paso a paso",
            "El administrador da de alta a un instructor o a un encargado, elige el rol y, si hace falta, un permiso concreto.",
            ["crear usuario", "crear un usuario", "nuevo usuario", "agregar usuario", "usuarios y permisos", "permisos"],
            []),

        ["perfil"] = new(
            "¿Cómo cambio mi perfil y mi contraseña?",
            "Paso a paso",
            "Desde su nombre puede actualizar la foto, las funciones y la clave. El rol lo cambia un administrador.",
            ["mi perfil", "cambiar contrasena", "foto de perfil"],
            []),

        ["preguntas"] = new(
            "¿Qué hago si algo no me deja seguir?",
            "Explicación",
            "Respuestas cortas para cuando no puede entrar, no ve un menú o el stock sale vacío.",
            ["preguntas frecuentes", "no puedo entrar", "no veo el menu", "faq"],
            [])
    };
}

public sealed record FichaDidactica(
    string Pregunta,
    string Tipo,
    string Descripcion,
    string[] Intenciones,
    VarianteAyuda[] Variantes);

public sealed record VarianteAyuda(string Titulo, string Descripcion, string Tipo, string[] Frases);
