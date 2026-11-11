# Plan de transición — SIPITEX v1.0

Orden operativo post-entrega. Fuentes: `docs/HANDOVER.md`, `README.md`, `docs/DEPLOY.md`, `docs/BACKUP.md`, `docs/GUIA-INVENTARIO-INICIAL.md`.  
Responsables no nominados en el repo se marcan **PENDIENTE**.

Versión: **v1.0** documental · Referencia `main` `52e4170` (#162).

---

## Fase 0 — Preparación

| # | Paso | Responsable | Estado |
|---|------|-------------|--------|
| 0.1 | Confirmar entorno de hosting (Render u otro) y PostgreSQL 16 remoto | PENDIENTE (ops centro) | PENDIENTE |
| 0.2 | Copiar variables desde `.env.example` al panel; **no** pegar secretos en git | PENDIENTE (ops) | PENDIENTE |
| 0.3 | Leer `docs/HANDOVER.md` y manuales de rol | PENDIENTE (Admin + Encargado + Instructores) | PENDIENTE |

---

## Fase 1 — Variables obligatorias de producción

Orden: definir antes del primer arranque productivo.

| # | Paso | Responsable | Notas |
|---|------|-------------|-------|
| 1.1 | `ASPNETCORE_ENVIRONMENT=Production` | PENDIENTE (ops) | |
| 1.2 | `DATABASE_URL` o `ConnectionStrings__DefaultConnection` (no localhost) | PENDIENTE (ops/DBA) | |
| 1.3 | `Seed__DemoUsers=false`; **no** definir `SEED_DEMO_DATA` | PENDIENTE (ops) | HANDOVER #10 |
| 1.4 | `ADMIN_SEED_PASSWORD` (≥6) solo si aún no hay Administrador | PENDIENTE (ops) | Crea `admin@sipitex.local` |
| 1.5 | `Email__Enabled=true`, `Email__Provider=Brevo`, `Email__ApiKey`, `Email__FromAddress`, `Email__FromName` | PENDIENTE (ops) | HTTPS; SMTP bloqueado en Render free |
| 1.6 | `App__BaseUrl` con URL pública | PENDIENTE (ops) | Correos / verificación actas |

---

## Fase 2 — Privacidad institucional

| # | Paso | Responsable | Estado |
|---|------|-------------|--------|
| 2.1 | Completar `Privacidad__*` confirmados (NIT, contacto, dirección, plazos, proveedores, URL política) | PENDIENTE (área jurídica / centro) | Vacío o `PENDIENTE` **no se publica** |
| 2.2 | Revisar checklist `PENDIENTES-PRIVACIDAD.md` | PENDIENTE | HANDOVER #9 |
| 2.3 | Verificar `/Privacidad` tras desplegar | PENDIENTE (Admin) | |

---

## Fase 3 — Primer arranque y seguridad

| # | Paso | Responsable | Estado |
|---|------|-------------|--------|
| 3.1 | Desplegar (`render.yaml` / Docker) y verificar health | PENDIENTE (ops) | |
| 3.2 | Iniciar sesión como `admin@sipitex.local` (si se creó) | PENDIENTE (Admin) | |
| 3.3 | **Rotar** `ADMIN_SEED_PASSWORD` / contraseña admin tras primer acceso | PENDIENTE (Admin/ops) | |
| 3.4 | Crear usuarios reales (Instructor, Encargado); asignar plantas al Encargado | PENDIENTE (Admin) | |
| 3.5 | Rotar `Email__ApiKey` y secretos DB si hubo exposición o cambio de responsable | PENDIENTE (ops) | |
| 3.6 | Confirmar que cuentas demo `@sipitex.test` **no** existen en producción | PENDIENTE (Admin) | |

> Nota histórica: seed PR #95 (`cristianccbr@gmail.com`) **no** es el admin de producción actual.

---

## Fase 4 — Inventario inicial

| # | Paso | Responsable | Estado |
|---|------|-------------|--------|
| 4.1 | Respaldo PostgreSQL (`docs/BACKUP.md`) **antes** de importar | PENDIENTE (ops/Admin) | Obligatorio |
| 4.2 | Seguir `GUIA-INVENTARIO-INICIAL.md` + plantilla `docs/plantillas/` | PENDIENTE (Admin) | |
| 4.3 | Simular en `/Inventario/Importar`; revisar omitidas/advertencias | PENDIENTE (Admin) | |
| 4.4 | Confirmar importación (sobrescribe stock si cantidad distinta) | PENDIENTE (Admin) | Solo carga inicial |
| 4.5 | Completar fichas técnicas de material pendientes a mano | PENDIENTE (Admin/Encargado) | |

---

## Fase 5 — Configuración de negocio

| # | Paso | Responsable | Estado |
|---|------|-------------|--------|
| 5.1 | Cargar / revisar BOM (MRP) de productos del centro | PENDIENTE (Admin + instructores con permiso) | |
| 5.2 | Definir si se activa mano de obra en costeo (hoy no suma) | PENDIENTE (centro) | HANDOVER #6 |
| 5.3 | Preferencias de alertas por usuario; prueba de correo | PENDIENTE (usuarios / Admin) | |
| 5.4 | Asignar permisos extendidos necesarios a instructores | PENDIENTE (Admin) | |

---

## Fase 6 — Operación diaria (primer ciclo)

| # | Paso | Responsable | Estado |
|---|------|-------------|--------|
| 6.1 | Crear OP → simular MRP en `/Mrp` → importar requisitos (no es automático) | PENDIENTE (Admin/Instructor) | HANDOVER #1 |
| 6.2 | Solicitudes → aprobación Encargado → entrega → consumo → reingreso | Roles según manuales | |
| 6.3 | Calidad / actas / trazabilidad según flujo del taller | Instructor + Admin | |
| 6.4 | No cancelar OP ni anular acta esperando reversión de stock | Todos | HANDOVER #3 — deuda |

---

## Fase 7 — Aceptación y cierre administrativo

| # | Paso | Responsable | Estado |
|---|------|-------------|--------|
| 7.1 | UAT con los tres roles (casos en `CASOS-DE-USO.md`) | PENDIENTE (centro) | |
| 7.2 | Contrastar matrices RF (`MATRIZ-ENTREGABLES-REQUISITOS.md`) | PENDIENTE | Incluye RF16 criterios por prenda **Ausente** |
| 7.3 | Firmar / archivar `CIERRE-PROYECTO.md` y `RESUMEN-EJECUTIVO.md` | PENDIENTE (dirección / ADSO) | |
| 7.4 | Decidir creación de git tag `v1.0` (hoy solo etiqueta documental) | PENDIENTE | |
| 7.5 | Planificar deuda HANDOVER (PT UI, reversión stock, nomenclatura, legacy) | PENDIENTE | Fuera de v1.0 |

---

## Orden resumido

```
0 Preparación → 1 Env producción → 2 Privacidad → 3 Arranque/rotación
→ 4 Inventario (con backup) → 5 BOM/alertas/permisos → 6 Ciclo piloto → 7 Aceptación
```

No reordenar 4 antes de 3.1–3.4. No ejecutar importador sobre planta en operación diaria sin respaldo.
