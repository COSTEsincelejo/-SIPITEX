using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Sipitex.Application.Search;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Ayuda;

// Textos del centro de ayuda. Las vistas solo pintan esta lista: para corregir un paso
// se edita aquí, no el HTML. Los pasos describen botones y campos que existen hoy.
public static class AyudaCatalogo
{
    public static readonly string[] OrdenCategorias =
    [
        "Primeros pasos",
        "Órdenes de producción",
        "MRP / Materiales",
        "Plantas de inventario",
        "Inventario por planta",
        "Solicitudes de materiales",
        "Reingreso desde etapas",
        "Movimientos de stock",
        "Costeo de prendas",
        "Reportes y Estadísticas",
        "Usuarios y permisos",
        "Mi perfil",
        "Preguntas frecuentes"
    ];

    private static readonly string[] Todos = UserRoles.All;
    private static readonly string[] Admin = [UserRoles.Administrador];
    private static readonly string[] AdminInstructor = [UserRoles.Administrador, UserRoles.Instructor];
    private static readonly string[] AdminEncargado = [UserRoles.Administrador, UserRoles.EncargadoDeBodega];

    private static readonly JsonSerializerOptions JsonOpciones = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Default
    };

    private static readonly TemaAyuda[] Temas = ConstruirTemas();
    private static readonly AccionCatalogo[] Acciones = ConstruirAcciones();

    public static IReadOnlyList<TerminoGlosario> Glosario { get; } = ConstruirGlosario();

    public static IReadOnlyList<TemaAyuda> Visibles(ClaimsPrincipal user) =>
        Temas.Where(t => t.RolesPermitidos.Any(user.IsInRole)).ToArray();

    public static IReadOnlyList<AccionCatalogo> AccionesBusquedaSinFiltro() => Acciones;

    // Tema de la pantalla actual, para el botón "?" del encabezado.
    public static TemaAyuda? ParaRuta(string? controller, string? action, ClaimsPrincipal user)
    {
        if (string.IsNullOrWhiteSpace(controller) || string.IsNullOrWhiteSpace(action))
            return null;
        if (controller.Equals("Ayuda", StringComparison.OrdinalIgnoreCase))
            return null;

        var clave = controller + "/" + action;
        return Visibles(user).FirstOrDefault(t =>
            t.Rutas.Any(r => CoincideRuta(r, clave)));
    }

    public static string JsonTemas(ClaimsPrincipal user)
    {
        var lista = Visibles(user).Select(t => new
        {
            t.Id,
            t.Titulo,
            t.Categoria,
            t.Resumen,
            t.QueEs,
            t.ParaQueSirve,
            t.Pasos,
            t.Cambios,
            t.Advertencias,
            t.RolesPermitidos,
            t.PalabrasClave,
            t.UrlRelacionada,
            t.Icono
        });
        return JsonSerializer.Serialize(lista, JsonOpciones).Replace("<", "\\u003c", StringComparison.Ordinal);
    }

    private static bool CoincideRuta(string ruta, string clave)
    {
        if (ruta.EndsWith("/*", StringComparison.Ordinal))
        {
            var prefijo = ruta[..^1];
            return clave.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(ruta, clave, StringComparison.OrdinalIgnoreCase);
    }

    private static AccionCatalogo[] ConstruirAcciones()
    {
        var lista = new List<AccionCatalogo>
        {
            Accion(
                "Ayuda",
                "Centro de ayuda: guías, glosario y soporte",
                "/Ayuda",
                "fa-circle-question",
                Todos,
                ["ayuda", "centro de ayuda", "manual", "tutorial", "como se usa"])
        };

        foreach (var tema in Temas)
        {
            lista.Add(Accion(
                tema.Titulo,
                tema.Resumen,
                "/Ayuda#" + tema.Id,
                tema.Icono,
                tema.RolesPermitidos,
                tema.PalabrasClave.Append(tema.Titulo).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()));
        }

        return lista.ToArray();
    }

    private static AccionCatalogo Accion(
        string titulo,
        string descripcion,
        string url,
        string icono,
        string[] roles,
        string[] palabras) =>
        new()
        {
            Titulo = titulo,
            Descripcion = descripcion,
            Url = url,
            Icono = icono,
            Categoria = "Ayuda",
            RolesPermitidos = roles,
            PalabrasClave = palabras,
            Sinonimos = [],
            Intencion = null,
            Prioridad = 0
        };

    private static TemaAyuda[] ConstruirTemas() =>
    [
        Tema(
            "primeros-pasos", "Primeros pasos", "Primeros pasos", "fa-route",
            "Cómo entrar, qué ve cada rol y dónde pedir ayuda.",
            "SIPITEX es el sistema de aprendizaje, producción e inventario textil del CMTC SENA. Después de iniciar sesión cada persona ve solo las secciones de su rol.",
            "Sirve para ubicar el menú, el buscador y el perfil antes de registrar producción o inventario.",
            [
                "En la pantalla de acceso escriba su Correo y Contraseña y pulse Entrar.",
                "Si la cuenta es nueva, abra el correo, copie el código de 6 dígitos y use Confirmar correo. Sin ese paso no podrá entrar.",
                "Si olvidó la clave, use ¿Olvidaste tu contraseña?, pida el código y defina una nueva.",
                "A la izquierda está el menú: Operación (el día a día) y Análisis (cifras y reportes).",
                "Arriba, Buscar en SIPITEX abre la pantalla que usted describe. Ctrl+K también enfoca ese cuadro.",
                "La campana lleva a Alertas. Su nombre, a la derecha, abre Mi perfil."
            ],
            [
                "El menú no se personaliza a mano: cambia solo cuando un administrador modifica su rol o sus permisos.",
                "Para repetir esta explicación, abra Ayuda y pulse Repetir recorrido."
            ],
            [
                "El encargado de bodega no crea órdenes de producción.",
                "El instructor no administra plantas ni el listado de usuarios."
            ],
            Todos,
            ["primeros pasos", "entrar", "iniciar sesion", "menu", "sidebar", "roles"],
            "/Home",
            ["Home/Index"]),

        Tema(
            "ordenes", "Órdenes de producción", "Órdenes de producción", "fa-clipboard-list",
            "Crear una orden, seguir su avance y saber qué no se puede cambiar.",
            "Una orden de producción junta un producto que ya tiene ficha técnica, una cantidad, un cliente opcional y una fecha límite. Al crearla, el sistema calcula los materiales (MRP) con esa ficha.",
            "Sirve para lanzar el trabajo del taller y ver en qué estado va: pendiente de aprobación, en producción, finalizada o cancelada.",
            [
                "Abra Órdenes de producción en el menú.",
                "En la tarjeta Crear orden de producción elija el Producto (tiene que existir en la ficha técnica), escriba la Cantidad total y, si quiere, el Cliente.",
                "Indique la Fecha límite.",
                "Pulse Generar orden + MRP. El botón solo aparece para el administrador, o para un instructor que tenga el permiso Ordenes.Crear.",
                "La tabla muestra el número de orden, el avance y el estado de materiales. El enlace Ver detalle / flujo abre las etapas.",
                "Si el estado es Pendiente, el administrador puede pulsar Aprobar. Mientras no se apruebe, no se entregan materiales ni se registra producción.",
                "Cuando la planta ya entregó materiales, registre unidades producidas con el campo de unidades y el botón + uds."
            ],
            [
                "Solo el administrador ve el enlace Editar, y únicamente si la orden no está cancelada ni finalizada.",
                "No se puede editar una orden cancelada o finalizada.",
                "No se puede cancelar una orden finalizada.",
                "No se puede iniciar producción si aún hay materiales pendientes de entrega en la planta.",
                "No se puede saltar etapas: el flujo avanza de una en una (Trazo, Corte, Confección, Control de Calidad, Terminado)."
            ],
            [
                "El encargado de bodega consulta órdenes, pero no ve el formulario de crear.",
                "Un instructor sin el permiso Ordenes.Crear tampoco ve Generar orden + MRP."
            ],
            Todos,
            ["orden", "ordenes", "como crear orden", "crear orden", "generar orden", "aprobar orden", "editar orden"],
            "/Ordenes",
            ["Ordenes/*"]),

        Tema(
            "mrp", "MRP / Materiales", "MRP / Materiales", "fa-diagram-project",
            "Ficha técnica (BOM), materiales de un producto y simulación.",
            "La ficha técnica dice qué materiales y cuánto lleva un producto. MRP usa esa lista para calcular lo que hace falta fabricar una cantidad.",
            "Sirve para consultar materiales, simular una cantidad y, si su rol lo permite, crear o editar la ficha.",
            [
                "Abra MRP / Materiales.",
                "Para consultar, escriba código, referencia o nombre (por ejemplo CAM-001 o Camisa) y pulse Consultar materiales.",
                "En cada producto, Ver materiales abre el detalle. Editar abre la ficha si usted puede gestionarla.",
                "Nueva ficha técnica pide Producto, notas y las líneas de material. Ese botón depende del permiso de fichas técnicas: lo tiene el administrador, el encargado de bodega y el instructor con Mrp.GestionarFichas.",
                "Eliminar la ficha técnica deja el producto sin BOM. Solo el administrador ve ese botón, y el sistema no borra una ficha usada por órdenes activas.",
                "Más abajo, elija Producto y Cantidad a fabricar y pulse Calcular MRP y alerta stock. El encargado también puede simular; el instructor necesita el permiso Mrp.Simular.",
                "Fichas & producción (menú, solo administrador e instructor) es otra pantalla: Número de grupo, Proceso (Trazo, Corte, Confección, Calidad o Terminado) e Instructores registrados. Pulse el alta de esa tarjeta para registrar la ficha de grupo."
            ],
            [
                "Editar la ficha cambia la receta hacia adelante; no reescribe consumos ya congelados.",
                "No se puede repetir el mismo material en la receta.",
                "No se puede eliminar una ficha referenciada por órdenes activas: el mensaje indica cuáles son.",
                "Quitar un instructor de la ficha técnica usa la × de su etiqueta, en la misma pantalla MRP."
            ],
            [
                "Eliminar la ficha técnica es solo del administrador.",
                "El instructor sin permisos extendidos consulta, pero no crea fichas ni simula."
            ],
            Todos,
            ["mrp", "bom", "ficha tecnica", "materiales", "como crear ficha", "simular mrp", "fichas"],
            "/Mrp",
            ["Mrp/*", "Fichas/*", "GruposConfeccion/*", "Consumos/*", "Calidad/*"]),

        Tema(
            "plantas", "Administrar plantas", "Plantas de inventario", "fa-warehouse",
            "Cómo registrar una planta desde Inventario por planta, y por qué Planta de Inventario 1 no se elimina.",
            "Una planta de inventario es el lugar donde se guarda el stock (la bodega del taller). Cada material vive dentro de una planta.",
            "Sirve para crear plantas nuevas, renombrarlas y desactivarlas cuando ya no se usan, sin perder el historial. Se hace en Inventario por planta, con el botón Administrar plantas.",
            [
                "Abra Inventario por planta y pulse Administrar plantas (también dice Administrar catálogo). El encargado no ve ese botón: solo consulta las plantas que tiene asignadas.",
                "Escriba el Nombre (por ejemplo Planta de Inventario 3) y pulse Crear.",
                "En la tabla del panel, Operar inventario abre el detalle para entradas y ajustes.",
                "Editar cambia el nombre. Desactivar abre la confirmación. Si hay stock u otras dependencias, elija la planta destino y pulse Reasignar y desactivar."
            ],
            [
                "Desactivar exige que no quede stock: si hay unidades, primero reasígnelas a otra planta.",
                "Planta de Inventario 1 no se puede eliminar: el sistema la usa como planta por defecto al crear solicitudes de insumos libres.",
                "Tampoco se puede eliminar la última planta activa.",
                "Si la planta tiene solicitudes u otros registros, el sistema pide reasignarlos antes de desactivarla."
            ],
            [
                "Solo el administrador crea, edita y desactiva plantas.",
                "El instructor consulta Inventario por planta, pero no administra plantas."
            ],
            AdminEncargado,
            ["planta", "plantas", "como registrar una planta", "registrar planta", "crear planta", "bodega", "eliminar planta", "administrar plantas"],
            "/PlantasInventario#gestion-plantas",
            ["PlantasInventario/Create", "PlantasInventario/Edit", "PlantasInventario/Delete"]),

        Tema(
            "inventario", "Inventario por planta", "Inventario por planta", "fa-warehouse",
            "Consultar el stock de una planta y leer los niveles Bajo y Crítico.",
            "Inventario por planta lista los insumos y las cantidades de las plantas que usted puede ver, junto con el resumen por planta.",
            "Sirve para saber si hay material, filtrar por planta y por nivel, y ver el detalle sin cambiar de pantalla.",
            [
                "Abra Inventario por planta.",
                "En Seleccione una planta elija una planta o deje Todas las plantas, y pulse Ver. Los enlaces Todas y el nombre de cada planta hacen lo mismo.",
                "El resumen muestra Insumos, OK, Bajo, Crítico y Sin mínimo. Ver inventario deja esa planta seleccionada.",
                "En el detalle filtre por nombre o por nivel. Los niveles son: OK (hay stock y llega al mínimo), Bajo (hay stock pero no llega al mínimo), Crítico (el mínimo está definido y no hay existencias) y Sin mínimo definido (el material no tiene mínimo; no es una falta).",
                "Si su usuario no tiene plantas asignadas, la lista queda vacía: no es un error del sistema.",
                "El administrador abre Administrar plantas o Administrar catálogo para crear, renombrar o desactivar. Operar inventario registra entradas y ajustes, según su permiso."
            ],
            [
                "Crear o renombrar plantas se hace en Administrar plantas, en esta misma pantalla. Solo lo ve el administrador.",
                "No borre un material que esté en una ficha técnica: el sistema pide quitarlo de esas recetas antes.",
                "Un ajuste de stock queda en Movimientos de stock; no se deshace borrando la fila."
            ],
            [
                "El encargado solo ve las plantas asignadas a su usuario.",
                "El instructor consulta el inventario, pero no administra el catálogo de plantas."
            ],
            Todos,
            ["inventario", "stock", "bodega", "stock bajo", "stock critico", "consultar inventario", "inventario por planta"],
            "/PlantasInventario",
            ["PlantasInventario/Index", "PlantasInventario/Consultar", "PlantasInventario/Detalle", "Inventario/*", "Actas/*", "Trazabilidad/*"]),

        Tema(
            "solicitudes", "Solicitudes de materiales", "Solicitudes de materiales", "fa-truck-ramp-box",
            "Pedir insumos y resolver la cola de la planta.",
            "Hay dos caminos. El instructor (y el administrador) arma un pedido en Solicitar insumos. La planta lo atiende en Solicitudes de materiales. Materiales de órdenes es la entrega de lo que ya pidió una orden de producción.",
            "Sirve para que el taller pida tela, hilo u otros insumos y la bodega los entregue o los rechace con un motivo.",
            [
                "Para pedir: abra Solicitar insumos (también está en Mis solicitudes). El botón dice Solicitar insumos.",
                "La Descripción general es opcional. Grupo SENA y Orden de producción también son opcionales.",
                "Elija la PlantaInventario destino. En cada ítem escriba Descripción del ítem y Cantidad. Agregar ítem suma otra línea; Quitar la borra.",
                "Las Observaciones son opcionales. Pulse Enviar a planta de inventario.",
                "La cola de la planta está en Solicitudes de materiales. Abra el detalle y use Confirmar resolución cuando haya marcado cada ítem.",
                "En Materiales de órdenes revise la orden y entregue las cantidades pedidas. No se entregan materiales a una orden cerrada ni a una que siga pendiente de aprobación."
            ],
            [
                "Después de enviada, la solicitud se corrige resolviéndola en la planta, no reescribiendo el pedido original.",
                "No se puede eliminar un material de la orden si ya tiene entregas registradas.",
                "El encargado sin ninguna planta asignada ve la cola vacía y el aviso de que no tiene planta."
            ],
            [
                "Solicitar insumos es del administrador y del instructor.",
                "Resolver la cola y entregar materiales de órdenes es del administrador y del encargado de bodega. El instructor ve sus pedidos y el estado."
            ],
            Todos,
            ["solicitud", "solicitudes", "solicitar materiales", "como solicitar materiales", "insumos", "materiales de ordenes"],
            "/PlantasInventarioSolicitudes",
            ["PlantasInventarioSolicitudes/*", "SolicitudesMaterial/*", "PlantasInventarioOrdenes/Index"]),

        Tema(
            "reingreso", "Reingreso desde etapas", "Reingreso desde etapas", "fa-rotate-left",
            "Devolver material o producto terminado desde una etapa al inventario.",
            "El reingreso registra lo que vuelve desde Trazo, Corte, Confección, Control de Calidad o Terminado.",
            "Sirve para que ese material entre otra vez a la planta y quede un movimiento de entrada con la orden y la etapa.",
            [
                "Abra Reingreso desde etapas (también hay un acceso desde Materiales de órdenes).",
                "Elija la Orden de producción. Si la orden no tiene etapas, el aviso pide generarlas desde el detalle de la orden.",
                "Elija la Etapa de origen. El número entre paréntesis es lo disponible en esa etapa.",
                "En Tipo de reingreso marque Material de planta de inventario o Producto terminado (orden). Si es material, elija cuál.",
                "Escriba la Cantidad y, si hace falta, Observaciones.",
                "Pulse Registrar reingreso."
            ],
            [
                "No se puede reingresar más de lo disponible en la etapa.",
                "El reingreso no edita la orden: suma una entrada de stock o registra el producto terminado, según el tipo elegido.",
                "No hay un botón de borrar el reingreso. Un error se corrige con un ajuste documentado en Movimientos de stock."
            ],
            ["El instructor no tiene esta pantalla."],
            AdminEncargado,
            ["reingreso", "como reingresar", "devolver material", "reingreso desde etapas"],
            "/PlantasInventarioOrdenes/Reingreso",
            ["PlantasInventarioOrdenes/Reingreso"]),

        Tema(
            "movimientos", "Movimientos de stock", "Movimientos de stock", "fa-clock-rotate-left",
            "Ver entradas, salidas, ajustes y aprobaciones.",
            "Cada cambio de cantidad deja una fila: fecha (UTC), usuario, tipo, origen, material, cantidad y stock resultante.",
            "Sirve para revisar qué pasó con un insumo sin modificar el inventario desde esta lista.",
            [
                "Abra Movimientos de stock.",
                "Use Desde, Hasta y Material, y pulse Filtrar. Material en Todos muestra el conjunto.",
                "Si entró desde una planta, el filtro queda en esa planta y Volver a inventario regresa al detalle.",
                "Si no hay filas, la tabla lo dice: no es una falla."
            ],
            [
                "Esta pantalla no edita ni elimina movimientos.",
                "Para corregir una cantidad use el ajuste en el inventario de la planta; el historial conserva las dos líneas."
            ],
            ["El instructor no tiene esta pantalla. Consulta el stock en Inventario por planta."],
            AdminEncargado,
            ["movimientos", "movimiento de stock", "historial", "entradas", "salidas", "ajustes"],
            "/PlantasInventario/Movimientos",
            ["PlantasInventario/Movimientos"]),

        Tema(
            "costeo", "Costeo de prendas", "Costeo de prendas", "fa-coins",
            "Calcular el costo de materiales de una orden.",
            "El costo final usa solo materiales: la cantidad consumida por el costo unitario que quedó congelado al consumir, repartido en el volumen de la orden.",
            "Sirve para conocer el costo de la prenda. La mano de obra no entra en ese total.",
            [
                "Abra Costeo de prendas.",
                "Elija la Orden y pulse Calcular.",
                "El administrador puede guardar la tarifa COP / hora con Guardar tarifa. Esa tarifa es de referencia del grupo de confección y no se suma al costo de la prenda."
            ],
            [
                "Cambiar la tarifa no recalcula consumos ya registrados.",
                "No se edita el costo congelado de un consumo desde esta pantalla."
            ],
            ["El encargado de bodega no tiene Costeo de prendas en el menú."],
            AdminInstructor,
            ["costeo", "costo", "costo de prenda", "como costear", "tarifa"],
            "/Costos",
            ["Costos/*"]),

        Tema(
            "reportes", "Reportes y Estadísticas", "Reportes y Estadísticas", "fa-file-export",
            "Ver indicadores y descargar PDF o Excel.",
            "Estadísticas muestra prendas producidas, tasa de calidad, órdenes activas e inventario. Reportes descarga esos mismos frentes en PDF o Excel. Alertas avisa stock bajo o crítico y pendientes.",
            "Sirve para revisar el taller y llevarse un archivo, sin volver a escribir los datos.",
            [
                "Abra Estadísticas para ver las tarjetas y la gráfica. Ir a reportes e Ir a alertas están en el encabezado.",
                "En Reportes, los filtros Instructor, ficha, jornada y fecha son opcionales, salvo en Actividad del instructor: ahí el instructor es obligatorio.",
                "El instructor solo descarga sus fichas y órdenes. Su filtro de instructor no se puede cambiar.",
                "Elija el reporte y pulse la descarga PDF o Excel de esa tarjeta.",
                "En Alertas active las notificaciones de su rol. Configurar la evaluación solo aparece si tiene el permiso Alertas.Configurar (el administrador siempre lo tiene)."
            ],
            [
                "Un filtro que no aplica a ese reporte se ignora en la descarga (por ejemplo Instructor en Inventario).",
                "No se borran estadísticas: reflejan lo que ya está registrado en órdenes, calidad e inventario."
            ],
            ["Auditoría, en el menú, es solo del administrador."],
            Todos,
            ["reportes", "estadisticas", "alertas", "pdf", "excel", "como descargar reporte"],
            "/Reportes",
            ["Reportes/*", "Estadisticas/*", "Alertas/*", "Auditoria/*"]),

        Tema(
            "usuarios", "Usuarios y permisos", "Usuarios y permisos", "fa-users-gear",
            "Cómo crear un usuario, asignarle rol y qué no se puede borrar.",
            "El administrador crea cuentas de Administrador, Instructor y Encargado de bodega. El rol decide el menú. Los permisos extendidos suman una capacidad concreta.",
            "Sirve para dar de alta a un instructor o a un encargado y para desactivar a quien ya no debe entrar, sin perder su historial.",
            [
                "Abra Usuarios y pulse Crear usuario.",
                "Escriba Nombre, Correo y Contraseña temporal.",
                "En Rol elija Administrador, Instructor o Encargado de bodega.",
                "Si el rol es Instructor, puede asignar una Ficha. Si es Encargado de bodega, el bloque Plantas de inventario asignadas es obligatorio: marque al menos una (Ctrl o Cmd para varias). Sin planta no verá la cola de solicitudes.",
                "Los permisos extendidos son opcionales: Registrar materiales, Aprobar solicitudes, Simular MRP, Crear o editar ficha técnica, Crear órdenes de producción y Configurar alertas.",
                "Pulse Guardar. La persona recibe un código de 6 dígitos y no entra hasta confirmar el correo.",
                "En el listado, Editar abre la misma ficha. Desactivar deja la cuenta inactiva. Activar la devuelve."
            ],
            [
                "Prefiera Desactivar si la persona ya tiene registros. Eliminar es definitivo y el sistema lo bloquea cuando hay historial.",
                "No se puede eliminar al último administrador activo.",
                "No se puede cambiar el rol del administrador.",
                "Al editar, la nueva contraseña es opcional: déjela vacía para conservar la actual.",
                "El instructor y el encargado no tienen el menú Usuarios."
            ],
            [
                "Las plantas elegidas para un instructor o un administrador no se guardan: solo el encargado de bodega queda con plantas asignadas.",
                "Desactivar no borra órdenes, solicitudes ni movimientos."
            ],
            Admin,
            ["usuario", "usuarios", "crear usuario", "como crear usuario", "rol", "permisos", "desactivar usuario"],
            "/Account/Users",
            ["Account/Users", "Account/CreateUser", "Account/EditUser"]),

        Tema(
            "perfil", "Mi perfil", "Mi perfil", "fa-user",
            "Cambiar nombre, foto, funciones y contraseña.",
            "Mi perfil muestra sus datos y el rol. El rol no se edita aquí: lo cambia un administrador en Usuarios.",
            "Sirve para que el taller sepa quién es usted y para renovar la clave sin pedirla al administrador.",
            [
                "Pulse su nombre arriba a la derecha, o Mi perfil al final del menú.",
                "Puede subir una Foto de perfil o marcar quitarla.",
                "Corrija el Nombre. El Correo se muestra; si el sistema pide confirmar un cambio, use el código que llega al correo.",
                "Descripción de mis funciones es un texto libre, por ejemplo el puesto en la bodega o el proceso que acompaña.",
                "Para cambiar la clave escriba Nueva contraseña y Confirmar contraseña.",
                "Pulse Guardar perfil."
            ],
            [
                "No puede cambiar su propio rol ni sus plantas desde esta pantalla.",
                "Si deja las contraseñas vacías, la clave actual se conserva."
            ],
            ["Cerrar sesión está en Salir, no dentro del perfil."],
            Todos,
            ["perfil", "mi perfil", "contrasena", "foto", "cambiar clave"],
            "/Account/Profile",
            ["Account/Profile"]),

        Tema(
            "preguntas", "Preguntas frecuentes", "Preguntas frecuentes", "fa-comments",
            "Respuestas cortas a lo que más se traba al usar SIPITEX.",
            "Son las dudas de acceso, stock y permisos que el sistema ya responde con un mensaje en pantalla.",
            "Sirve para intentar la solución antes de escribir al soporte.",
            [
                "No puedo entrar: confirme el correo con el código de 6 dígitos o use ¿Olvidaste tu contraseña?.",
                "No veo un menú: su rol no lo incluye. Un administrador lo revisa en Usuarios.",
                "El stock sale vacío: el encargado no tiene planta asignada, o el filtro de planta no tiene materiales.",
                "No aparece Generar orden + MRP: hace falta ser administrador o instructor con el permiso Crear órdenes de producción.",
                "La planta no se deja eliminar: es Planta de Inventario 1, es la última activa, o todavía tiene stock u otros registros.",
                "Quiero borrar un usuario y el sistema no deja: desactívelo. El último administrador tampoco se puede eliminar."
            ],
            [
                "Estas respuestas no cambian datos. La corrección se hace en la pantalla que indica cada pregunta."
            ],
            ["Si el mensaje del sistema dice otra cosa, haga caso de ese texto: es la validación que está vigente."],
            Todos,
            ["preguntas frecuentes", "faq", "no puedo entrar", "no veo el menu", "ayuda frecuente"],
            "/Ayuda#preguntas",
            [])
    ];

    private static IReadOnlyList<TerminoGlosario> ConstruirGlosario() =>
    [
        new("Planta de inventario", "Lugar donde se guarda el stock. Planta de Inventario 1 es la planta por defecto y no se puede eliminar."),
        new("Bodega", "En SIPITEX es la misma planta de inventario. Inventario por planta consulta el stock de esas plantas."),
        new("Orden de producción", "Pedido para fabricar una cantidad de un producto que ya tiene ficha técnica, con fecha límite."),
        new("MRP", "Cálculo de materiales necesarios a partir de la ficha técnica y de la cantidad a fabricar."),
        new("BOM o ficha técnica", "Lista de materiales y cantidades que lleva un producto. En el menú también existe Fichas & producción, que es la ficha de grupo y proceso del instructor."),
        new("Stock OK", "Hay existencias y alcanzan el mínimo definido para ese material."),
        new("Stock Bajo", "Hay existencias, pero la cantidad es menor que el mínimo."),
        new("Stock Crítico", "El material tiene mínimo definido y la existencia está en cero."),
        new("Sin mínimo definido", "El material no tiene mínimo. No cuenta como falta ni dispara la misma alerta que Bajo o Crítico."),
        new("Reingreso", "Devolución de material o de producto terminado desde una etapa (Trazo, Corte, Confección, Control de Calidad o Terminado) hacia el inventario."),
        new("Acta de ingreso o egreso", "Documento de entrada o salida con nombre, cargo, hora UTC y firma dibujada. Se crea con Nueva acta."),
        new("Solicitud de materiales", "Pedido de insumos por descripción, o la entrega de materiales ya ligados a una orden.")
    ];

    private static TemaAyuda Tema(
        string id,
        string titulo,
        string categoria,
        string icono,
        string resumen,
        string queEs,
        string paraQueSirve,
        string[] pasos,
        string[] cambios,
        string[] advertencias,
        string[] roles,
        string[] palabras,
        string url,
        string[] rutas) =>
        new()
        {
            Id = id,
            Titulo = titulo,
            Categoria = categoria,
            Icono = icono,
            Resumen = resumen,
            QueEs = queEs,
            ParaQueSirve = paraQueSirve,
            Pasos = pasos,
            Cambios = cambios,
            Advertencias = advertencias,
            RolesPermitidos = roles,
            PalabrasClave = palabras,
            UrlRelacionada = url,
            Rutas = rutas
        };
}

public sealed class TemaAyuda
{
    public required string Id { get; init; }
    public required string Titulo { get; init; }
    public required string Categoria { get; init; }
    public required string Icono { get; init; }
    public required string Resumen { get; init; }
    public required string QueEs { get; init; }
    public required string ParaQueSirve { get; init; }
    public string[] Pasos { get; init; } = [];
    public string[] Cambios { get; init; } = [];
    public string[] Advertencias { get; init; } = [];
    public string[] RolesPermitidos { get; init; } = [];
    public string[] PalabrasClave { get; init; } = [];
    public string UrlRelacionada { get; init; } = "";
    public string[] Rutas { get; init; } = [];
}

public sealed record TerminoGlosario(string Termino, string Definicion);
