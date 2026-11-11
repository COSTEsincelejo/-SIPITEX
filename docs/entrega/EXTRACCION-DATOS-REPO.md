# Extracción de datos reales del repositorio — SIPITEX

**Fuente:** solo archivos del repo. Sin inventar.  
**Fecha de extracción:** 2026-11-11  
**Rama base:** `main` (actualizado al momento de la extracción).

> **Aviso de numeración:** `docs/01-Requisitos.md` + seed `DbInitializer.SeedRequirementsAsync` usan una numeración RF; `docs/08-IEEE830-Especificacion.md` usa **otra** (mismos IDs, textos distintos). Abajo se listan las tres fuentes por separado.

---

## 1. Requisitos funcionales y no funcionales

### 1.1 RF — `docs/01-Requisitos.md`

| ID | Módulo | Texto corto | Prioridad | Fuente |
|----|--------|-------------|-----------|--------|
| RF01 | Usuarios | CRUD usuarios por rol | Alta | `docs/01-Requisitos.md` |
| RF02 | Usuarios | Autenticación JWT | Alta | `docs/01-Requisitos.md` |
| RF03 | Inventario | Registrar entradas con fecha | Media | `docs/01-Requisitos.md` |
| RF04 | Inventario | Consultar stock en tiempo real | Alta | `docs/01-Requisitos.md` |
| RF05 | Inventario | Estado del material | Media | `docs/01-Requisitos.md` |
| RF06 | Salida | Solicitud de materiales | Alta | `docs/01-Requisitos.md` |
| RF07 | Salida | Aprobación encargado de bodega | Alta | `docs/01-Requisitos.md` |
| RF08 | Salida | Trazabilidad por orden | Media | `docs/01-Requisitos.md` |
| RF09 | Órdenes | Crear órdenes de producción | Alta | `docs/01-Requisitos.md` |
| RF10 | Órdenes | Estados de orden | Media | `docs/01-Requisitos.md` |
| RF11 | Órdenes | Cálculo MRP automático | Alta | `docs/01-Requisitos.md` |
| RF12 | Fichas | Ficha ↔ proceso | Alta | `docs/01-Requisitos.md` |
| RF13 | Fichas | Múltiples fichas por instructor | Baja | `docs/01-Requisitos.md` |
| RF14 | Producción | Sesión diaria con observaciones | Media | `docs/01-Requisitos.md` |
| RF15 | Producción | Avance vs meta | Alta | `docs/01-Requisitos.md` |
| RF16 | Calidad | Criterios por tipo de prenda | Media | `docs/01-Requisitos.md` |
| RF17 | Calidad | Trazabilidad reproceso | Media | `docs/01-Requisitos.md` |
| RF18 | Estadísticas | Reportes por período/instructor | Media | `docs/01-Requisitos.md` |
| RF19 | Estadísticas | Consumo de materiales | Alta | `docs/01-Requisitos.md` |
| RF20 | Estadísticas | Dashboard KPI | Alta | `docs/01-Requisitos.md` |

### 1.2 RNF — `docs/01-Requisitos.md`

| ID | Texto corto | Fuente |
|----|-------------|--------|
| RNF01 | Carga < 2 s en intranet | `docs/01-Requisitos.md` |
| RNF02 | JWT con expiración | `docs/01-Requisitos.md` |
| RNF03 | Control de acceso por rol | `docs/01-Requisitos.md` |
| RNF04 | Acceso desde PCs de la red | `docs/01-Requisitos.md` |
| RNF05 | UI responsiva | `docs/01-Requisitos.md` |
| RNF06 | Código modular | `docs/01-Requisitos.md` |
| RNF07 | Docker Compose | `docs/01-Requisitos.md` |
| RNF08 | Integridad transaccional | `docs/01-Requisitos.md` |

### 1.3 RF — seed `DbInitializer` (`FunctionalRequirement`)

Fuente: `src/Sipitex.Infrastructure/Data/DbInitializer.cs` (`SeedRequirementsAsync`).

