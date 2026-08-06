# Ampliación MES — Órdenes de producción

## Fecha
2026-08-06

## Objetivo
Extender el módulo de Órdenes con flujo de etapas tipo MES/ERP **sin romper** MRP, inventario de insumos, materiales de bodega ni `AddProduction`.

## Análisis previo (resumen)
- `ProducedQuantity` + BOM/bodega ya gestionan avance y stock de insumos.
- Fichas usan `ProcessName` libre (no es pipeline).
- No existía inventario de producto terminado → se creó tabla nueva `FinishedGoodStocks`.
- Riesgo crítico: doble conteo si cada etapa llamara `RegisterProductionAsync` → las etapas manejan WIP aparte; el ingreso parcial a inventario y el `+ uds` legacy actualizan `ProducedQuantity` de forma controlada.

## Tablas nuevas (aditivas)
- `ProductFlowTemplates` / `ProductFlowStageTemplates`
- `ProductionOrderStages`
- `ProductionOrderStageMovements` (append-only)
- `ProductionOrderHistoryEntries` (append-only)
- `FinishedGoodStocks` / `FinishedGoodMovements`
- `InstructorStagePermissions`
- Columnas en `ProductionOrders`: `ClientName`, `CurrentStageId`

## Comportamiento preservado
- `Ordenes/Create`, `AddProduction`, materiales de bodega, BOM snapshot, Fichas, Calidad: intactos.
- Órdenes sin etapas se inicializan al abrir detalle o al crear (plantilla por producto o default Trazo→…→Terminado).

## Diferido (opcional siguiente iteración)
- Adjuntos de archivos en la orden.
- UI dedicada de plantillas por producto (hoy se seedan default Camisa/Pantalón/*).
- Pantalla de inventario de producto terminado en menú Inventario.
