using Sipitex.Application.DTOs;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

// Catálogo fijo de módulos y funcionalidades implementadas en SIPITEX
public static class FuncionalidadesCatalog
{
    public static IReadOnlyList<FuncionalidadCatalogItem> Default { get; } =
    [
        // --- Inventario ---
        new("Inventario", "Consultar materiales",
            "Lista el stock de materiales textiles con unidad, mínimo y estado.",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero}, {UserRoles.Instructor}"),
        new("Inventario", "Registrar material",
            "Crea un material nuevo en bodega (nombre, unidad, stock y mínimo).",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero} (+ permiso extendido)"),
        new("Inventario", "Ajustar stock",
            "Incrementa o descuenta cantidad de un material existente.",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero}"),
        new("Inventario", "Actualizar estado de material",
            "Cambia el estado físico (Bueno, Regular, Deteriorado).",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero}"),
        new("Inventario", "Eliminar material",
            "Elimina un material del catálogo de inventario.",
            UserRoles.Administrador),
        new("Inventario", "Solicitud de material (legacy)",
            "Crea una solicitud de material ligada a una orden de producción.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}"),
        new("Inventario", "Aprobar / rechazar solicitud",
            "Resuelve solicitudes de material pendientes desde inventario.",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero} (+ permiso extendido)"),

        // --- Órdenes ---
        new("Órdenes de producción", "Consultar órdenes",
            "Muestra órdenes OP con meta, avance, estado y fecha límite.",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero}, {UserRoles.Instructor}"),
        new("Órdenes de producción", "Crear orden",
            "Registra una nueva orden de producción con producto y cantidades.",
            UserRoles.Administrador),
        new("Órdenes de producción", "Registrar avance",
            "Suma unidades producidas a una orden activa.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}"),

        // --- MRP ---
        new("MRP / Materiales", "Consultar BOM",
            "Lista productos BOM (fichas técnicas) y sus componentes.",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero}, {UserRoles.Instructor}"),
        new("MRP / Materiales", "Simular requerimientos",
            "Calcula necesidad de materiales según producto y cantidad a producir.",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero} (+ permiso extendido)"),
        new("MRP / Materiales", "Crear / editar producto BOM",
            "Mantiene el catálogo de productos y componentes de la ficha técnica.",
            $"{UserRoles.Administrador}, {UserRoles.Bodeguero}"),
        new("MRP / Materiales", "Eliminar producto BOM",
            "Quita un producto del catálogo BOM.",
            UserRoles.Administrador),

        // --- Fichas ---
        new("Fichas & producción", "Consultar fichas",
            "Lista fichas de formación con instructores, turno y orden asociada.",
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
            "Lista y crea solicitudes multi-ítem ligadas a ficha de formación.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}"),
        new("Solicitudes de material", "Ver detalle de solicitud",
            "Consulta ítems, estado y resolución de una solicitud.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}"),

        // --- Bodega ---
        new("Bodega — solicitudes", "Cola de solicitudes",
            "Lista todas las solicitudes de material pendientes o resueltas.",
            UserRoles.Bodeguero),
        new("Bodega — solicitudes", "Resolver solicitud",
            "Aprueba o rechaza una solicitud y genera código de entrega.",
            UserRoles.Bodeguero),

        // --- Calidad ---
        new("Control de calidad", "Consultar inspecciones",
            "Historial de inspecciones con resultado y motivo de reproceso.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}"),
        new("Control de calidad", "Registrar inspección",
            "Registra unidades inspeccionadas y resultado (Aprobado / Reproceso).",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}"),

        // --- Estadísticas ---
        new("Estadísticas", "Dashboard KPI",
            "Indicadores de producción, calidad, órdenes activas y stock bajo.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}, {UserRoles.Bodeguero}"),

        // --- Reportes ---
        new("Reportes", "Exportar inventario",
            "Descarga reporte de inventario en PDF o Excel.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}, {UserRoles.Bodeguero}"),
        new("Reportes", "Exportar órdenes",
            "Descarga reporte de órdenes de producción en PDF o Excel.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}, {UserRoles.Bodeguero}"),
        new("Reportes", "Exportar calidad",
            "Descarga reporte de inspecciones de calidad en PDF o Excel.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}, {UserRoles.Bodeguero}"),
        new("Reportes", "Exportar dashboard",
            "Descarga resumen KPI del dashboard en PDF o Excel.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}, {UserRoles.Bodeguero}"),

        // --- Alertas ---
        new("Alertas", "Preferencias de alerta",
            "Activa o desactiva notificaciones por correo según tipo de alerta.",
            $"{UserRoles.Administrador}, {UserRoles.Instructor}, {UserRoles.Bodeguero}"),
        new("Alertas", "Evaluar alertas",
            "Ejecuta la evaluación programada y envía correos pendientes.",
            UserRoles.Administrador),

        // --- Administración ---
        new("Administración", "Gestión de usuarios",
            "Lista, crea, edita y activa/desactiva cuentas del sistema.",
            UserRoles.Administrador),
        new("Administración", "Asignar roles y permisos",
            "Define rol (Instructor/Bodeguero), ficha y permisos extendidos.",
            UserRoles.Administrador),
        new("Administración", "Descargar reporte de funcionalidades",
            "Genera un documento Word con el catálogo de módulos del sistema.",
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