| ID | Módulo | Texto corto (Description) | Status seed | Observation seed |
|----|--------|---------------------------|-------------|------------------|
| RF01 | Usuarios | Crear, editar y desactivar usuarios por rol. | Cumple | CRUD de usuarios con roles. |
| RF02 | Usuarios | Autenticación con credenciales propias por rol. | Cumple | Login con cookies de autenticación. |
| RF03 | Inventario | Registrar entradas con fecha, cantidad y unidad. | Cumple | Fecha de última entrada en material. |
| RF04 | Inventario | Consultar stock disponible en tiempo real. | Cumple | Actualización reactiva. |
| RF05 | Inventario | EncargadoDeBodega registra estado del material (Bueno/Regular/Deteriorado). | Cumple | Selector de estado en inventario. |
| RF06 | Salida | Instructor solicita materiales (orden, producto, cantidad). | Cumple | Formulario de solicitud. |
| RF07 | Salida | EncargadoDeBodega aprueba / rechaza y registra entrega. | Cumple | Aprobación y rechazo implementados. |
| RF08 | Salida | Salida trazada por orden y producto. | Parcial | Se guarda orderId pero no historial visible. |
| RF09 | Órdenes | Admin crea órdenes con producto, cantidad y fecha. | Cumple | Formulario completo. |
| RF10 | Órdenes | Estados: Pendiente, En Proceso, Finalizada, Cancelada. | Parcial | Solo 'En Proceso' y 'Finalizada'. |
| RF11 | Órdenes | Cálculo MRP automático por prenda y talla. | Cumple | BOM hardcoded, falta diferenciación por talla. |
| RF12 | Fichas | Fichas asociadas a un proceso de la línea. | Cumple | Cada ficha tiene proceso. |
| RF13 | Fichas | Un instructor puede tener múltiples fichas. | Parcial | Estructuralmente posible, sin UI asignación. |
| RF14 | Producción | Instructor registra sesión diaria: ficha, unidades, observaciones. | Cumple | Sesiones con observaciones persistidas. |
| RF15 | Producción | Mostrar avance acumulado vs meta de la orden. | Cumple | Tabla de órdenes muestra avance. |
| RF16 | Calidad | Criterios diferenciales por tipo de prenda. | Ausente | Solo aprobado/reproceso genérico. |
| RF17 | Calidad | Prendas en reproceso con trazabilidad de motivo y responsable. | Cumple | Motivo y responsable obligatorios en reproceso. |
| RF18 | Estadísticas | Reportes de producción por período, instructor y orden. | Parcial | Por orden, falta filtro por período/instructor. |
| RF19 | Estadísticas | Estadísticas de consumo de materiales por producto. | Cumple | Cálculo vía BOM+MRP. |
| RF20 | Estadísticas | Dashboard con KPIs: unidades, calidad, eficiencia. | Cumple | KPIs y gráfico. |

### 1.4 RNF — seed `DbInitializer` (`NonFunctionalRequirement`)

| ID | Texto corto (Description) | Status seed | Observation seed |
|----|---------------------------|-------------|------------------|
| RNF01 | Carga rápida en intranet (<2s) | Cumple | HTML estático liviano. |
| RNF02 | Autenticación con sesión y expiración | Cumple | Cookies con expiración de 8 horas. |
| RNF03 | Control de acceso por rol | Cumple | Authorize por rol en controladores. |
| RNF04 | Disponible desde cualquier PC de la intranet | Parcial | Requiere servidor HTTP. |
| RNF05 | Interfaz responsiva e intuitiva | Cumple | Layout adaptable. |
| RNF06 | Código modular y documentado | Parcial | Arquitectura por capas. |
| RNF07 | Despliegue con Docker Compose | Cumple | Dockerfile + docker-compose.yml. |
| RNF08 | Integridad transaccional | Parcial | PostgreSQL con EF Core. |

### 1.5 RF — `docs/08-IEEE830-Especificacion.md` (numeración distinta)

| ID | Módulo | Texto corto | Prioridad | Fuente |
|----|--------|-------------|-----------|--------|
| RF01 | Usuarios | Crear, editar y desactivar usuarios por rol | Alta | `docs/08-IEEE830-Especificacion.md` |
| RF02 | Usuarios | Iniciar y cerrar sesión con cookies | Alta | `docs/08-IEEE830-Especificacion.md` |
| RF03 | Usuarios | Otorgar permisos extendidos desde el administrador | Alta | `docs/08-IEEE830-Especificacion.md` |
| RF04 | Inventario | Registrar cualquier material (nombre, stock, mínimo, unidad) | Alta | `docs/08-IEEE830-Especificacion.md` |
| RF05 | Inventario | Consultar stock y filtrar por nivel (Agotado / Por agotarse / Normal) | Alta | `docs/08-IEEE830-Especificacion.md` |
| RF06 | Inventario | Actualizar estado físico (Bueno / Regular / Deteriorado) | Media | `docs/08-IEEE830-Especificacion.md` |
| RF07 | Inventario | Ajustar stock y fecha de última entrada | Media | `docs/08-IEEE830-Especificacion.md` |
| RF08 | Salida | Solicitar material para una orden (crear material si no existe) | Alta | `docs/08-IEEE830-Especificacion.md` |
| RF09 | Salida | Aprobar o rechazar solicitud; al aprobar, descontar stock | Alta | `docs/08-IEEE830-Especificacion.md` |
| RF10 | Órdenes | Crear orden con cualquier nombre de producto | Alta | `docs/08-IEEE830-Especificacion.md` |
| RF11 | Órdenes | Registrar avance y finalizar al alcanzar la meta | Alta | `docs/08-IEEE830-Especificacion.md` |
| RF12 | MRP | Mantener BOM por producto | Alta | `docs/08-IEEE830-Especificacion.md` |
| RF13 | MRP | Simular requerimiento neto frente al stock | Alta | `docs/08-IEEE830-Especificacion.md` |
| RF14 | Fichas | Asociar ficha a proceso, instructor y orden | Alta | `docs/08-IEEE830-Especificacion.md` |
| RF15 | Producción | Registrar sesión diaria (ficha, unidades, observaciones) | Alta | `docs/08-IEEE830-Especificacion.md` |
| RF16 | Calidad | Registrar inspección; en reproceso exigir motivo y responsable | Media | `docs/08-IEEE830-Especificacion.md` |
| RF17 | Reportes | Exportar inventario, órdenes, calidad y dashboard (PDF/Excel) | Alta | `docs/08-IEEE830-Especificacion.md` |
| RF18 | Reportes | Filtrar por día, semana, mes, año, instructor o ficha | Alta | `docs/08-IEEE830-Especificacion.md` |
| RF19 | Alertas | Preferencias por usuario y evaluación de eventos | Media | `docs/08-IEEE830-Especificacion.md` |
| RF20 | Estadísticas | Dashboard con KPIs y gráfico de avance | Alta | `docs/08-IEEE830-Especificacion.md` |

