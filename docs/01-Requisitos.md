# Fase 1 — Análisis de requisitos (Cascada)

**Proyecto:** SIPITEX · CMTC SENA ADSO  
**Versión documento:** 1.0

## 1.1 Objetivo del sistema

Gestionar la producción textil del centro: inventario de materias primas, órdenes de producción, MRP, fichas de aprendices, control de calidad y reportes KPI.

## 1.2 Actores

| Actor | Responsabilidad |
|-------|-----------------|
| Administrador | Órdenes, configuración, reportes |
| Instructor | Solicitud de materiales, registro de producción |
| Encargado de bodega | Entradas/salidas, aprobación de solicitudes |
| Control de calidad | Inspecciones y reprocesos |

## 1.3 Requisitos funcionales (RF01–RF20)

| ID | Módulo | Descripción | Prioridad |
|----|--------|-------------|-----------|
| RF01 | Usuarios | CRUD usuarios por rol | Alta |
| RF02 | Usuarios | Autenticación JWT | Alta |
| RF03 | Inventario | Registrar entradas con fecha | Media |
| RF04 | Inventario | Consultar stock en tiempo real | Alta |
| RF05 | Inventario | Estado del material | Media |
| RF06 | Salida | Solicitud de materiales | Alta |
| RF07 | Salida | Aprobación encargado de bodega | Alta |
| RF08 | Salida | Trazabilidad por orden | Media |
| RF09 | Órdenes | Crear órdenes de producción | Alta |
| RF10 | Órdenes | Estados de orden | Media |
| RF11 | Órdenes | Cálculo MRP automático | Alta |
| RF12 | Fichas | Ficha ↔ proceso | Alta |
| RF13 | Fichas | Múltiples fichas por instructor | Baja |
| RF14 | Producción | Sesión diaria con observaciones | Media |
| RF15 | Producción | Avance vs meta | Alta |
| RF16 | Calidad | Criterios por tipo de prenda | Media |
| RF17 | Calidad | Trazabilidad reproceso | Media |
| RF18 | Estadísticas | Reportes por período/instructor | Media |
| RF19 | Estadísticas | Consumo de materiales | Alta |
| RF20 | Estadísticas | Dashboard KPI | Alta |

## 1.4 Requisitos no funcionales (RNF01–RNF08)

| ID | Descripción |
|----|-------------|
| RNF01 | Carga < 2 s en intranet |
| RNF02 | JWT con expiración |
| RNF03 | Control de acceso por rol |
| RNF04 | Acceso desde PCs de la red |
| RNF05 | UI responsiva |
| RNF06 | Código modular |
| RNF07 | Docker Compose |
| RNF08 | Integridad transaccional |

## 1.5 Entregable de fase

Documento de requisitos aprobado → base para diseño (Fase 2).

La matriz de cumplimiento RF/RNF se documenta en este archivo y se siembra en la base de datos para referencia del proyecto.

## 1.6 Módulos posteriores (ya implementados)

Además del alcance original RF01–RF20, el código incluye:

| Módulo | Controller | Uso |
|--------|------------|-----|
| Plantas de inventario | `PlantasInventarioController` | Catálogo, alta/edición y reasignación |
| Solicitudes de planta | `PlantasInventarioSolicitudesController` | Cola de `SolicitudMaterial` por planta y rol |
| Órdenes de planta | `PlantasInventarioOrdenesController` | Entrega y reingreso de materiales de orden |
| Grupos de confección | `GruposConfeccionController` | Horas y prendas para costeo de mano de obra |
| Consumos | `ConsumosController` | Consumo real (no solo teórico BOM) |
| Costeo | `CostosController` | Costo de prenda (material histórico + tarifa) |
| Actas | `ActasController` | Ingreso/egreso con conformidad simple |
| Auditoría | `AuditoriaController` | Trazabilidad de acciones críticas |
| Solicitudes de ficha | `SolicitudesMaterialController` | Pedidos multi-ítem desde ficha o insumos libres |
| Búsqueda | `BusquedaController` | API JSON del buscador |
| Inicio | `HomeController` | Landing y error |

Detalle de mapeo: [`03-Implementacion.md`](03-Implementacion.md).

