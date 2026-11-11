# Matriz de trazabilidad — SIPITEX v1.0

Fuente: `docs/entrega/EXTRACCION-DATOS-REPO.md` §2 (mapeo por numeración **seed / `01-Requisitos`**).  
Para equivalencias con IEEE 830 ver `MATRIZ-ENTREGABLES-REQUISITOS.md`.

Criterio: controllers, services y tests existentes; no “corrige” el seed.

---

## RF01–RF20 (seed / 01-Requisitos)

| ID | Texto corto (seed) | Módulo | Clase / página (controllers · services) | Tests (ejemplos) |
|----|--------------------|--------|------------------------------------------|------------------|
| RF01 | CRUD usuarios por rol | Usuarios | `AccountController` (Users, CreateUser, EditUser, ToggleUserStatus, DeleteUser) · `UserAccountService` | `UserAccountServiceTests.cs` |
| RF02 | Autenticación por credenciales | Usuarios | `AccountController` (Login, Logout, reset/confirm) · `SesionAutenticada`; cookies `Program.cs` | `LoginLockoutTests.cs`, `EmailConfirmationFlowTests.cs`, `PasswordResetServiceTests.cs` |
| RF03 | Entradas con fecha/cantidad/unidad | Inventario | `PlantasInventarioController` (AddMaterial, AdjustStock); import `InventarioController` · `InventoryService`, `InventarioImportService` | `InventoryEditAndEntryOriginTests.cs`, `InventarioImportTests.cs` |
| RF04 | Consultar stock | Inventario | `PlantasInventarioController` (Index, Consultar, Detalle) · `InventarioPlantaService`, `InventoryService` | `PlantasInventarioConsultarControllerTests.cs`, `InventarioConsultaFilterTests.cs`, `InventarioInstructorRestrictionTests.cs` |
| RF05 | Estado material | Inventario | `PlantasInventarioController.UpdateStatus` · `InventoryService` | `InventoryEditAndEntryOriginTests.cs` (relacionado) |
| RF06 | Solicitar materiales | Salida | `SolicitudesMaterialController`; cola `PlantasInventarioSolicitudesController` · `SolicitudMaterialService` | `SolicitudMaterialServiceTests.cs`, `SolicitarInsumosPlantasTests.cs`, `Integration/SolicitudMaterialFlowTests.cs` |
| RF07 | Aprobar/rechazar | Salida | `PlantasInventarioSolicitudesController.Resolve`; entrega `PlantasInventarioOrdenesController.Deliver` · `SolicitudMaterialApprovalService`, `OrderMaterialService` | `SolicitudMaterialApprovalServiceTests.cs`, `InventoryApproveRejectTests.cs` |
| RF08 | Salida trazada por orden | Salida | `PlantasInventarioOrdenesController`, `OrdenesController` · `OrderMaterialService`, `StockMovementService` | `OrderMaterialServiceTests.cs`, `StockMovementLedgerTests.cs`, `StockUnicoDescuentoTests.cs` |
| RF09 | Crear órdenes | Órdenes | `OrdenesController.Create` · `ProductionOrderService` | `ProductionOrderServiceTests.cs`, `OrdenesCrearPermissionTests.cs` |
| RF10 | Estados de orden | Órdenes | `OrdenesController` (Approve, Cancel, Edit, etapas) · `ProductionOrderService`, `ProductionFlowService` | `OrderApprovePendingTests.cs`, `ProductionOrderEditCancelTests.cs`, `ProductionFlowServiceTests.cs` |
| RF11 | MRP | Órdenes/MRP | `MrpController` (Index, Simulate, BOM) · `MrpService`, `BomCatalogService` | `MrpServiceTests.cs`, `BomCatalogServiceTests.cs`, `MrpGestionarFichasPermissionTests.cs` |
| RF12 | Ficha ↔ proceso | Fichas | `FichasController` · `FichaService` | `FichaCreateAndFilterTests.cs`, `FichaInstructorProcesoTests.cs` |
| RF13 | Múltiples fichas / instructor | Fichas | `FichasController` (AssignInstructor, …) · `FichaService`; `FichaInstructor` | `FichaInstructorAssignmentTests.cs`, `FichaServiceScopeTests.cs` |
| RF14 | Sesión diaria | Producción | `FichasController` (Register, QuickRegister) · `FichaService` | `FichaAssignedOrderTests.cs` |
| RF15 | Avance vs meta | Producción | `OrdenesController` (AddProduction, Detail); `EstadisticasController` · `ProductionOrderService`, `StatisticsService` | `ProductionOrderServiceTests.cs`, `EstadisticasInstructorScopeTests.cs` |
| RF16 | Criterios por tipo de prenda | Calidad | `CalidadController` · `QualityService` | `QualityInstructorScopeTests.cs` — seed **Ausente** (solo Aprobado/Reproceso) |
| RF17 | Reproceso motivo/responsable | Calidad | `CalidadController.Create` · `QualityService` | `QualityInstructorScopeTests.cs` |
| RF18 | Reportes período/instructor | Estadísticas | `ReportesController` · reporting + `ActividadInstructorService` | `ReportesInstructorScopeTests.cs`, `ReportFilterHelperTests.cs`, `ActividadInstructorServiceTests.cs` |
| RF19 | Consumo de materiales | Estadísticas | `ConsumosController` · `MaterialConsumptionService`, `ProductionConsumptionService`, `MrpService` | `MaterialConsumptionServiceTests.cs`, `ProductionConsumptionLedgerTests.cs` |
| RF20 | Dashboard KPI | Estadísticas | `EstadisticasController.Index`; `HomeController.Index` · `StatisticsService` | `EstadisticasInstructorScopeTests.cs` |