### 1.6 RNF — `docs/08-IEEE830-Especificacion.md`

| ID | Texto corto | Fuente |
|----|-------------|--------|
| RNF01 | Tiempo de respuesta percibido menor a 2 segundos en intranet | `docs/08-IEEE830-Especificacion.md` |
| RNF02 | Sesión autenticada con expiración (8 horas) | `docs/08-IEEE830-Especificacion.md` |
| RNF03 | Control de acceso por rol y permisos extendidos | `docs/08-IEEE830-Especificacion.md` |
| RNF04 | Acceso desde cualquier PC de la intranet del centro | `docs/08-IEEE830-Especificacion.md` |
| RNF05 | Interfaz responsiva (móvil y escritorio) | `docs/08-IEEE830-Especificacion.md` |
| RNF06 | Código modular por capas y documentado | `docs/08-IEEE830-Especificacion.md` |
| RNF07 | Despliegue reproducible con Docker Compose | `docs/08-IEEE830-Especificacion.md` |
| RNF08 | Integridad de datos con EF Core / PostgreSQL | `docs/08-IEEE830-Especificacion.md` |

**Discrepancia real en código vs RNF02 docs/seed:** cookie usa `SeguridadOptions.MinutosInactividad` (default **20** minutos) en `src/Sipitex.Application/Seguridad/SeguridadOptions.cs` + `src/Sipitex.Web/Program.cs` (`ExpireTimeSpan = TimeSpan.FromMinutes(seguridad.MinutosEfectivos())`). No hay “8 horas” en el código de sesión.

---

## 2. Mapeo RF/RNF → código (por numeración seed / `01-Requisitos`)

Criterio: módulos, controllers, services y tests existentes en el repo. Los `Status` del seed pueden estar desactualizados respecto al código (p. ej. RF10); aquí se cita evidencia de archivos, no se “corrige” el seed.

