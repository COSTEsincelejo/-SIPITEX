# ADR 0007 — Seed condicionado al entorno

- **Estado:** aceptada
- **Fecha:** 2026-11-11 (`560f6af` / verificación entrega `52e4170`; lógica en `Program.cs` + `DbInitializer`)

## Contexto

Producción no debe cargar usuarios/materiales/órdenes demo. Desarrollo y demos públicas sí pueden.

## Decisión

Resolución en `ResolveSeedDemoData` (`Program.cs`):

1. Si existe `SEED_DEMO_DATA` → true solo con `1`/`true`/`yes` (case variants).
2. Si no → `Seed:DemoUsers` (default: true en Development, false en producción vía config / `render.yaml` `Seed__DemoUsers=false`).

Comportamiento (`DbInitializer`):

- Con seed demo: usuarios demo + materiales/órdenes/fichas de ejemplo si la BD está vacía de materiales.
- Sin seed demo: si no hay Administrador y `ADMIN_SEED_PASSWORD` ≥ 6 caracteres → crea `admin@sipitex.local` (contraseña no se loguea).
- Siempre: RF/RNF seed, preferencias alerta, catálogo CMTC BOM (`CmtcBomCatalogSeed`).

## Consecuencias

- HANDOVER #10: no reactivar `SEED_DEMO_DATA` en entorno real.
- Credenciales demo solo Development (`admin@sipitex.test`, etc. en README).
- Inventario real: importación Excel + `GUIA-INVENTARIO-INICIAL.md` (sobrescribe stock).
