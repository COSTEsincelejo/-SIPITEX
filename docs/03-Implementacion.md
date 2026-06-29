# Fase 3 — Implementación (Cascada)

## 3.1 Mapeo módulo → código

| Módulo UI | Controller | Servicio |
|-----------|------------|----------|
| Inventario | `InventarioController` | `InventoryService` |
| Órdenes | `OrdenesController` | `ProductionOrderService` |
| MRP | `MrpController` | `MrpService` |
| Fichas | `FichasController` | `FichaService` |
| Calidad | `CalidadController` | `QualityService` |
| Estadísticas | `EstadisticasController` | `StatisticsService` |
| Requisitos | `RequisitosController` | `RequirementService` |

## 3.2 Reglas de negocio implementadas

1. **Consumo BOM:** al registrar producción se descuenta stock según la lista de materiales del producto.  
2. **Stock mínimo:** alerta visual cuando `Stock < MinStock`.  
3. **Solicitudes:** estado Pendiente → Aprobada descuenta inventario.  
4. **Órdenes:** al alcanzar `ProducedQuantity >= TotalQuantity` → estado Finalizada.  
5. **MRP:** calcula requerimiento neto = BOM × cantidad − stock disponible.

## 3.3 Seed de datos

`DbInitializer` carga materiales, BOM (Camisa/Pantalón), órdenes OP-001/OP-002, fichas y matriz RF/RNF.

## 3.4 Entregable de fase

Código compilable en `src/` → pruebas (Fase 4).