| ID | Texto corto (seed) | Controllers / rutas | Services / soporte | Tests (ejemplos) | Notas |
|----|--------------------|---------------------|--------------------|------------------|-------|
| RF01 | CRUD usuarios por rol | `AccountController` (`Users`, `CreateUser`, `EditUser`, `ToggleUserStatus`, `DeleteUser`) — `src/Sipitex.Web/Controllers/AccountController.cs` | `UserAccountService` — `src/Sipitex.Application/Services/UserAccountService.cs` | `tests/Sipitex.Tests/UserAccountServiceTests.cs` | Catálogo: `FuncionalidadesCatalog` “Gestión de usuarios” |
| RF02 | Autenticación por credenciales | `AccountController` (`Login`, `Logout`, reset/confirm) | `SesionAutenticada` — `src/Sipitex.Web/Security/SesionAutenticada.cs`; cookies en `Program.cs` | `LoginLockoutTests.cs`, `EmailConfirmationFlowTests.cs`, `PasswordResetServiceTests.cs` | `01-Requisitos` dice JWT; seed/IEEE/código = cookies |
| RF03 | Entradas con fecha/cantidad/unidad | `PlantasInventarioController` (`AddMaterial`, `AdjustStock`); import `InventarioController` | `InventoryService`, `InventarioImportService` | `InventoryEditAndEntryOriginTests.cs`, `InventarioImportTests.cs` | |
| RF04 | Consultar stock | `PlantasInventarioController` (`Index`, `Consultar`, `Detalle`) | `InventarioPlantaService`, `InventoryService` | `PlantasInventarioConsultarControllerTests.cs`, `InventarioConsultaFilterTests.cs`, `InventarioInstructorRestrictionTests.cs` | Policy `PuedeConsultarInventario` |
| RF05 | Estado material | `PlantasInventarioController.UpdateStatus` | `InventoryService` | `InventoryEditAndEntryOriginTests.cs` (relacionado) | |
| RF06 | Solicitar materiales | `SolicitudesMaterialController`; cola `PlantasInventarioSolicitudesController` | `SolicitudMaterialService` | `SolicitudMaterialServiceTests.cs`, `SolicitarInsumosPlantasTests.cs`, `Integration/SolicitudMaterialFlowTests.cs` | |
| RF07 | Aprobar/rechazar | `PlantasInventarioSolicitudesController.Resolve`; entrega `PlantasInventarioOrdenesController.Deliver` | `SolicitudMaterialApprovalService`, `OrderMaterialService` | `SolicitudMaterialApprovalServiceTests.cs`, `InventoryApproveRejectTests.cs` | Policy `PuedeAprobarSolicitudes` |
| RF08 | Salida trazada por orden | `PlantasInventarioOrdenesController`, `OrdenesController` (materiales OP) | `OrderMaterialService`, `StockMovementService` | `OrderMaterialServiceTests.cs`, `StockMovementLedgerTests.cs`, `StockUnicoDescuentoTests.cs` | Seed: Parcial |
| RF09 | Crear órdenes | `OrdenesController.Create` | `ProductionOrderService` | `ProductionOrderServiceTests.cs`, `OrdenesCrearPermissionTests.cs` | Policy `PuedeCrearOrdenes` |
| RF10 | Estados de orden | `OrdenesController` (`Approve`, `Cancel`, `Edit`, flujo etapas) | `ProductionOrderService`, `ProductionFlowService` | `OrderApprovePendingTests.cs`, `ProductionOrderEditCancelTests.cs`, `ProductionFlowServiceTests.cs` | Seed Observation desactualizada vs código (hay Pendiente/Cancelada) |
| RF11 | MRP | `MrpController` (`Index`, `Simulate`, BOM CRUD) | `MrpService`, `BomCatalogService` | `MrpServiceTests.cs`, `BomCatalogServiceTests.cs`, `MrpGestionarFichasPermissionTests.cs` | HANDOVER: no auto al crear OP |
| RF12 | Ficha ↔ proceso | `FichasController` | `FichaService` | `FichaCreateAndFilterTests.cs`, `FichaInstructorProcesoTests.cs` | Distinto de BOM / ficha técnica material |
| RF13 | Múltiples fichas / instructor | `FichasController` (`AssignInstructor`, …) | `FichaService`; entidad `FichaInstructor` | `FichaInstructorAssignmentTests.cs`, `FichaServiceScopeTests.cs` | Seed: Parcial; HANDOVER: `User.FichaAsignadaId` solo informativo |
| RF14 | Sesión diaria | `FichasController` (`Register`, `QuickRegister`) | `FichaService` | `FichaAssignedOrderTests.cs` | |
| RF15 | Avance vs meta | `OrdenesController` (`AddProduction`, `Detail`); `EstadisticasController` | `ProductionOrderService`, `StatisticsService` | `ProductionOrderServiceTests.cs`, `EstadisticasInstructorScopeTests.cs` | |
| RF16 | Criterios por tipo de prenda | `CalidadController` | `QualityService` | `QualityInstructorScopeTests.cs` | Seed: **Ausente** (solo Aprobado/Reproceso) |
| RF17 | Reproceso con motivo/responsable | `CalidadController.Create` | `QualityService` | `QualityInstructorScopeTests.cs` | |
| RF18 | Reportes período/instructor | `ReportesController` (`Ordenes`, `Actividad*`, …) | reporting en Infrastructure + `ActividadInstructorService` | `ReportesInstructorScopeTests.cs`, `ReportFilterHelperTests.cs`, `ActividadInstructorServiceTests.cs` | Seed: Parcial |
| RF19 | Consumo de materiales | `ConsumosController`; apoyo MRP/BOM | `MaterialConsumptionService`, `ProductionConsumptionService`, `MrpService` | `MaterialConsumptionServiceTests.cs`, `ProductionConsumptionLedgerTests.cs` | |
| RF20 | Dashboard KPI | `EstadisticasController.Index`; `HomeController.Index` | `StatisticsService` | `EstadisticasInstructorScopeTests.cs` | |
| RNF01 | Carga <2s | PENDIENTE (sin benchmark en repo) | PENDIENTE | PENDIENTE | Solo afirmación en docs/seed |
| RNF02 | Sesión con expiración | `Program.cs` cookie auth | `SeguridadOptions` | PENDIENTE test de TTL exacto | Default **20 min**, no 8 h |
| RNF03 | Acceso por rol | Controllers `[Authorize]`; policies | `PermissionRules`, `SipitexAuthorizationExtensions` | `AuthorizationPolicyTests.cs`, `ModuleAccessTests.cs` | |
| RNF04 | Acceso intranet | `docs/05-Despliegue.md`, `DEPLOY.md` | Docker/Render | PENDIENTE prueba de red | Seed: Parcial |
| RNF05 | UI responsiva | Vistas `src/Sipitex.Web/Views`, CSS wwwroot | PENDIENTE métrica | PENDIENTE | |
| RNF06 | Código modular | Proyectos `Sipitex.Domain/Application/Infrastructure/Web` | — | — | Seed: Parcial |
| RNF07 | Docker Compose | `Dockerfile`, `docker-compose.yml` | — | — | |
| RNF08 | Integridad / transacciones | EF Core + Npgsql | `SipitexDbContext` | `ConcurrenciaStockYNumeracionTests.cs`, `StockUnicoDescuentoTests.cs` | Seed: Parcial |

### 2.1 Módulos posteriores (fuera de RF01–RF20 original)

Fuente: `docs/01-Requisitos.md` §1.6 + `docs/03-Implementacion.md` + `FuncionalidadesCatalog`.

