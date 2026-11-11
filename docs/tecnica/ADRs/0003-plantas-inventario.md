# ADR 0003 — Inventario por plantas (`PlantaInventario`)

- **Estado:** aceptada
- **Fecha:** 2026-10-28 (`a186fbf` — Unificar inventario por planta en una sola pantalla; nomenclatura previa `996a4df`)

## Contexto

El stock físico se organiza por ubicaciones. El rol operativo se llama «Encargado de bodega», pero el modelo de dominio usa **planta de inventario** (glosario `AyudaCatalogo`).

## Decisión

- Entidad `PlantaInventario` (catálogo; Planta 1 por defecto; baja lógica `Activo`).
- Cada `Material` tiene `PlantaInventarioId` obligatorio (default 1).
- Encargados se asignan vía `UserPlantaInventario` (M2M).
- UI operativa: `/PlantasInventario` (+ solicitudes/órdenes de planta). `/Inventario` redirige; solo queda importación admin.

## Consecuencias

- Consulta de stock con policy `PuedeConsultarInventario` (Admin o Encargado).
- Solicitudes (`SolicitudMaterial`) y actas referencian planta origen/destino.
- Nomenclatura «bodega» vs «planta» sigue siendo deuda (HANDOVER #4).
