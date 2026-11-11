# Auditoría de cierre: Roles y funciones vs código SIPITEX

| Campo | Valor |
|-------|--------|
| **Proyecto** | SIPITEX (ASP.NET Core MVC + EF Core + SQLite) |
| **Alcance** | Solo lectura — sin cambios de código de aplicación |
| **Referencia original** | `AUDITORIA_ROLES_FUNCIONES.md` (PR #46 / 15 gaps críticos) |
| **Código auditado** | Branch `main` @ `2af46a2` (incluye merge de PR #57) |
| **Fecha de verificación** | 2026-08-12 |
| **Suite de tests** | **187 passed / 0 failed / 0 skipped** (`dotnet test tests/Sipitex.Tests`) |
| **Clasificación** | ✅ Resuelto · ⚠️ Parcialmente resuelto · ❌ Sigue sin resolver |

**Nota:** La verificación se basó en el código fusionado en `main`, no en las descripciones de los PRs. **PR #47** (`cursor/instructor-scope-inventario-ordenes-171f`) permanece **OPEN / no fusionado**; por eso los gaps #8 y #12 no están cerrados en `main` aunque exista trabajo en esa rama.

---

## Resumen numérico

| Estado actual | Cantidad |
|---------------|----------|
| ✅ Resueltos | **10** de 15 |
| ⚠️ Parcialmente resueltos | **3** de 15 |
| ❌ Abiertos | **2** de 15 |

---

## 1. Tabla de gaps (1–15)

| Gap # | Descripción breve | Estado original (#46) | Estado actual | PR que lo tocó | Evidencia (archivo:línea) | Pendiente (si aplica) |
|-------|-------------------|----------------------|---------------|----------------|---------------------------|------------------------|
| 1 | Crear/editar/eliminar cuentas de los tres roles (incl. Admin + hard-delete) | ⚠️ | ✅ Resuelto | **#51** | `UserRoles.CreatableByAdmin` incluye Administrador — `User.cs:57–62`; `AccountController.CreateUser` / `DeleteUser` — `AccountController.cs:249–354`; `UserAccountService.DeleteUserAsync` (hard remove) — `UserAccountService.cs:155–189` | — |
| 2 | CRUD completo de órdenes (crear, editar, **aprobar**, cancelar/eliminar) | ⚠️ | ⚠️ Parcial | **#50** | Editar: `OrdenesController.Edit` — `OrdenesController.cs:75–123` + `ProductionOrderService.UpdateOrderAsync` — `ProductionOrderService.cs:190–276`. Cancelar: `OrdenesController.Cancel` — `OrdenesController.cs:125–141` + `CancelOrderAsync` — `ProductionOrderService.cs:278–316` + `OrderChangeLog`. Crear: policy `PuedeCrearOrdenes` — `OrdenesController.cs:143`. | **Falta acción explícita de “aprobar orden”.** El catálogo documenta aprobación implícita al crear (`EnProceso`) — `FuncionalidadesCatalog.cs:44–46`. No hay endpoint `ApproveOrder` ni transición desde `Pendiente`. Enum solo: Pendiente/EnProceso/Finalizada/Cancelada. |
| 3 | Inventario Admin: editar metadatos de material + tipificar entradas | ⚠️ | ✅ Resuelto | **#52** | `InventarioController.EditMaterial` — `InventarioController.cs:91–103`; `InventoryService.UpdateMaterialAsync` — `InventoryService.cs:138–161`; `StockEntryOrigin` (Compra/Devolucion/OtraFuenteAutorizada) — `StockEntryOrigin.cs`; origen en alta/ajuste — `InventoryService.cs:56–57`, `111–112`; UI — `Views/Inventario/Index.cshtml:57–59`, `140–144` | — |
| 4 | Asignar ficha técnica (BOM) a uno o varios instructores | ❌ | ✅ Resuelto | **#53** | Entidad `BomProductInstructor` — `BomProductInstructor.cs`; M2M en `SipitexDbContext`; `BomCatalogService.AssignInstructorAsync` / `RemoveInstructorAsync` — `BomCatalogService.cs:165–209`; UI Admin — `MrpController.AssignInstructor` / `RemoveInstructor` — `MrpController.cs:55–77` | — |
| 5 | Historial/auditoría global de actividades de todos los usuarios | ❌ | ❌ Abierto | — | Búsqueda sin `ActivityLog` / `AuditLog` / registro append-only transversal de acciones de usuario en `src/`. Existen solo historiales de dominio: `OrderChangeLog`, `ProductionOrderHistoryEntry`, `StockMovement`. | Falta entidad + escritura en puntos de mutación + UI/consulta de auditoría transversal (quién hizo qué, cuándo), distinto del historial MES/bodega. |
| 6 | Instructor gestiona fichas técnicas solo si Admin autorizó | ⚠️ | ✅ Resuelto | **#54** | `ExtendedPermissions.MrpGestionarFichas` — `ExtendedPermissions.cs:13`; `PermissionRules.PuedeGestionarFichasTecnicas` — `PermissionRules.cs:28–33`; Create/Edit con policy — `MrpController.cs:79–132`; Delete permanece Admin — `MrpController.cs:134–144` | — |
| 7 | Instructor crea órdenes solo con permiso extendido | ❌ | ✅ Resuelto | **#55** | `ExtendedPermissions.OrdenesCrear` — `ExtendedPermissions.cs:14`; `PermissionRules.PuedeCrearOrdenes` — `PermissionRules.cs:35–38`; `OrdenesController.Create` `[Authorize(Policy = PuedeCrearOrdenes)]` — `OrdenesController.cs:143–168`; instructor creador se asigna a etapas — `OrdenesController.cs:148–154` | — |
| 8 | Instructor gestiona únicamente órdenes asignadas (listado/detalle/avance) | ⚠️ | ⚠️ Parcial | **#56** (servicio/calidad); **#47 OPEN** (Ordenes UI) | Capacidad en servicio: `GetOrdersAsync` filtra si se pasan viewer* — `ProductionOrderService.cs:50–63`; `CanAccessOrderAsync` — `ProductionOrderService.cs:105–122`; gate de etapas — `ProductionFlowService.EnsureCanActOnStageAsync`. Calidad sí pasa viewer — `CalidadController.cs:32`. | **`OrdenesController` no cablea el alcance:** `Index` llama `GetOrdersAsync` **sin** viewer — `OrdenesController.cs:45` → Instructor ve **todas**. `Detail` / `GetMesDetailAsync` no llama `CanAccessOrderAsync` — `OrdenesController.cs:56–72`, `ProductionFlowService.cs:130–194` (devuelve detalle de cualquier orden). `AddProduction` no valida asignación — `OrdenesController.cs:170–178`. PR #47 no fusionado. |
| 9 | Instructor ve solo solicitudes de materiales de su alcance | ⚠️ | ⚠️ Parcial | (parcial en flujo ficha; sin PR dedicado de cierre) | Flujo `SolicitudMaterial` scoped: `GetListAsync` filtra por `SolicitanteId` — `SolicitudMaterialService.cs:115–116`; detalle — `GetDetailAsync` L133–134. | **Legacy `MaterialRequest` en Inventario sigue global:** `InventoryService.GetRequestsAsync` sin filtro — `InventoryService.cs:177–187`; `InventarioController.BuildViewModel` carga todas — `InventarioController.cs:223`; combo de órdenes también sin viewer — L215. Instructor con acceso a Inventario (gap #12) ve solicitudes de todos. |
| 10 | Calidad solo sobre órdenes asignadas | ⚠️ | ✅ Resuelto | **#56** | `QualityService.GetRecordsAsync` filtra con `CanAccessOrderAsync` — `QualityService.cs:42–46`; `AddRecordAsync` — L69–71; `CalidadController.Create` → `Forbid()` — `CalidadController.cs:50–51`; Index pasa viewer a órdenes/registros — L30–35 | — |
| 11 | Reportes limitados al alcance del Instructor | ⚠️ | ✅ Resuelto | **#57** | `ReportesController.ResolveFilter` fuerza `instructorId = self` — `ReportesController.cs:134–146`; `IsInstructorOnly` — L165–166; Inventario reporte → `Forbid()` — L64–65; UI oculta tarjeta Inventario; `ReportService.ResolveOrderScopeAsync` une fichas + etapas MES | — |
| 12 | Instructor sin acceso a inventario general | ❌ | ❌ Abierto | **#47 OPEN** (no en `main`) | `InventarioController` clase `[Authorize]` sin policy de inventario — `InventarioController.cs:14–15`; `Index` abierto a cualquier autenticado — L37–42; menú Inventario visible a todos — `_Layout.cshtml:49`. **No existe** `PuedeAccederInventario` en `PermissionRules` / policies de `main`. | Bloquear consulta general de stock/solicitudes al Instructor (salvo flujo autorizado explícito). Fusionar/adaptar PR #47 o equivalente: policy + Forbid + ocultar menú/home. Nota: el **reporte** Inventario sí está Forbid (gap #11), pero el módulo Inventario no. |
| 13 | Bodeguero: entradas tipificadas (compra/devolución/otra) | ⚠️ | ✅ Resuelto | **#52** | Mismo mecanismo que gap #3: `StockEntryOrigin` obligatorio en alta y en ajuste al alza; formularios Bodeguero/Admin en Inventario | — |
| 14 | Bodeguero: reingreso desde etapas de producción | ❌ | ✅ Resuelto | **#49** | `BodegaOrdenesController.Reingreso` GET/POST — `BodegaOrdenesController.cs:60–97`; `ProductionFlowService.RegisterStageReentryAsync` — `ProductionFlowService.cs:459+`; menú — `_Layout.cshtml:57–58`; vista `Views/BodegaOrdenes/Reingreso.cshtml` | — |
| 15 | Historial de movimientos de bodega (ledger) | ❌ | ✅ Resuelto | **#48** | Entidad `StockMovement` — `StockMovement.cs`; registro en alta/ajuste/aprobación/entrega/reingreso; `InventarioController.Movimientos` Admin/Bodeguero — `InventarioController.cs:44–63`; `StockMovementService.GetHistoryAsync`; UI `Views/Inventario/Movimientos.cshtml` | — |

### Mapa PR → gaps (fusionados en `main` tras #46)

| PR | Gaps | Estado en GitHub |
|----|------|------------------|
| #47 | #8 (Ordenes UI), #9 (Inventario legacy, colateral), #12 | **OPEN — no en `main`** |
| #48 | #15 | Merged |
| #49 | #14 | Merged |
| #50 | #2 (parcial: editar/cancelar; aprobar implícito) | Merged |
| #51 | #1 | Merged |
| #52 | #3, #13 | Merged |
| #53 | #4 | Merged |
| #54 | #6 | Merged |
| #55 | #7 | Merged |
| #56 | #10 (+ API `CanAccessOrderAsync` / filtro en servicio de órdenes) | Merged |
| #57 | #11 | Merged |

---

## 2. Regresiones detectadas

**No se detectaron regresiones** sobre funciones que en la auditoría #46 estaban marcadas ✅ y que el manual exige intactas.

Comprobaciones puntuales en `main`:

| Función (✅ original) | Verificación actual |
|----------------------|---------------------|
| Admin: activar/desactivar cuentas | `AccountController.ToggleUserStatus` + `ToggleUserStatusAsync` siguen presentes |
| Admin: asignar/revocar permisos extendidos | `ExtendedPermissions` ampliado (nuevas claves) sin quitar las existentes; Create/Edit User Admin-only |
| Admin: instructor elevado no gestiona cuentas Admin | Users/Create/Edit/Toggle/Delete siguen `[Authorize(Roles = Administrador)]` |
| Admin: asignar instructores a etapas MES | `OrdenesController.AssignInstructor` / `SetStagePermission` intactos |
| Admin: consultar todas las órdenes | `OrdenesController.Index` llama `GetOrdersAsync` sin viewer → lista completa (correcto para Admin) |
| Admin: CRUD fichas técnicas BOM | `MrpController` Create/Edit/Delete; Delete sigue Admin |
| Admin: reportes generales | `ReportesController` sin forzar filtro para Admin/Bodeguero |
| Instructor: sin autoregistro; perfil/contraseña/foto | Sin endpoint Register; `Profile` autenticado |
| Instructor: consultar materiales por ficha técnica | `MrpController.Index` accesible |
| Bodeguero: salidas / cola / validar stock / aprobar-rechazar / entrega parcial / stock en tiempo real / perfil | `BodegaOrdenesController.Deliver` / `ValidateStock`; `BodegaSolicitudesController.Resolve`; `OrderMaterialService.DeliverAsync` + `EntregaParcial`; `AccountController.Profile` |

**Notas (no son regresiones de ✅):**

- El reporte de Inventario quedó **Forbid** para Instructor (gap #11 / PR #57). Eso no rompe un ✅ del Instructor (los reportes estaban ⚠️).
- PR #47 no fusionado: el Instructor **sigue** entrando a Inventario (comportamiento ya ❌ en #46), no una regresión nueva.

---

## 3. Gaps aún abiertos

Listos para prompts de implementación (mismo nivel de detalle que la auditoría original).

### 3.1 ❌ Gap #5 — Historial/auditoría de actividades de todos los usuarios

**Estado original:** ❌ No implementado.  
**Estado actual:** ❌ Sigue sin resolver.

**Qué pide el manual:** registro de actividades de todos los usuarios (auditoría transversal).

**Qué hay hoy:** historiales de dominio únicamente (`OrderChangeLog` por orden, `ProductionOrderHistoryEntry` MES, `StockMovement` de bodega). No hay entidad/controlador de ActivityLog/AuditLog global.

**Falta:**

1. Modelo append-only (p. ej. actor, rol, acción, entidad afectada, timestamp, detalle).
2. Escritura en mutaciones relevantes (cuentas, órdenes, inventario, calidad, solicitudes, etc.) o middleware/filtro equivalente.
3. UI de consulta (Admin) con filtros por usuario/fecha/módulo.
4. Tests de que las acciones críticas dejan rastro y que no-Admin no altera el log.

**No confundir** con historial MES ni con movimientos de stock.

---

### 3.2 ❌ Gap #12 — Instructor sin acceso a inventario general

**Estado original:** ❌ No implementado.  
**Estado actual:** ❌ Sigue sin resolver en `main` (trabajo en PR #47 OPEN).

**Qué pide el manual:** Instructor sin acceso a inventario general, salvo flujo de producción autorizado.

**Evidencia actual:**

- `InventarioController` `[Authorize]` sin restricción de rol/policy — `InventarioController.cs:14–42`.
- Sidebar muestra Inventario a todos — `_Layout.cshtml:49`.
- No existe `PuedeAccederInventario` en `main`.

**Falta:**

1. Policy (Admin, Bodeguero, y opcionalmente Instructor solo con claim explícito tipo `Inventario.Registrar` si el producto lo permite) aplicada a `InventarioController` (al menos Index/consulta).
2. Ocultar enlace en layout/home/búsqueda para Instructor sin permiso.
3. Asegurar que flujos autorizados (p. ej. materiales en detalle de orden, solicitudes de ficha) no dependan de la pantalla general de Inventario.
4. Tests: Instructor sin permiso → Forbid/AccessDenied; Admin/Bodeguero OK.

**Relacionado:** al cerrar #12 se facilita cerrar el remanente de #9 (solicitudes legacy en Inventario).

---

### 3.3 ⚠️ Gap #8 — Gestionar únicamente las órdenes asignadas

**Estado original:** ⚠️ Parcial (gate de etapas sí; listado no).  
**Estado actual:** ⚠️ Parcial (servicio listo; UI Órdenes no cableada).

**Qué ya está:**

- `ProductionOrderService.GetOrdersAsync(viewer*)` filtra por asignación (etapa MES `InstructorUserId` o ficha `BelongsToInstructor`) — `ProductionOrderService.cs:50–63`, `368–392`.
- `CanAccessOrderAsync` — L105–122.
- Gate de actuación en etapas — `EnsureCanActOnStageAsync`.
- Calidad ya usa el alcance (gap #10 / PR #56).

**Qué falta (preciso):**

1. `OrdenesController.Index` debe pasar `(userId, role, name)` a `GetOrdersAsync` — hoy L45 sin viewer → Instructor ve todas.
2. `OrdenesController.Detail` (y cualquier acción de avance/materiales sobre la orden) debe `Forbid()` si `!CanAccessOrderAsync` — hoy L56–72 no valida.
3. `AddProduction` debe validar asignación antes de `RegisterProductionAsync` — hoy L170–178.
4. Revisar otros callers que listan órdenes para Instructor sin viewer (`InventarioController.BuildViewModel` L215, `FichasController` si aplica).
5. Tests de UI/controller: Instructor no lista/abre/avanza orden ajena; Admin/Bodeguero sin cambio.

**Nota:** gran parte está en PR #47 OPEN; también se puede cablear solo la parte de Órdenes sin esperar inventario.

---

### 3.4 ⚠️ Gap #9 — Ver estado de solicitudes solo de su alcance

**Estado original:** ⚠️ Parcial.  
**Estado actual:** ⚠️ Parcial.

**Qué ya está:** flujo `SolicitudesMaterial` / `SolicitudMaterialService` limita Instructor a `SolicitanteId` — `SolicitudMaterialService.cs:107–137`.

**Qué falta:**

1. Acotar `InventoryService.GetRequestsAsync` (o su uso desde Inventario) al Instructor (por solicitante y/o por órdenes asignadas).
2. En `InventarioController.BuildViewModel`, no cargar solicitudes/órdenes globales para Instructor — `InventarioController.cs:210–223`.
3. Tests: Instructor en Inventario (mientras exista acceso) no ve `MaterialRequest` ajenas.

**Dependencia recomendada:** cerrar gap #12 (bloquear Inventario) reduce la superficie; si Inventario queda Forbid al Instructor, el listado legacy deja de ser vector — igual conviene scopear el servicio por defensa en profundidad.

---

### 3.5 ⚠️ Gap #2 (remanente) — Aprobar orden de producción

**Estado original:** ⚠️ (faltaban editar, aprobar, cancelar).  
**Estado actual:** ⚠️ (editar + cancelar ✅; aprobar ❌ como acción explícita).

**Qué ya está:** crear, editar con `OrderChangeLog`, cancelar soft a `Cancelada`.

**Qué falta respecto al manual:**

1. Definir si “aprobar” es acción de negocio real (p. ej. `Pendiente` → `EnProceso`) o se confirma el diseño actual de **aprobación implícita al crear**.
2. Si el manual exige acción explícita: endpoint Admin (y policy), transición de estado, registro en `OrderChangeLog`, UI en detalle/listado, tests.

Hasta que producto confirme, el gap **no** puede marcarse ✅ frente al texto del manual (“crear, editar, aprobar, eliminar”).

---

## 4. Suite de tests

| Métrica | Valor |
|---------|--------|
| Comando | `dotnet test tests/Sipitex.Tests/Sipitex.Tests.csproj` |
| Branch | `main` @ `2af46a2` |
| Passed | **187** |
| Failed | **0** |
| Skipped | **0** |
| Resultado | Verde |

La suite verde valida regresiones automatizadas de los gaps ya implementados; **no** sustituye los gaps abiertos (#5, #12) ni el cableado pendiente de #8/#9 en controladores.

---

## 5. Conclusión

De los **15 gaps críticos** de la auditoría #46, en `main` hoy hay **10 resueltos**, **3 parciales** (#2 aprobar; #8 órdenes UI; #9 solicitudes legacy) y **2 abiertos** (#5 ActivityLog global; #12 inventario Instructor). El bloqueador estructural más visible es **PR #47 sin fusionar**, que deja abiertos #12 y el cableado de #8 (y agrava #9 vía Inventario).

*Fin del reporte de cierre. Sin propuestas de implementación de código en este paso — solo el inventario de pendientes listo para prompts.*