| Módulo | Controller | Servicio(s) | Tests (ejemplos) |
|--------|------------|-------------|------------------|
| Plantas de inventario | `PlantasInventarioController` | `InventarioPlantaService`, `PlantaInventarioService` | `PlantaInventarioServiceTests.cs`, `PlantasInventarioDetalleTests.cs` |
| Solicitudes de planta | `PlantasInventarioSolicitudesController` | `SolicitudMaterialService` | `PlantaInventarioSolicitudesScopeTests.cs` |
| Órdenes de planta | `PlantasInventarioOrdenesController` | `OrderMaterialService`, `ProductionFlowService` | `OrderMaterialServiceTests.cs`, `StageReentryTests.cs` |
| Grupos de confección | `GruposConfeccionController` | `GrupoConfeccionService` | `GrupoConfeccionServiceTests.cs` |
| Consumos | `ConsumosController` | `MaterialConsumptionService` | `MaterialConsumptionServiceTests.cs` |
| Costeo | `CostosController` | `GarmentCostingService`, `CostingSettingsService` | `GarmentCostingServiceTests.cs`, `CostosControllerTests.cs` |
| Actas | `ActasController` (+ `VerificarController`) | `ActaMovimientoService`; PDF `ActaPdfService` | `ActasControllerTests.cs`, `VerificarActaTests.cs` |
| Trazabilidad | `TrazabilidadController` | `TrazabilidadService` | `TrazabilidadControllerTests.cs`, `TrazabilidadServiceTests.cs` |
| Auditoría | `AuditoriaController` | `ActivityLogService` | `AuditoriaControllerTests.cs`, `ActivityLogServiceTests.cs` |
| Alertas | `AlertasController` | `AlertService` | `AlertStockYSolicitudesTests.cs`, `AlertNotifyUsersTests.cs` |
| Búsqueda | `BuscarController`, `BusquedaController` (`/api/busqueda`) | `BuscadorCatalogo` / `IBusquedaService` | `BuscarControllerTests.cs`, `BuscadorCatalogoTests.cs` |
| Ayuda | `AyudaController` | `AyudaCatalogo` | `AyudaCatalogoTests.cs`, `AyudaBusquedaTests.cs` |
| Privacidad | `PrivacidadController` | `PoliticaPrivacidadDocumento` | `PoliticaPrivacidadHttpTests.cs` |

Catálogo fijo de funcionalidades: `src/Sipitex.Application/Services/FuncionalidadesCatalog.cs`.

---

## 3. Roles y permisos

### 3.1 Roles (`UserRoles`)

Fuente: `src/Sipitex.Domain/Entities/User.cs`

| Constante | Valor persistido / claim |
|-----------|--------------------------|
| `UserRoles.Administrador` | `Administrador` |
| `UserRoles.Instructor` | `Instructor` |
| `UserRoles.EncargadoDeBodega` | `Encargado de bodega` |

`UserRoles.All` = los tres. `UserRoles.CreatableByAdmin` = los tres.

### 3.2 Permisos extendidos (`ExtendedPermissions`)

Fuente: `src/Sipitex.Domain/Entities/ExtendedPermissions.cs`  
Claim type: `"permiso"`.

| Key | Label (UI) |
|-----|------------|
| `Inventario.Registrar` | Registrar materiales en inventario |
| `Solicitudes.Aprobar` | Aprobar / rechazar solicitudes |
| `Mrp.Simular` | Simular MRP |
| `Mrp.GestionarFichas` | Crear / editar ficha técnica (BOM) |
| `Ordenes.Crear` | Crear órdenes de producción |
| `Alertas.Configurar` | Configurar / evaluar alertas |

### 3.3 Reglas (`PermissionRules`) → policies

Fuentes: `src/Sipitex.Application/Authorization/PermissionRules.cs`, `AuthorizationPolicyNames.cs`, `Sipitex.Web/Authorization/SipitexAuthorizationExtensions.cs`

| Método / Policy | Quién pasa |
|-----------------|------------|
| `PuedeRegistrarMateriales` | Admin **o** Encargado de bodega **o** Instructor + `Inventario.Registrar` |
| `PuedeAprobarSolicitudes` | Admin **o** Encargado de bodega **o** Instructor + `Solicitudes.Aprobar` |
| `PuedeSimularMrp` | Admin **o** Encargado de bodega **o** Instructor + `Mrp.Simular` |
| `PuedeGestionarFichasTecnicas` | Admin **o** Encargado de bodega **o** Instructor + `Mrp.GestionarFichas` (Delete BOM: solo Admin en controller) |
| `PuedeCrearOrdenes` | Admin **o** Instructor + `Ordenes.Crear` (Encargado **no**) |
| `PuedeConfigurarAlertas` | Admin **o** claim `Alertas.Configurar` |
| `PuedeConsultarInventario` | Solo Admin **o** Encargado de bodega |

Tests: `tests/Sipitex.Tests/AuthorizationPolicyTests.cs`, `ModuleAccessTests.cs`, `MrpGestionarFichasPermissionTests.cs`, `OrdenesCrearPermissionTests.cs`.

---

## 4. HANDOVER — deuda conocida

Fuente literal: `docs/HANDOVER.md` § «Deuda conocida y trabajo futuro».

### Operativa / producto

| # | Deuda |
|---|--------|
| 1 | MRP no se calcula automáticamente al crear una orden: hay que simular en `/Mrp` e importar requisitos a la OP. |
| 2 | Producto terminado sin pantalla propia en el menú: el stock PT vive en el flujo MES / reingreso; falta un módulo de consulta dedicado. |
| 3 | Cancelar orden o anular acta no revierte stock ni consumos ya aplicados. |
| 4 | Nomenclatura no unificada: «Ficha» (grupo SENA) vs ficha técnica BOM vs ficha del material; «Bodega» (rol) vs «Planta de inventario» (UI); «material» vs «insumo». |
| 5 | `User.FichaAsignadaId` es solo informativo: el alcance real del instructor va por `FichaInstructor` / etapas MES. |
| 6 | Costeo excluye mano de obra del total hasta que el centro defina y active la tarifa en el cálculo (hoy solo materiales; la tarifa de referencia se guarda pero no se suma). |

