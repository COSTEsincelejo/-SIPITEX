using Sipitex.Application.DTOs;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

// Catálogo fijo de módulos y funcionalidades implementadas en SIPITEX
// (fuente del Word descargable). Debe reflejar policies y gates reales.
public static class FuncionalidadesCatalog
{
    public static IReadOnlyList<FuncionalidadCatalogItem> Default { get; } =
    [
        // --- Inventario ---
        new("Inventario", "Consultar materiales",
            "Lista el stock con unidad, mínimo, estado y nivel (OK / Bajo / Crítico). Crítico = sin existencias (Stock ≤ 0); Bajo = Stock > 0 y bajo el mínimo.",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero}"),
        new("Inventario", "Consultar historial de movimientos",
            "Lista entradas, salidas, ajustes y aprobaciones con fecha, usuario, cantidad y nivel del stock resultante.",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero}"),
        new("Inventario", "Registrar material",
            "Crea un material nuevo en bodega (nombre, unidad, stock, origen de entrada).",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero}"),
        new("Inventario", "Editar material",
            "Modifica nombre, unidad y stock mínimo de un material existente.",
            UserRoles.Administrador),
        new("Inventario", "Ajustar stock",
            "Incrementa o descuenta cantidad de un material existente; al aumentar exige origen de entrada.",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero}"),
        new("Inventario", "Actualizar estado de material",
            "Cambia el estado físico (Bueno, Regular, Deteriorado).",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero}"),
        new("Inventario", "Eliminar material",
            "Elimina un material del catálogo de inventario.",
            UserRoles.Administrador),
        new("Inventario", "Solicitud de material (legacy)",
            "Crea una solicitud de un material ligada a una orden de producción (flujo distinto a Mis solicitudes por ficha).",
            UserRoles.Administrador),
        new("Inventario", "Aprobar / rechazar solicitud legacy",
            "Resuelve solicitudes legacy pendientes desde Inventario y descuenta stock al aprobar.",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero} (+ permiso extendido Solicitudes.Aprobar)"),

        // --- Órdenes ---
        new("Órdenes de producción", "Consultar órdenes",
            "Lista órdenes con meta, avance, estado y materiales. Instructor: solo las que puede operar (asignación MES) y/o preparar materiales (BOM ∪ etapa).",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero}, {UserRoles.Instructor}"),
        new("Órdenes de producción", "Crear orden",
            "Registra una orden con producto y cantidades. Nace en Pendiente hasta que el Administrador la apruebe.",
            $"{UserRoles.Administrador} (+ permiso extendido Ordenes.Crear para Instructor)"),
        new("Órdenes de producción", "Aprobar orden",
            "Pasa Pendiente → EnProceso; habilita producción, MES y entrega física de materiales.",
            UserRoles.Administrador),
        new("Órdenes de producción", "Editar orden",
            "Modifica producto, cantidad, fecha límite y cliente; cada cambio genera OrderChangeLog.",
            UserRoles.Administrador),
        new("Órdenes de producción", "Cancelar orden",
            "Pasa la orden a Cancelada sin revertir stock ya entregado.",
            UserRoles.Administrador),
        new("Órdenes de producción", "Preparar materiales de la orden",
            "Añade, quita o importa materiales BOM mientras la orden no esté Finalizada/Cancelada. En Pendiente no hay entrega física. Instructor: gate BomProductInstructor ∪ etapa MES.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}"),
        new("Órdenes de producción", "Registrar avance de producción",
            "Suma unidades producidas a una orden EnProceso lista para producción. Instructor: solo si puede operar la orden.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}"),
        new("Órdenes de producción", "Operar flujo MES (etapas)",
            "Inicia, pausa, reanuda, completa etapas; procesa unidades y movimientos parciales. Admin configura etapas y asigna instructores. Instructor: solo órdenes/etapas que puede operar, en EnProceso.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}"),

        // --- MRP ---
        new("MRP / Materiales", "Consultar BOM",
            "Lista productos BOM (fichas técnicas) y componentes. Instructor: solo fichas técnicas asignadas.",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero}, {UserRoles.Instructor}"),
        new("MRP / Materiales", "Simular requerimientos",
            "Calcula necesidad de materiales según producto y cantidad a producir.",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero} (+ permiso extendido Mrp.Simular)"),
        new("MRP / Materiales", "Crear / editar producto BOM",
            "Mantiene el catálogo de productos y componentes de la ficha técnica.",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero} (+ permiso extendido Mrp.GestionarFichas)"),
        new("MRP / Materiales", "Eliminar producto BOM",
            "Quita un producto del catálogo BOM si no hay órdenes activas que lo referencien. Misma policy que crear/editar.",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero} (+ permiso extendido Mrp.GestionarFichas)"),
        new("MRP / Materiales", "Asignar instructor a ficha técnica",
            "Asigna o quita instructores autorizados a una ficha técnica (BOM).",
            UserRoles.Administrador),

        // --- Fichas ---
        new("Fichas & producción", "Consultar fichas",
            "Lista fichas de formación SENA con instructores, turno y orden asociada. Instructor: solo las suyas.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}"),
        new("Fichas & producción", "Crear ficha",
            "Registra una ficha de producción con código, proceso e instructores.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}"),
        new("Fichas & producción", "Asignar / quitar instructor",
            "Gestiona instructores de una ficha y su proceso asignado.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}"),
        new("Fichas & producción", "Registrar producción",
            "Registra unidades producidas por ficha (completo o registro rápido).",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}"),

        // --- Solicitudes material (ficha) ---
        new("Solicitudes de material", "Mis solicitudes",
            "Lista y crea solicitudes multi-ítem ligadas a ficha de formación. Instructor: solo las que él solicitó; Admin: vista global.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}"),
        new("Solicitudes de material", "Ver detalle de solicitud",
            "Consulta ítems, estado y resolución de una solicitud (mismo alcance que el listado).",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}"),

        // --- Bodega ---
        new("Bodega — solicitudes", "Cola de solicitudes",
            "Lista solicitudes de material por ficha (pendientes o todas) para resolución de bodega.",
            UserRoles.Bodeguero),
        new("Bodega — solicitudes", "Resolver solicitud",
            "Aprueba o rechaza ítems (incluye parcial) y genera código de entrega; descuenta stock.",
            UserRoles.Bodeguero),
        new("Bodega — órdenes", "Materiales de órdenes",
            "Consulta requisitos de materiales por orden, valida stock y entrega (parcial o total) cuando la orden está EnProceso.",
            UserRoles.Bodeguero),
        new("Bodega — órdenes", "Reingreso desde etapas",
            "Registra materiales o producto que regresan desde etapas MES (Trazo, Corte, Confección, Control de Calidad, Terminado).",
            UserRoles.Bodeguero),

        // --- Calidad ---
        new("Control de calidad", "Consultar inspecciones",
            "Historial de inspecciones con resultado y motivo de reproceso. Instructor: solo órdenes que puede operar.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}"),
        new("Control de calidad", "Registrar inspección",
            "Registra unidades inspeccionadas y resultado (Aprobada / Reproceso / Rechazada) sobre órdenes propias (Instructor) o todas (Admin).",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}"),

        // --- Estadísticas ---
        new("Estadísticas", "Dashboard KPI",
            "Indicadores de producción, calidad, órdenes activas/pendientes y materiales que requieren atención (Crítico + Bajo). Instructor: KPIs acotados a sus órdenes.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}, {UserRoles.Bodeguero}"),

        // --- Reportes ---
        new("Reportes", "Exportar inventario",
            "Descarga reporte de inventario en PDF o Excel (incluye nivel de stock). No disponible para Instructor.",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero}"),
        new("Reportes", "Exportar órdenes",
            "Descarga reporte de órdenes en PDF o Excel. Instructor: alcance forzado a sí mismo.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}, {UserRoles.Bodeguero}"),
        new("Reportes", "Exportar calidad",
            "Descarga reporte de inspecciones en PDF o Excel. Instructor: alcance forzado a sí mismo.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}, {UserRoles.Bodeguero}"),
        new("Reportes", "Exportar dashboard",
            "Descarga resumen KPI en PDF o Excel (con desglose Crítico/Bajo). Instructor: alcance forzado a sí mismo.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}, {UserRoles.Bodeguero}"),
        new("Reportes", "Exportar actividad del instructor",
            "Descarga producción por ficha/jornada y materiales consumidos. Instructor: solo su propia actividad; Admin/Bodeguero eligen instructor.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}, {UserRoles.Bodeguero}"),

        // --- Alertas ---
        new("Alertas", "Preferencias de alerta",
            "Activa o desactiva notificaciones por correo según tipo (stock bajo/crítico desglosado en un solo tipo StockBajo, órdenes, solicitudes, etc.).",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}, {UserRoles.Bodeguero}"),
        new("Alertas", "Evaluar alertas",
            "Ejecuta la evaluación y envía correos pendientes.",
            $"{UserRoles.Administrador} (+ permiso extendido Alertas.Configurar para cualquier rol)"),

        // --- Administración ---
        new("Administración", "Gestión de usuarios",
            "Lista, crea, edita, activa/desactiva y elimina (hard delete si no hay dependencias) cuentas de Administrador, Instructor y Bodeguero.",
            UserRoles.Administrador),
        new("Administración", "Asignar roles y permisos",
            "Define rol y permisos extendidos (Inventario.Registrar, Solicitudes.Aprobar, Mrp.Simular, Mrp.GestionarFichas, Ordenes.Crear, Alertas.Configurar). Nota: Inventario.Registrar no abre la pantalla de Inventario al Instructor (consulta restringida a Admin/Bodeguero).",
            UserRoles.Administrador),
        new("Administración", "Descargar reporte de funcionalidades",
            "Genera un documento Word con este catálogo de módulos del sistema.",
            UserRoles.Administrador),

        // --- Cuenta ---
        new("Cuenta", "Iniciar / cerrar sesión",
            "Autenticación por correo y contraseña con cookie de sesión.",
            "Todos los roles"),
        new("Cuenta", "Recuperar contraseña",
            "Solicita y aplica restablecimiento de contraseña por correo.",
            "Todos los roles"),
        new("Cuenta", "Mi perfil",
            "Actualiza nombre, foto, descripción de función y contraseña.",
            "Todos los roles")
    ];
}
