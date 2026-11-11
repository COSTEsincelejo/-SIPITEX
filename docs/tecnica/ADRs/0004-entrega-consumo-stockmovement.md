# ADR 0004 — Entrega descuenta stock; consumo solo anota

- **Estado:** aceptada
- **Fecha:** 2026-11-09 (`c304b39` — el consumo ya no vuelve a descontar el stock entregado; refuerzo `c3dcc94` 2026-11-06)

## Contexto

Había riesgo de **doble descuento**: bajar `Material.Stock` en la entrega de la OP y otra vez al registrar consumo.

## Decisión

- **Entrega** (`OrderMaterialService` / `PlantasInventarioOrdenesController.Deliver`): actualiza `ProductionOrderMaterialRequirement.QuantityDelivered`, descuenta `Material.Stock` y escribe ledger `StockMovement`.
- **Consumo** (`MaterialConsumptionService`): crea `ConsumoMaterial` enlazado a la línea entregada; comentario explícito en el servicio: *no* crea otro `StockMovement` ni resta stock.
- Reingreso de sobrante: `QuantityReentered` / flujo de reingreso (`129390b`).

## Consecuencias

- Stock físico = entregas (y ajustes/entradas/reingresos), no = suma de fichas de consumo.
- Cancelar orden o anular acta **no** revierte stock ya aplicado (HANDOVER #3).
- Costeo usa snapshot de costo en el consumo (`CostoUnitarioAlMomento`).
