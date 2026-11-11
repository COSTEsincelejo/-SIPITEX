# Mantenimiento y soporte — SIPITEX

## 1. Contactos y SLA

| Ítem | Estado |
|------|--------|
| Mesa de ayuda / correo de soporte institucional | **PENDIENTE: definir con la institución** |
| Responsable técnico del centro | **PENDIENTE: definir con la institución** |
| Horario de atención / severidades / SLA (respuesta y resolución) | **PENDIENTE: definir con la institución** |
| Escalamiento jurídico / RNBD | Ver [`PENDIENTES-PRIVACIDAD.md`](../../PENDIENTES-PRIVACIDAD.md) |

Configuración técnica placeholder en appsettings: `Soporte:Responsable` = «Coordinación CMTC SENA»; `Soporte:Correo` vacío hasta que el centro lo complete.

Traspaso operativo: [`docs/HANDOVER.md`](../HANDOVER.md).

## 2. Rutinas de mantenimiento

| Frecuencia | Acción |
|------------|--------|
| Antes de cambios de modelo / release | Respaldo PostgreSQL ([`BACKUP.md`](../BACKUP.md)) |
| Cada despliegue | Verificar health, login, migraciones aplicadas, correo de prueba |
| Tras actualizar NuGet | `dotnet test` + `dotnet list package --vulnerable --include-transitive` ([`docs/calidad/`](../calidad/)) |
| Inventario inicial | Import Excel solo con backup; el import **sobrescribe stock** (FAQ) |
| Rotación | `ADMIN_SEED_PASSWORD`, `Email__ApiKey`, contraseña PG (HANDOVER) |

## 3. Release notes — v1.0

Fuente canónica de hitos/PRs: [`docs/entrega/CHANGELOG.md`](../entrega/CHANGELOG.md) (etiqueta documental **v1.0**, 2026-11-11; referencia `main` `52e4170` / #162). Resumen operativo:

### 3.1 Cierre (#161–#162) y correcciones de stock (#152, #159, #160)

- Verificación final de entrega (seed, HANDOVER, README).
- Cierre de hallazgos de auditoría (stock, seed, privacidad, calidad, entrega).
- Consumo único / no descontar de nuevo lo entregado; no consumir en orden finalizada/cancelada; reingreso de sobrante.

### 3.2 Actas, Serilog, MES y actividad (#150–#158)

- Actas: planta, firma pendiente, anulación, PDF, verificación QR y correo.
- Serilog de peticiones y eventos de negocio.
- Concurrencia de stock/numeración; ciclo de prenda por etapa; actividad instructor (pantalla/Excel/PDF); ayuda con búsqueda.

### 3.3 Inventario CMTC y privacidad 1.1 (hito 2026-10)

- Inventario unificado por planta (#134); import Excel / plantilla CMTC (#136–#141, #143).
- Política de datos personales 1.1 (#141); sesión/antiforgery/habeas data (#132).

### 3.4 Correo HTTPS, PostgreSQL y plantas (2026-09–10)

- Resend → Brevo documentado (#127–#128); opción Resend sigue configurable.
- Migración SQLite → PostgreSQL 16 (#97–#98); persistencia fotos/outbox/cookies en PG (#123); Render/`DATABASE_URL`.

### 3.5 Bodegas→plantas, BOM/MRP/MES, base auth

- Renombre Bodega→PlantaInventario / Encargado de bodega (#85); multi-planta y auditoría (#71–#81).
- Flujo MES de órdenes (#45); alcance por rol, BOM/MRP, activity log (#45–#70); auth/usuarios y CI tempranos (#1–#44).

### 3.6 Calidad medida en esta entrega documental

- Suite: **768** passed / 0 failed (2026-11-11) — [`docs/calidad/INFORME-PRUEBAS.md`](../calidad/INFORME-PRUEBAS.md).
- Deuda: HANDOVER. UAT / pentest / rendimiento: **no ejecutados**.

## 4. Cómo reportar un incidente (provisional)

Hasta definir contactos institucionales: documentar entorno (`ASPNETCORE_ENVIRONMENT`), hora UTC, usuario/rol (sin contraseñas), URL, mensaje de error / código de correlación de la página Error, y si aplica logs Serilog del contenedor. Canal formal: **PENDIENTE**.
