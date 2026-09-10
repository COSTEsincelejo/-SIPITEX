# Fase 3 — Implementación (Cascada)

## 3.1 Mapeo módulo → código

Cada controlador de `src/Sipitex.Web/Controllers` y su función:

| Módulo UI | Ruta | Controller | Servicio | Función |
|-----------|------|------------|----------|---------|
| Inicio | `/Home` | `HomeController` | — | Landing autenticada, privacidad y página de error |
| Inventario | `/Inventario` | `InventarioController` | `InventoryService` | Stock, altas, estado de material, movimientos |
| Órdenes | `/Ordenes` | `OrdenesController` | `ProductionOrderService`, `ProductionFlowService` | Crear/aprobar/cancelar órdenes y flujo MES |
| MRP | `/Mrp` | `MrpController` | `MrpService`, `BomCatalogService` | Ficha técnica y simulación de requerimientos |
| Fichas | `/Fichas` | `FichasController` | `FichaService` | Grupos SENA, sesiones de producción |
| Solicitudes (ficha) | `/SolicitudesMaterial` | `SolicitudesMaterialController` | `SolicitudMaterialService` | Solicitudes multi-ítem por ficha o insumos libres |
| Plantas de inventario | `/PlantasInventario` | `PlantasInventarioController` | `PlantaInventarioService` | Catálogo y reasignación de plantas |
| Solicitudes de planta | `/PlantasInventarioSolicitudes` | `PlantasInventarioSolicitudesController` | `SolicitudMaterialService` | Cola de aprobación por planta / rol |
| Órdenes de planta | `/PlantasInventarioOrdenes` | `PlantasInventarioOrdenesController` | `OrderMaterialService`, `ProductionFlowService` | Entrega de materiales y reingreso |
| Grupos de confección | `/GruposConfeccion` | `GruposConfeccionController` | `GrupoConfeccionService` | Registro de horas y prendas por grupo |
| Consumos | `/Consumos` | `ConsumosController` | `MaterialConsumptionService` | Consumo real de materiales por orden |
| Costeo | `/Costos` | `CostosController` | `GarmentCostingService` | Costo estimado (histórico de compra + tarifa) |
| Actas | `/Actas` | `ActasController` | `ActaMovimientoService` | Actas de ingreso/egreso y PDF |
| Calidad | `/Calidad` | `CalidadController` | `QualityService` | Inspecciones y reproceso |
| Auditoría | `/Auditoria` | `AuditoriaController` | `ActivityLogService` | Consulta de activity log (Administrador) |
| Estadísticas | `/Estadisticas` | `EstadisticasController` | `StatisticsService` | KPIs y gráficos |
| Reportes | `/Reportes` | `ReportesController` | `ReportService` | PDF / Excel |
| Alertas | `/Alertas` | `AlertasController` | `AlertService` | Preferencias y evaluación de correo |
| Usuarios / login | `/Account` | `AccountController` | `UserAccountService` | Login, perfil y CRUD de usuarios |
| Búsqueda | `/api/busqueda` | `BusquedaController` | `BusquedaService` | Autocompletado del header |

El flujo MES (Trazo → Corte → Confección → Control de Calidad → Terminado) es **aditivo** respecto al consumo BOM; los diagramas de secuencia de órdenes en `docs/diagramas` siguen describiendo el alta de la orden. No se regeneraron PNG: el estado del producto no cambió de semántica, solo se documentaron módulos posteriores.

## 3.2 Reglas de negocio implementadas

1. **Consumo BOM:** al registrar producción se descuenta stock según la lista de materiales del producto.  
2. **Stock mínimo:** alerta visual cuando `Stock < MinStock`.  
3. **Solicitudes:** estado Pendiente → Aprobada descuenta inventario.  
4. **Órdenes:** nacen en Pendiente; el Administrador aprueba a EnProceso; al alcanzar `ProducedQuantity >= TotalQuantity` → Finalizada.  
5. **MRP:** calcula requerimiento neto = BOM × cantidad − stock disponible.  
6. **Costeo:** materiales al costo histórico de la última entrada con precio; mano de obra = horas de grupo × `Costing:LaborHourRate` (aviso en UI si la tarifa es 0).  
7. **Actas:** conformidad simple (nombre, cargo, timestamp UTC); política definitiva sin firma gráfica.  
8. **Login:** 5 fallos (correo + IP) → bloqueo 15 minutos.

## 3.3 Seed de datos

`DbInitializer` aplica migraciones y, si el catálogo está vacío, carga materiales, BOM (Camisa/Pantalón), órdenes OP-001/OP-002, fichas y matriz RF/RNF.

Los usuarios `*@sipitex.test` solo se crean con `Seed:DemoUsers=true`. En producción, si no hay Administrador, se crea `admin@sipitex.local`.

## 3.4 Entregable de fase

Código compilable en `src/` → pruebas (Fase 4).