### Técnica / legado

| # | Deuda |
|---|--------|
| 7 | Código legacy sin uso de producto: `MaterialRequest` (solicitudes 1-ítem), RF/RNF + `RequirementService` (sin pantalla / DI incompleto), búsqueda antigua `/api/busqueda` frente a `Buscar/Sugerencias`. |
| 8 | `BomItem → Material` en Cascade y blockers de borrado de usuario incompletos (no listan consumos/actas/grupos/prendas) — riesgo si se borra fuera del servicio. |
| 9 | Privacidad institucional incompleta (NIT, contacto, proveedores, RNBD, menores). Ver `PENDIENTES-PRIVACIDAD.md`. Vacío o `PENDIENTE` no se publican. |
| 10 | Seed demo: no reactivar `SEED_DEMO_DATA` en el entorno real. Inventario inicial: `GUIA-INVENTARIO-INICIAL.md`. |

También en HANDOVER (operativo, no numerado como deuda de producto): importador `/Inventario/Importar` **sobrescribe stock**; variables `Privacidad__*` pendientes.

---

## 5. Glosario (candidatos)

### 5.1 Desde `AyudaCatalogo.Glosario`

Fuente: `src/Sipitex.Application/Ayuda/AyudaCatalogo.cs` (`ConstruirGlosario`).

| Término | Definición |
|---------|------------|
| Planta de inventario | Lugar donde se guarda el stock. Planta de Inventario 1 es la planta por defecto y no se puede eliminar. |
| Bodega | En SIPITEX es la misma planta de inventario. El menú Plantas de inventario consulta el stock de esas plantas. |
| Orden de producción | Pedido para fabricar una cantidad de un producto que ya tiene ficha técnica, con fecha límite. |
| MRP | Cálculo de materiales necesarios a partir de la ficha técnica y de la cantidad a fabricar. |
| BOM | Receta de la prenda: materiales y cantidades por unidad. Se arma en MRP. No es la ficha técnica del material. |
| Ficha técnica de material | Datos del insumo (composición, gramaje, normas). Se abre con Ficha, en la fila del material. No cambia la receta de la prenda. |
| Stock OK | Hay existencias y alcanzan el mínimo definido para ese material. |
| Stock Bajo | Hay existencias, pero la cantidad es menor que el mínimo. |
| Stock Crítico | El material tiene mínimo definido y la existencia está en cero. |
| Sin mínimo definido | El material no tiene mínimo. No cuenta como falta ni dispara la misma alerta que Bajo o Crítico. |
| Reingreso | Devolución a la planta del material entregado a la orden que no se consumió. También se puede hacer con la orden finalizada o cancelada. El producto terminado vuelve en unidades de la etapa. |
| Acta de ingreso o egreso | Documento de entrada o salida con nombre, cargo, hora UTC y firma dibujada. Se crea con Nueva acta. |
| Solicitud de materiales | Pedido de insumos por descripción, o la entrega de materiales ya ligados a una orden. |

### 5.2 Candidatos adicionales desde comentarios de dominio

| Término | Comentario / ubicación |
|---------|------------------------|
| Ficha (formación) | «Ficha de formación del SENA (grupo/proceso del taller)» — `src/Sipitex.Domain/Entities/Ficha.cs` |
| Ficha técnica (prenda / BOM) | «Cabecera de ficha técnica (producto) — metadatos de la receta BOM» — `BomProduct.cs` |
| Ficha técnica de material | «No es la ficha de la prenda (BomProduct)» — `FichaTecnicaMaterial.cs` |
| ConsumoMaterial | «Ficha de consumo: lo usado en una orden. No descuenta stock.» — `ConsumoMaterial.cs` |
| MaterialRequest | Solicitud 1-ítem legacy a plantaInventario — `MaterialRequest.cs` (HANDOVER: sin uso de producto) |
| FinishedGoodStock | «Inventario de producto terminado (aparte de materiales/insumos)» — `FinishedGoodStock.cs` |
| BOM / Bill of Materials | También en `docs/08-IEEE830-Especificacion.md` §1.3 |
| MRP | Idem IEEE830 §1.3 |
| SRS | Especificación de requisitos de software — IEEE830 §1.3 |

---

## 6. Controllers — rutas/actions y Authorize

Convención de ruta MVC: `/{Controller}/{Action}` salvo `BusquedaController` (`[Route("api/busqueda")]`).  
`hereda` = atributo de clase. Valores `UserRoles.*` y `AuthorizationPolicyNames.*` como en código.

