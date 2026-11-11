# Cierre de proyecto — SIPITEX v1.0

| Campo | Valor |
|-------|--------|
| Proyecto | SIPITEX — Sistema Integrado de Aprendizaje Producción e Inventario Textil |
| Institución | SENA CMTC · Programa ADSO |
| Versión de entrega | **v1.0** (etiqueta documental; **no** hay git tags en el repositorio) |
| Primer commit | 2026-06-28 (`c35d9e7` — Primer commit - proyecto Sipitex) |
| Último `main` de referencia | 2026-11-11 · `52e4170` (#162) |
| Rama documental | `cursor/docs-entrega-completa-31a7` |
| Fuentes | `docs/entrega/EXTRACCION-DATOS-REPO.md`, `docs/HANDOVER.md`, `README.md`, `docs/01-Requisitos.md`, `docs/08-IEEE830-Especificacion.md`, historial git |

## 1. Qué se cierra

Entrega documental y operativa del sistema web ASP.NET Core MVC + PostgreSQL 16, con roles Administrador, Instructor y Encargado de bodega, según `docs/HANDOVER.md` y el `README.md`.

Incluye módulos de órdenes/MES, MRP/BOM, plantas de inventario, solicitudes, consumos, calidad, actas, trazabilidad, reportes, alertas, auditoría, ayuda y política de privacidad (`/Privacidad` v1.1).

## 2. Evidencia de calidad (2026-11-11)

| Métrica | Resultado |
|---------|-----------|
| Tests | **768 passed**, 0 failed, ~19 s |
| Cobertura coverlet (line-rate) | **87,06 %** |
| Cobertura branch | **48,05 %** |
| Application | 81,3 % |
| Domain | 91,3 % |
| Infrastructure | 97,2 % |
| Web | 46,1 % |
| Views | 20,9 % |

## 3. Hitos técnicos cerrados (historial)

| Hito | Evidencia |
|------|-----------|
| Renombre Bodega → PlantaInventario | PR **#85** (2026-09-09) |
| Migración SQLite → PostgreSQL 16 | PR **#97** (2026-09-16); consolidación migraciones **#98** |
| Seed admin histórico | PR **#95** mencionó `cristianccbr@gmail.com` (histórico; producción actual: `admin@sipitex.local`) |
| Stock único (sin doble descuento consumo) | PR **#152** (2026-11-09) |
| Auditoría / cierre hallazgos | PR **#161** (2026-11-11) |
| Verificación final entrega | PR **#162** (2026-11-11) |

## 4. Estado de requisitos (síntesis)

- Numeración **IEEE 830** (`docs/08-IEEE830-Especificacion.md`) alineada al código actual: base de la matriz de entregables.
- Numeración **`docs/01-Requisitos.md` / seed `DbInitializer`**: distinta; ver `MATRIZ-ENTREGABLES-REQUISITOS.md`.
- **RF16 (criterios diferenciales por tipo de prenda)** según seed/`01-Requisitos`: **Ausente** (solo Aprobado/Reproceso genérico). En IEEE830, RF16 describe inspección con motivo/responsable en reproceso (implementado).
- Deuda explícita: `docs/HANDOVER.md` (MRP no automático al crear OP, PT sin pantalla, cancelación sin reversión de stock, nomenclatura, privacidad institucional, legado).

## 5. Documentos de este cierre

Índice: [`README.md`](README.md) en esta carpeta.

## 6. Pendientes de cierre operativo (no bloquean la etiqueta v1.0 documental)

| Ítem | Estado |
|------|--------|
| Completar `Privacidad__*` institucionales | PENDIENTE (área jurídica / centro) |
| Rotar secretos post-arranque (`ADMIN_SEED_PASSWORD`, API correo, DB) | PENDIENTE (operaciones) |
| Carga inventario inicial con respaldo previo | PENDIENTE (administrador + guía `GUIA-INVENTARIO-INICIAL.md`) |
| Crear git tag `v1.0` | PENDIENTE (no existe tag; solo etiqueta documental) |
| Responsable formal de aceptación institucional | PENDIENTE |

## 7. Declaración

Se declara la **versión de entrega documental v1.0** sobre el estado de `main` al commit `52e4170` (#162), con suites de prueba y cobertura citadas arriba, y con la deuda y exclusiones documentadas en `ALCANCE-FINAL.md` y `HANDOVER.md`.
