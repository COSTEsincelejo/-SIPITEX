# ADR 0005 — Snapshot BOM por orden de producción

- **Estado:** aceptada
- **Fecha:** 2026-09-16 (entidad presente en modelo PostgreSQL consolidado `57a2512`; uso en costeo/congelado reforzado `6bcb592` 2026-09-17)

## Contexto

El BOM vigente (`BomProduct` / `BomItem`) puede editarse después de crear una OP. Producción y costeo necesitan la receta **congelada** al momento de la orden.

## Decisión

- Al crear/gestionar la OP se persiste `ProductionOrderBomSnapshot` (código/nombre material, `QuantityPerUnit`, unidad) por `ProductionOrderId`.
- Comentario en `ProductionOrder`: *«Receta congelada al crear la orden (independiente del BOM vigente)»*.
- Importación de requisitos desde BOM / MRP trabaja sobre esa base; MRP **no** se calcula solo al crear la OP (HANDOVER #1).

## Consecuencias

- Cambios posteriores al BOM de catálogo no reescriben snapshots de órdenes existentes.
- Costeo de prenda se apoya en materiales congelados / consumos con costo al momento.
- FK `Material` en snapshot con Restrict (no borrar material si hay snapshot).
