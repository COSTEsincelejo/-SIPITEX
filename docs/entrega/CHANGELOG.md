# CHANGELOG — SIPITEX hasta entrega documental v1.0

Formato: hitos agrupados. Fechas y PRs tomados del historial git / mensajes de commit mergeados.  
**No** hay git tags; **v1.0** es etiqueta documental sobre `main` `52e4170` (#162).

---

## [v1.0] — 2026-11-11 (documental)

Referencia: commit `52e4170` — docs: verificación final de entrega (#162).

### Cierre y verificación

- **#162** (2026-11-11) — Verificación final de entrega (seed, HANDOVER, README).
- **#161** (2026-11-11) — Cierre de hallazgos de auditoría: stock, seed, privacidad, calidad y entrega.
- **#159** (2026-11-09) — Consumo no se registra en orden finalizada o cancelada.
- **#160** (2026-11-09) — Reingreso de material devuelve sobrante de la orden.
- **#152** (2026-11-09) — Consumo ya no vuelve a descontar el stock entregado (**stock único**).

### Actas, actividad y concurrencia (previo inmediato)

- **#158** — Acta se verifica por QR y se envía por correo.
- **#157** — Acta guarda planta, firma pendiente y anulación.
- **#156** — Serilog: peticiones y eventos de negocio.
- **#155** — Actividad del instructor: una sola cuenta en pantalla, Excel y PDF.
- **#154** — Stock y numeración de órdenes dejan de pisarse.
- **#153** — Ciclo de la prenda sigue la etapa del taller.
- **#150** / **#151** — Resumen visual/PDF actividad instructor; búsqueda en centro de ayuda.

---

## Hito — Inventario unificado, importación CMTC y privacidad (2026-10)

Fechas vistas en historial:

| Fecha | PR | Resumen |
|-------|-----|---------|
| 2026-10-28 | **#134** | Unificar inventario por planta en una sola pantalla |
| 2026-10-14 | **#132** | Endurecer sesión, cabeceras, antiforgery y habeas data |
| 2026-11-05 | **#138** | Ficha técnica del material e importación desde Excel |
| — | **#136** / **#137** / **#139** / **#140** / **#141** | Importador Excel, plantilla CMTC, menú/ayuda alineados, política datos 1.1 |
| — | **#143** | Inventario: solo importación y redirecciones |
| — | **#129**–**#131** | Buscador header; planta obligatoria para encargado; centro de ayuda |
| — | **#145**–**#149** | Plantas en solicitud, selector producto OP, PDF acta, trazabilidad unidad BOM |

---

## Hito — Correo HTTPS: Resend → Brevo (2026-10-02)

| Fecha | PR | Resumen |
|-------|-----|---------|
| 2026-10-02 | **#127** | Enviar códigos de correo por HTTPS con Resend |
| 2026-10-02 | **#128** | Registrar respuesta de Brevo al enviar el código |

Producción documentada con `Email__Provider=Brevo` (README / HANDOVER). `Resend` permanece como opción configurable.

---

## Hito — Persistencia en PostgreSQL y plantas (2026-09-16 … 2026-09-30)

| Fecha | PR | Resumen |
|-------|-----|---------|
| 2026-09-16 | **#97** | Migrar SIPITEX de SQLite a PostgreSQL 16 |
| 2026-09-16 | **#98** | Consolidar migraciones EF Core en un único `InitialCreate` PostgreSQL |
| 2026-09-16 | **#100** | Consultar inventario por planta de inventario |
| 2026-09-16 | **#96** | Cerrar brechas de producción (firmas, costeo, trazabilidad, alertas, UX) |
| 2026-09-17 | **#101** | Trazabilidad de costo final con promedio ponderado |
| — | **#102** / **#104** | Inventario por bodega/filtros; costeo materiales congelados |
| 2026-09-30 | **#123** | Persistir fotos, correos y claves de cookie en PostgreSQL |
| — | **#119** / **#118** / **#117** / **#120** / **#121** | Arranque PG, Render, footer commit, DATABASE_URL, inventario operativo en planta |
| — | **#99** | Checklist Render / Blueprint sin secretos |

---

## Hito — Cierre de producción, seed y nomenclatura (2026-09-09 … 2026-09-10)

| Fecha | PR | Resumen |
|-------|-----|---------|
| 2026-09-09 | **#85** | Bodega→PlantaInventario; Bodeguero→Encargado de bodega; Nº ficha→Nº grupo |
| 2026-09-10 | **#91** | Integrar grupos, consumos, actas y costeo sobre main |
| 2026-09-10 | **#92** | Rutas y alcance: solicitudes, órdenes, reingreso, actas |
| 2026-09-10 | **#94** | Cierre de producción: seed, logging, costeo, lockout y CI |
| 2026-09-10 | **#95** | Sembrar administrador `cristianccbr@gmail.com` (**histórico**; producción actual documenta `admin@sipitex.local`) |
| — | **#83** | UX calidad (bitácora inspecciones) |
| — | **#82** | Scoping StockMovement por bodega |

---

## Hito — Bodegas / multi-planta y auditoría (PRs merge #71–#81)

Merges registrados (mensajes `Merge pull request #…`):

- **#71** add-bodegas-fase1  
- **#75**–**#80** scoping, admin bodegas, sidebar, filtros, CRUD, multi-bodega  
- **#81** auditoria-trazabilidad  

(Precedentes de inventario multi-ubicación antes del renombre #85 y de la UI unificada #134.)

---

## Hito — Alcance por rol, BOM/MRP y MES (PRs #45–#70)

Merges / commits con número visto en historial:

- **#45** órdenes-flujo-mes  
- **#48** stock-movement-historial · **#49** reingreso-bodeguero-etapas  
- **#50** órdenes editar/cancelar · **#51** admin cuentas crear/eliminar  
- **#52** inventario editar origen entrada  
- **#53**–**#55** BOM instructores, MRP gestionar fichas, órdenes crear instructor  
- **#56**–**#57** / **#61**–**#64** alcance instructor (calidad, reportes, inventario, órdenes, estadísticas)  
- **#59**–**#60** BOM ficha técnica fases A/B  
- **#62** activity-log-global  
- **#65**–**#67** / **#69**–**#70** MRP delete, materiales BOM, seed fichas CMTC, insumos libres  

---

## Hito — Solicitudes material, fichas y UX base (PRs #1–#44)

- **#1** completar-auth-usuarios · **#2** reportes-alertas-correo  
- **#4** ieee830-diagramas · **#5** roles-tests-ci  
- **#7**–**#13** formularios, auth UI, scope fichas, cuentas/perfil  
- **#18**–**#21** comentarios Domain  
- **#25** / **#28** / **#30** sidebar, rename aprendizaje, ficha instructor proceso  
- **#32**–**#35** modelo/UI/bodega/tests solicitud material  
- **#36** BOM fichas técnicas CRUD · **#37**–**#38** / **#40**–**#44** navegación, funcionalidades DOCX, búsqueda, reportes, materiales bodega, unidades libres  

---

## Hito — Fundación del repositorio

| Fecha | Commit / nota |
|-------|----------------|
| 2026-06-28 | `c35d9e7` — Primer commit - proyecto Sipitex |
| 2026-07-10 | Autenticación con roles; correcciones Users; retiro módulo Cumplimiento RF/RNF de esa etapa |
| 2026-07-23 | Completa autenticación y despliegue Docker |

IEEE 830 doc: v2.1 fechada **2026-07-28** en `docs/08-IEEE830-Especificacion.md`.

---

## Notas

- Listados priorizan PRs con número en el mensaje de commit en `main`; no se inventan fechas ausentes del historial consultado.
- PRs intermedios omitidos del resumen siguen en el log (`git log --grep='#'`).
- Cambios documentales de esta carpeta `docs/entrega/` acompañan la etiqueta **v1.0** sin crear tag git.