| Controller | Archivo | Authorize clase | Action | HTTP | Authorize action |
|------------|---------|-----------------|--------|------|------------------|
| AccountController | `src/Sipitex.Web/Controllers/AccountController.cs` | `[Authorize]` | Login | GET, POST | AllowAnonymous |
| | | | ForgotPassword | GET, POST | AllowAnonymous |
| | | | ForgotPasswordConfirmation | GET | AllowAnonymous |
| | | | ResetPassword | GET, POST | AllowAnonymous |
| | | | ConfirmEmail | GET, POST | AllowAnonymous |
| | | | ResendEmailConfirmation | POST | AllowAnonymous |
| | | | Logout | POST | hereda |
| | | | KeepAlive | POST | hereda |
| | | | Profile | GET, POST | hereda |
| | | | Users | GET | Roles=Administrador |
| | | | CreateUser | GET, POST | Roles=Administrador |
| | | | EditUser | GET, POST | Roles=Administrador |
| | | | ToggleUserStatus | POST | Roles=Administrador |
| | | | DeleteUser | POST | Roles=Administrador |
| | | | DescargarFuncionalidades | GET | Roles=Administrador |
| | | | AccessDenied | GET | AllowAnonymous |
| | | | Photo | GET | AllowAnonymous |
| ActasController | `.../ActasController.cs` | Roles=Admin,Instructor,Encargado | Index, Create, Details, Firmar, Pdf, EnviarCorreo | GET/POST según action | hereda |
| | | | Anular | POST | Roles=Administrador |
| AlertasController | `.../AlertasController.cs` | `[Authorize]` | Index, SavePreferences, Probar | GET/POST | hereda |
| | | | Evaluar | POST | Policy=PuedeConfigurarAlertas |
| AuditoriaController | `.../AuditoriaController.cs` | Roles=Administrador | Index | GET | hereda |
| AyudaController | `.../AyudaController.cs` | `[Authorize]` | Index | — (sin `[Http*]`) | hereda |
| | | | Buscar | GET | hereda |
| BuscarController | `.../BuscarController.cs` | `[Authorize]` | Sugerencias | GET | hereda |
| BusquedaController | `.../BusquedaController.cs` | `[Authorize]` + Route `api/busqueda` | Get | GET `/api/busqueda` | hereda |
| CalidadController | `.../CalidadController.cs` | `[Authorize]` | Index | GET | Roles=Admin,Instructor |
| | | | Create | POST | Roles=Admin,Instructor |
| ConsumosController | `.../ConsumosController.cs` | Roles=Admin,Instructor | Index, Create | GET, POST | hereda |
| CostosController | `.../CostosController.cs` | Roles=Admin,Instructor | Index | GET | hereda |
| | | | Tarifa | POST | Roles=Administrador |
| EstadisticasController | `.../EstadisticasController.cs` | `[Authorize]` | Index | GET | hereda |
| FichasController | `.../FichasController.cs` | `[Authorize]` | Index, CreateFicha, AssignInstructor, RemoveInstructor, UpdateInstructorProceso, Register, QuickRegister | GET/POST | Roles=Admin,Instructor |
| GruposConfeccionController | `.../GruposConfeccionController.cs` | Roles=Admin,Instructor | Index, Create, Cerrar | GET/POST | hereda |
| HomeController | `.../HomeController.cs` | — | Index | — | `[Authorize]` |
| | | | Privacy | — | AllowAnonymous (redirige a Privacidad) |
| | | | Error | — | AllowAnonymous |
| InventarioController | `.../InventarioController.cs` | `[Authorize]` | Importar, DescargarPlantilla, Confirmar | GET/POST | Roles=Administrador |
| | | | Index | GET | hereda (redirect permanente a PlantasInventario) |
| | | | Movimientos | GET | Roles=Admin,Encargado |
| MrpController | `.../MrpController.cs` | `[Authorize]` | Index, Materials, MaterialsByCodigo | GET | Roles=Admin,Encargado,Instructor |
| | | | Simulate | POST | Policy=PuedeSimularMrp |
| | | | Create, Edit | GET, POST | Policy=PuedeGestionarFichasTecnicas |
| | | | AssignInstructor, RemoveInstructor, Delete | POST | Roles=Administrador |
| OrdenesController | `.../OrdenesController.cs` | `[Authorize]` | Index, Detail | GET | Roles=Admin,Encargado,Instructor |
| | | | Edit, Approve, Cancel, AddStage, RemoveStage, MoveStage, AssignInstructor, SetStagePermission | GET/POST | Roles=Administrador |
| | | | Create | POST | Policy=PuedeCrearOrdenes |
| | | | AddProduction, AddMaterial, RemoveMaterial, ImportBomMaterials, StartStage, PauseStage, ResumeStage, CompleteStage, ProcessUnits, SendToNext, PartialInventoryIn, PartialWithdraw | POST | Roles=Admin,Instructor |
| PlantasInventarioController | `.../PlantasInventarioController.cs` | `[Authorize]` | Index, Consultar | GET | Policy=PuedeConsultarInventario |
| | | | Detalle, Movimientos, Material, GuardarFicha, AdjustStock, UpdateStatus | GET/POST | Roles=Admin,Encargado |
| | | | AddMaterial | POST | Policy=PuedeRegistrarMateriales |
| | | | EditMaterial, DeleteMaterial, Create, Edit, Delete, Reactivar | GET/POST | Roles=Administrador |
| PlantasInventarioOrdenesController | `.../PlantasInventarioOrdenesController.cs` | Roles=Admin,Instructor,Encargado | Index, Detail | GET | hereda |
| | | | Reingreso, ValidateStock, Deliver | GET/POST | Roles=Admin,Encargado |
| PlantasInventarioSolicitudesController | `.../PlantasInventarioSolicitudesController.cs` | Roles=Admin,Instructor,Encargado | Index, Detail | GET | hereda |
| | | | Resolve | POST | Policy=PuedeAprobarSolicitudes |
| PrivacidadController | `.../PrivacidadController.cs` | — | Index | GET | AllowAnonymous |
| | | | Descargar, Aceptar, Solicitar | GET/POST | `[Authorize]` |
| ReportesController | `.../ReportesController.cs` | `[Authorize]` | Index, Inventario, Ordenes, Calidad, Dashboard, ActividadInstructor, Actividad, ActividadPdf, ActividadExcel | GET | hereda |
| SolicitudesMaterialController | `.../SolicitudesMaterialController.cs` | Roles=Admin,Instructor | Index, Detail, SolicitarInsumos, Create | GET/POST | hereda |
| TrazabilidadController | `.../TrazabilidadController.cs` | Roles=Admin,Instructor,Encargado | Index, Generar, Details | GET/POST | hereda |
| VerificarController | `.../VerificarController.cs` | `[AllowAnonymous]` | Acta | GET | hereda (+ rate limit `verificar-acta`) |

