# Alcance final — SIPITEX v1.0

Fuentes: `README.md`, `docs/08-IEEE830-Especificacion.md` §1.2, `docs/01-Requisitos.md` §1.6, `docs/HANDOVER.md`, `docs/entrega/EXTRACCION-DATOS-REPO.md`.

Versión: **v1.0** (etiqueta documental; sin git tags). Referencia `main`: `52e4170` (#162), 2026-11-11.

---

## 1. Incluido en la entrega

### 1.1 Núcleo (IEEE 830 / implementación)

| Área | Qué incluye |
|------|-------------|
| Usuarios y sesión | CRUD por rol, cookies con expiración, permisos extendidos |
| Inventario / plantas | Materiales, stock, niveles, estado físico, ajuste, ficha técnica de material, importación Excel (Admin) |
| Solicitudes / salidas | Solicitud multi-ítem o insumos libres; aprobación/rechazo con descuento de stock |
| Órdenes / MES | Crear OP, etapas, avance, materiales de orden, entrega/reingreso |
| MRP / BOM | Mantener BOM, simular requerimiento neto |
| Fichas (formación) | Asociación proceso/instructor/orden; sesión diaria de producción |
| Calidad | Inspección Aprobado/Reproceso; motivo y responsable en reproceso |
| Reportes / estadísticas | PDF/Excel, filtros, dashboard KPI |
| Alertas | Preferencias y evaluación de eventos |
| Actas | Ingreso/egreso con firma gráfica, PDF, verificación QR |
| Trazabilidad | Código de prenda y línea de tiempo |
| Auditoría | Activity log (Administrador) |
| Ayuda / búsqueda | Centro de ayuda + buscador de encabezado |
| Privacidad | `/Privacidad` v1.1 (campos vacíos/`PENDIENTE` no se publican) |
| Despliegue | Docker Compose, blueprint Render, PostgreSQL 16 |

### 1.2 Módulos posteriores (ya en código; `01-Requisitos` §1.6)

Plantas de inventario, solicitudes/órdenes de planta, grupos de confección, consumos, costeo (solo materiales en el total), actas, trazabilidad, auditoría, solicitudes de material, búsqueda, inicio.

### 1.3 Roles

Administrador · Instructor · Encargado de bodega (`UserRoles`). Permisos extendidos según `ExtendedPermissions` / `PermissionRules` (ver extracción §3).

---

## 2. Excluido explícitamente

Según IEEE 830 §1.2:

- Facturación
- Nómina
- ERP externo
- Aplicación móvil nativa

Además (código / HANDOVER):

- Criterios de calidad **diferenciales por tipo de prenda** (RF16 seed / `01-Requisitos`) — **Ausente**
- Autenticación JWT (`01-Requisitos` RF02/RNF02) — supersedida por cookies
- Pantalla dedicada de consulta de **producto terminado** en el menú
- Cálculo MRP **automático** al crear la orden
- Reversión de stock/consumos al cancelar orden o anular acta
- Inclusión de **mano de obra** en el total de costeo (tarifa de referencia no se suma)
- Seed demo (`SEED_DEMO_DATA`) en entorno real

---

## 3. Deuda conocida (HANDOVER) — dentro del perímetro pero incompleta

### Operativa / producto

| # | Deuda |
|---|--------|
| 1 | MRP no se calcula automáticamente al crear una OP: simular en `/Mrp` e importar requisitos |
| 2 | Producto terminado sin pantalla propia; stock PT en flujo MES / reingreso |
| 3 | Cancelar orden o anular acta no revierte stock ni consumos ya aplicados |
| 4 | Nomenclatura no unificada (Ficha / BOM / material; Bodega rol vs Planta UI; material vs insumo) |
| 5 | `User.FichaAsignadaId` solo informativo; alcance real vía `FichaInstructor` / etapas MES |
| 6 | Costeo excluye mano de obra del total hasta definición/activación institucional |

### Técnica / legado

| # | Deuda |
|---|--------|
| 7 | Legacy: `MaterialRequest`, RF/RNF + `RequirementService` sin pantalla/DI completo, `/api/busqueda` vs `Buscar/Sugerencias` |
| 8 | `BomItem → Material` Cascade; blockers de borrado de usuario incompletos |
| 9 | Privacidad institucional incompleta (`PENDIENTES-PRIVACIDAD.md`) |
| 10 | No reactivar seed demo en producción; inventario inicial con guía dedicada |

Operativo adicional: importador `/Inventario/Importar` **sobrescribe stock**; variables `Privacidad__*` pendientes.

---

## 4. Discrepancias documentales a tener en cuenta

| Tema | Detalle |
|------|---------|
| Dos numeraciones RF | `01-Requisitos`/seed vs IEEE 830 — mismos IDs, textos distintos |
| RNF02 “8 horas” | Docs/seed; código default **20 minutos** (`SeguridadOptions`) |
| RNF01 &lt; 2 s | Sin benchmark en repo — PENDIENTE evidencia |
| Admin seed #95 | `cristianccbr@gmail.com` histórico; producción: `admin@sipitex.local` |
