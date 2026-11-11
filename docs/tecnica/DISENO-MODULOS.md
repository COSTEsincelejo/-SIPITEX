# Diseño por módulos

**Fuente:** README módulos, `ApplicationServiceExtensions`, controllers, entidades Domain, EXTRACCION §2.  
UI = vistas bajo `src/Sipitex.Web/Views/{Controller}/` (convención MVC).

| Módulo | Entidades principales | Servicios | UI (controller / rutas) |
|--------|----------------------|-----------|-------------------------|
| Usuarios / cuenta | `User`, `UserProfilePhoto`, `PasswordResetToken`, `AceptacionTratamiento` | `UserAccountService`, `PasswordResetService` | `/Account/*` (Users solo Admin) |
| Plantas de inventario | `PlantaInventario`, `Material`, `FichaTecnicaMaterial`, `UserPlantaInventario`, `StockMovement` | `InventarioPlantaService`, `PlantaInventarioService`, `InventoryService`, `StockMovementService`, `FichaTecnicaMaterialService` | `/PlantasInventario` |
| Importación inventario | mismas + movimientos | `InventarioImportService` | `/Inventario/Importar` (Admin) |
| Solicitudes material | `SolicitudMaterial`, `DetalleSolicitudMaterial`, `EntregaMaterial` | `SolicitudMaterialService`, `SolicitudMaterialApprovalService`, `CodigoGeneradorService` | `/SolicitudesMaterial`, cola `/PlantasInventarioSolicitudes` |
| Órdenes + materiales planta | `ProductionOrder`, `ProductionOrderMaterialRequirement`, `ProductionOrderBomSnapshot` | `ProductionOrderService`, `OrderMaterialService` | `/Ordenes`, `/PlantasInventarioOrdenes` |
| MES | `ProductionOrderStage`, `ProductionOrderStageMovement`, `ProductFlowTemplate*`, `FinishedGoodStock`, `FinishedGoodMovement`, `InstructorStagePermission` | `ProductionFlowService`, `ProductLifecycleService`, `ProductStateMachine` | acciones etapa en `/Ordenes` |
| MRP / BOM | `BomProduct`, `BomItem`, tallas/piezas/medidas, `BomProductInstructor` | `MrpService`, `BomCatalogService` | `/Mrp` |
| Consumos | `ConsumoMaterial` | `MaterialConsumptionService`, `MaterialConsumptionCostService`, `ProductionConsumptionService` | `/Consumos` |
| Fichas (SENA) | `Ficha`, `FichaInstructor`, `ProductionSession` | `FichaService` | `/Fichas` |
| Grupos confección | `GrupoConfeccion` | `GrupoConfeccionService` | `/GruposConfeccion` |
| Calidad | `QualityRecord` | `QualityService` | `/Calidad` |
| Costeo | costos en material/consumo; `AppSetting` tarifa | `GarmentCostingService`, `CostingSettingsService`, `InsumoCostoService` | `/Costos` |
| Actas | `ActaMovimiento`, `ActaMovimientoDetalle` | `ActaMovimientoService`; PDF `ActaPdfService` | `/Actas`, `/Verificar/Acta` |
| Trazabilidad | `PrendaTrazable` | `TrazabilidadService` | `/Trazabilidad` |
| Estadísticas / reportes | lecturas OP/calidad/consumo | `StatisticsService`; Infrastructure `ReportService`, actividad instructor | `/Estadisticas`, `/Reportes` |
| Alertas | `AlertPreference`, `AlertDelivery` | `AlertService` + hosted `AlertEvaluationHostedService` | `/Alertas` |
| Auditoría | `ActivityLog`, `OrderChangeLog` | `ActivityLogService` | `/Auditoria` (Admin) |
| Búsqueda / ayuda | — (catálogos Application) | `BuscadorSugerenciasService`, `BusquedaService` (legacy), `AyudaCatalogo` | `/Buscar`, `/api/busqueda`, `/Ayuda` |
| Privacidad | aceptaciones | `PoliticaPrivacidadDocumento` / options | `/Privacidad` |

## Notas de diseño observadas en código

- **Dos flujos de salida de material:** (1) materiales de OP (`OrderMaterialService` + `StockMovement`); (2) `SolicitudMaterial` → `EntregaMaterial`.
- **Legacy:** `MaterialRequest` y `/api/busqueda` sin uso de producto preferido (HANDOVER #7); UI nueva de búsqueda: `Buscar/Sugerencias`.
- **PT:** stock en `FinishedGoodStock`; sin módulo de menú dedicado (HANDOVER #2).