---

## 7. Paquetes NuGet (todos los `.csproj`)

### `src/Sipitex.Domain/Sipitex.Domain.csproj`

| Package | Version |
|---------|---------|
| *(ninguno)* | — |
| TargetFramework | `net10.0` |

### `src/Sipitex.Application/Sipitex.Application.csproj`

| Package | Version |
|---------|---------|
| Microsoft.Extensions.Logging.Abstractions | 10.0.0 |
| Microsoft.Extensions.Options | 10.0.0 |
| TargetFramework | `net10.0` |

### `src/Sipitex.Infrastructure/Sipitex.Infrastructure.csproj`

| Package | Version |
|---------|---------|
| ClosedXML | 0.105.0 |
| DocumentFormat.OpenXml | 3.3.0 |
| MailKit | 4.14.1 |
| Microsoft.Extensions.Http | 10.0.9 |
| Microsoft.AspNetCore.DataProtection.EntityFrameworkCore | 10.0.9 |
| Microsoft.EntityFrameworkCore | 10.0.9 |
| Microsoft.EntityFrameworkCore.Relational | 10.0.9 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.0 |
| Microsoft.Extensions.Options.ConfigurationExtensions | 10.0.9 |
| QRCoder | 1.6.0 |
| QuestPDF | 2025.7.1 |
| TargetFramework | `net10.0` |

### `src/Sipitex.Web/Sipitex.Web.csproj`

| Package | Version |
|---------|---------|
| Microsoft.AspNetCore.DataProtection.EntityFrameworkCore | 10.0.9 |
| Microsoft.EntityFrameworkCore.Design | 10.0.9 |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 10.0.9 |
| Serilog.AspNetCore | 10.0.0 |
| Serilog.Formatting.Compact | 3.0.0 |
| Serilog.Sinks.Console | 6.1.1 |
| TargetFramework | `net10.0` |

### `tests/Sipitex.Tests/Sipitex.Tests.csproj`

| Package | Version |
|---------|---------|
| coverlet.collector | 6.0.4 |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.0 |
| Npgsql | 10.0.0 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.0 |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.9 |
| Microsoft.NET.Test.Sdk | 17.14.1 |
| Moq | 4.20.72 |
| Serilog | 4.3.0 |
| xunit | 2.9.3 |
| xunit.runner.visualstudio | 3.1.4 |
| TargetFramework | `net10.0` |

### `tools/Sipitex.DataMigration/Sipitex.DataMigration.csproj`

| Package | Version |
|---------|---------|
| Microsoft.Data.Sqlite | 10.0.9 |
| Npgsql | 10.0.0 |
| TargetFramework | `net10.0` |

---

## 8. npm / `package.json`

| Ubicación | Package | Rango (`package.json`) | Versión lock |
|-----------|---------|------------------------|--------------|
| `docs/diagramas/package.json` | `@mermaid-js/mermaid-cli` (devDependency) | `^11.16.0` | `11.16.0` (`docs/diagramas/package-lock.json`) |

No hay `package.json` en la raíz de la aplicación ni en `src/`.

---

## 9. Ítems PENDIENTE (no hallados / no medidos en repo)

| Ítem | Motivo |
|------|--------|
| Matriz RF→código única y oficial | Existen **dos** numeraciones RF (`01`/`DbInitializer` vs `08-IEEE830`) sin archivo de equivalencia |
| Evidencia de RNF01 (<2 s) | Sin benchmark/prueba de rendimiento en el repo |
| RNF02 = 8 horas | Docs/seed lo afirman; código default = 20 minutos (`SeguridadOptions`) |
| Prueba automatizada de RNF05 (responsive) | No localizada |
| Módulo UI de producto terminado | Confirmado como deuda en HANDOVER (#2) |
| Criterios de calidad por tipo de prenda (RF16 seed) | Status seed **Ausente** |
| JWT (RF02/RNF02 en `01-Requisitos`) | Superado por cookies en implementación e IEEE830 |