---

## RNF01–RNF08

| ID | Texto corto | Módulo / soporte | Clase / artefacto | Tests |
|----|-------------|------------------|-------------------|-------|
| RNF01 | Carga &lt; 2 s | PENDIENTE | PENDIENTE benchmark | PENDIENTE |
| RNF02 | Sesión con expiración | Seguridad | `Program.cs` cookie; `SeguridadOptions` (default **20 min**) | PENDIENTE test TTL exacto |
| RNF03 | Acceso por rol | Autorización | Controllers `[Authorize]`; `PermissionRules`; `SipitexAuthorizationExtensions` | `AuthorizationPolicyTests.cs`, `ModuleAccessTests.cs` |
| RNF04 | Acceso intranet | Despliegue | `docs/05-Despliegue.md`, `DEPLOY.md`, Docker/Render | PENDIENTE prueba de red |
| RNF05 | UI responsiva | Web | Vistas `src/Sipitex.Web/Views`, CSS wwwroot | PENDIENTE |
| RNF06 | Código modular | Arquitectura | `Sipitex.Domain` / `Application` / `Infrastructure` / `Web` | — |
| RNF07 | Docker Compose | Despliegue | `Dockerfile`, `docker-compose.yml` | — |
| RNF08 | Integridad / transacciones | Datos | EF Core + Npgsql; `SipitexDbContext` | `ConcurrenciaStockYNumeracionTests.cs`, `StockUnicoDescuentoTests.cs` |

---

## Módulos posteriores (extracción §2.1)

| Módulo | Controller | Servicio(s) | Tests (ejemplos) |
|--------|------------|-------------|------------------|
| Plantas de inventario | `PlantasInventarioController` | `InventarioPlantaService`, `PlantaInventarioService` | `PlantaInventarioServiceTests.cs`, `PlantasInventarioDetalleTests.cs` |
| Solicitudes de planta | `PlantasInventarioSolicitudesController` | `SolicitudMaterialService` | `PlantaInventarioSolicitudesScopeTests.cs` |
| Órdenes de planta | `PlantasInventarioOrdenesController` | `OrderMaterialService`, `ProductionFlowService` | `OrderMaterialServiceTests.cs`, `StageReentryTests.cs` |
| Grupos de confección | `GruposConfeccionController` | `GrupoConfeccionService` | `GrupoConfeccionServiceTests.cs` |
| Consumos | `ConsumosController` | `MaterialConsumptionService` | `MaterialConsumptionServiceTests.cs` |
| Costeo | `CostosController` | `GarmentCostingService`, `CostingSettingsService` | `GarmentCostingServiceTests.cs`, `CostosControllerTests.cs` |
| Actas | `ActasController` (+ `VerificarController`) | `ActaMovimientoService`; `ActaPdfService` | `ActasControllerTests.cs`, `VerificarActaTests.cs` |
| Trazabilidad | `TrazabilidadController` | `TrazabilidadService` | `TrazabilidadControllerTests.cs`, `TrazabilidadServiceTests.cs` |
| Auditoría | `AuditoriaController` | `ActivityLogService` | `AuditoriaControllerTests.cs`, `ActivityLogServiceTests.cs` |
| Alertas | `AlertasController` | `AlertService` | `AlertStockYSolicitudesTests.cs`, `AlertNotifyUsersTests.cs` |
| Búsqueda | `BuscarController`, `BusquedaController` | `BuscadorCatalogo` / `IBusquedaService` | `BuscarControllerTests.cs`, `BuscadorCatalogoTests.cs` |
| Ayuda | `AyudaController` | `AyudaCatalogo` | `AyudaCatalogoTests.cs`, `AyudaBusquedaTests.cs` |
| Privacidad | `PrivacidadController` | `PoliticaPrivacidadDocumento` | `PoliticaPrivacidadHttpTests.cs` |

Catálogo de funcionalidades: `src/Sipitex.Application/Services/FuncionalidadesCatalog.cs`.

---

## Suite global (referencia entrega)

| Métrica | Valor (2026-11-11) |
|---------|-------------------|
| Resultado | 768 passed, 0 failed, ~19 s |
| Cobertura line-rate | 87,06 % (branch 48,05 %) |
