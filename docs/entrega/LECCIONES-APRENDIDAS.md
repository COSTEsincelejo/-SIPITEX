# Lecciones aprendidas — SIPITEX

> **BORRADOR** — Texto de reflexión interna a partir del historial y de `HANDOVER` / extracción.  
> No sustituye actas formales del centro. Completar responsables y validación institucional: **PENDIENTE**.

Versión entrega documental: **v1.0** · Referencia `main`: `52e4170` (#162).

---

## 1. SQLite → PostgreSQL (PR #97)

**Hecho:** Migración a PostgreSQL 16 (#97, 2026-09-16) y consolidación en un `InitialCreate` limpio (#98). Las migraciones SQLite anteriores quedaron reemplazadas; no hay conversión automática de `sipitex.db` (`docs/05-Despliegue.md`).

**Lección (borrador):** Cambiar de motor a mitad de proyecto obliga a tool de migración de datos (`tools/Sipitex.DataMigration`), checklist de Render/`DATABASE_URL`, y a tratar el histórico SQLite como legado descartable. Decidir Postgres temprano reduce doble mantenimiento de proveedores EF.

**Seguimiento:** Documentar en operación el flujo `--dry-run` de DataMigration — PENDIENTE si el centro aún tiene dumps SQLite.

---

## 2. Bodega → PlantaInventario (PR #85)

**Hecho:** Renombre de nomenclatura (#85, 2026-09-09): Bodega→PlantaInventario, Bodeguero→Encargado de bodega, Número de ficha→Número de grupo. Luego UI unificada (#134) e inventario operativo por planta (#121).

**Lección (borrador):** Renombrar dominio sin unificar el lenguaje de usuario deja deuda explícita (HANDOVER #4): el rol sigue diciendo «bodega» mientras la UI dice «planta». Planear glosario y menús **antes** del rename masivo.

**Seguimiento:** Campaña de nomenclatura única en UI/manuales — PENDIENTE.

---

## 3. Resend → Brevo (PRs #127 / #128)

**Hecho:** Códigos por HTTPS con Resend (#127, 2026-10-02); registro de respuesta Brevo (#128, mismo día). Producción documentada con Brevo por bloqueo SMTP del plan gratis de Render (README / HANDOVER).

**Lección (borrador):** El hosting condiciona el proveedor de correo. Mantener abstracción `Email__Provider` (Brevo / Resend / Smtp / Outbox) evitó reescrituras; el fallo visible cuando falta API key es preferible a “éxito simulado”.

**Seguimiento:** Completar `Privacidad__ProveedorCorreo` — PENDIENTE institucional.

---

## 4. Stock dual → stock único (PR #152)

**Hecho:** El consumo llegaba a descontar de nuevo el stock ya entregado a la orden. #152 (2026-11-09) corrige: el consumo no vuelve a descontar lo entregado. Refuerzos: #159 (no consumir en OP cerrada), #160 (reingreso de sobrante), #161 (cierre auditoría stock).

**Lección (borrador):** Dos caminos de descuento (entrega vs consumo) generan inconsistencia contable. Un ledger (`StockMovement`) y reglas “consumo ≤ entregado − consumido − reingresado” deben fijarse en dominio **antes** de ampliar pantallas. Tests de concurrencia (#154 y suite citada en extracción) son parte del cierre, no un extra.

**Seguimiento:** Cancelar OP / anular acta aún **no** revierte stock (HANDOVER #3) — deuda abierta.

---

## 5. PRs reemplazados / supersedidos

**Hecho observado en historial y deuda:**

| Evolución | Qué quedó atrás |
|-----------|-----------------|
| Solicitudes 1-ítem `MaterialRequest` | Flujo producto: `SolicitudMaterial` multi-ítem / insumos libres (HANDOVER #7) |
| Pantalla/API inventario monolítica | Redirect a `PlantasInventario`; importador aislado (#143) |
| Búsqueda `/api/busqueda` | `Buscar/Sugerencias` (legado coexistente — HANDOVER #7) |
| Seed admin #95 (`cristianccbr@gmail.com`) | Arranque producción: `admin@sipitex.local` + `ADMIN_SEED_PASSWORD` |
| Aserciones seed RF10 “solo En Proceso/Finalizada” | Código con Pendiente/Cancelada (extracción: seed desactualizado) |
| Ramas/PRs de entrega paralelos | Iteraciones de verificación/auditoría convergieron en #161 / #162 sobre `main` |

**Lección (borrador):** Dejar código y seeds “a medias” tras un reemplazo confunde matrices de cumplimiento. Cada PR que supersede otro debería: (1) retirar o marcar legacy, (2) actualizar seed/docs de estado, (3) una sola fuente de verdad de requisitos (hoy hay dos numeraciones RF).

**Seguimiento:** Retiro controlado de legacy listado en HANDOVER #7 — PENDIENTE. Alinear seed RF/RNF con IEEE 830 o documentar equivalencia oficial — PENDIENTE (extracción §9).

---

## 6. Otras observaciones (borrador)

- **Permisos extendidos** vs roles fijos: modelo flexible, pero exige pruebas de policy por gap (#53–#55, etc.).
- **Importador que sobrescribe stock:** potente para carga inicial, peligroso en operación diaria — la advertencia en HANDOVER debe ser parte del checklist de transición.
- **Cobertura Views/Web baja** frente a Domain/Infrastructure: el riesgo residual está en UI y controladores; la suite de 768 tests mitiga servicios, no sustituye UAT del centro — PENDIENTE UAT formal.

---

*Fin del BORRADOR. Validación por el equipo CMTC / ADSO: PENDIENTE.*
