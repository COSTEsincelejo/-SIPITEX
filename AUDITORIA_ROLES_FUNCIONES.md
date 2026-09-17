# Auditoría: Roles y funciones vs código SIPITEX

| Campo | Valor |
|-------|--------|
| **Proyecto** | SIPITEX (ASP.NET Core MVC + EF Core + SQLite) |
| **Alcance** | Solo lectura — sin cambios de código de aplicación |
| **Referencia** | Checklist de `SIPITEX_Manual_Roles_y_Funciones` (tarea de auditoría) |
| **Código auditado** | Branch `main` (HEAD al momento de la revisión) |
| **Clasificación** | ✅ Implementado · ⚠️ Parcial · ❌ No implementado |

**Nota de nomenclatura:** en el código, **ficha técnica** = producto BOM (`BomProduct` / MRP). **Ficha** = ficha de formación SENA (`Ficha` / `FichasController`). Se distingue en las notas cuando aplica.

---

## 1. Resumen por rol

### 1.1 Administrador

| Función | Estado | Evidencia (archivo:método) | Notas |
|---------|--------|----------------------------|-------|
| Crear/editar/eliminar cuentas de Administrador, Instructor, Bodeguero | ⚠️ Parcial | `AccountController.CreateUser` L249–278; `EditUser` L281–323; `UserAccountService.CreateUserAsync` L49–84 / `UpdateUserAsync` L87–140; `UserRoles.CreatableByAdmin` L57–61 | Crear/editar Instructor y Bodeguero: sí. **No** se pueden crear cuentas de Administrador desde UI (`CreatableByAdmin` solo Instructor/Bodeguero; seed en `DbInitializer`). **No** hay eliminar (hard delete); solo desactivar. Edición de Admin existente: rol bloqueado. |
| Activar/desactivar cuentas | ✅ Implementado | `AccountController.ToggleUserStatus` L326–334; `UserAccountService.ToggleUserStatusAsync` L143–153; vista `Views/Account/Users.cshtml` | Flag `User.IsActive`; login rechaza inactivos en `AuthenticateAsync` L34. |
| Asignar y revocar permisos a instructores | ✅ Implementado | `AccountController.CreateUser/EditUser` (SelectedPermissions); `ExtendedPermissions` L10–31; `UserAccountService` serializa en `PermisosExtendidos`; claims en `SignInUserAsync` L373–375 | Catálogo: Inventario.Registrar, Solicitudes.Aprobar, Mrp.Simular, Alertas.Configurar. |
| Restricción: instructor con permisos admin NO puede gestionar cuentas de Administrador | ✅ Implementado | `AccountController` Users/Create/Edit/Toggle: `[Authorize(Roles = UserRoles.Administrador)]` L238–337 | Gestión de usuarios exclusiva del rol Administrador. Permisos extendidos no otorgan CRUD de cuentas; instructor elevado no llega a Usuarios. |
| CRUD completo de órdenes de producción (crear, editar, aprobar, eliminar) | ⚠️ Parcial | Crear: `OrdenesController.Create` L73–85 + `ProductionOrderService.CreateOrderAsync` L87–136. Listar/avance: `Index`/`AddProduction`. | **Crear** solo Admin. **No** hay editar orden, aprobar orden ni eliminar/cancelar (enum `OrderStatus.Cancelada` existe pero sin acción de servicio/controlador). |
| Asignar instructores responsables a etapas de producción | ✅ Implementado | `OrdenesController.AssignInstructor` L198–205; `ProductionFlowService.AssignInstructorAsync` (~L314); `SetStagePermission` L244–250 | Etapas default: Trazo, Corte, Confección, Control de Calidad, Terminado (`ProductionFlowService.DefaultStageNames` L14–15). |
| Consultar estado/avance de todas las órdenes | ✅ Implementado | `OrdenesController.Index` L38–49; `ProductionOrderService.GetOrdersAsync` L44–85; detalle MES `OrdenesController.Detail` L53–71 | Admin ve todas; incluye % avance, estado materiales y etapa actual. |
| Inventario: consultar, entradas/salidas, CRUD productos, ajustar existencias | ⚠️ Parcial | Consulta/alta/ajuste/estado/borrar: `InventarioController` L30–146; `InventoryService` L35–190 | Consultar, registrar material, ajustar stock, cambiar estado, eliminar material: sí. Salidas vía aprobación de solicitudes. **Falta** edición completa de producto (nombre/unidad) y tipificación de movimiento (compra/devolución/salida). |
| CRUD de fichas técnicas | ✅ Implementado | `MrpController` Create/Edit/Delete L62–126; `BomCatalogService` Create/Update/Delete | Admin: CRUD completo. Bodeguero puede crear/editar (no eliminar). |
| Asignar ficha técnica a uno o varios instructores | ❌ No implementado | `BomProduct` sin relación a instructores; no encontrado en controllers/servicios | Asignación M2M existe para **fichas de formación** (`FichasController.AssignInstructor` L76–85 / `FichaInstructor`), no para fichas técnicas BOM. |
| Reportes generales (producción, inventario, productividad) | ✅ Implementado | `ReportesController` Inventario/Ordenes/Calidad/Dashboard/ActividadInstructor L39–107; `EstadisticasController.Index` L18–26 | PDF/Excel + KPIs. Productividad vía Dashboard / actividad instructor. |
| Historial/registro de actividades de todos los usuarios (auditoría) | ❌ No implementado | no encontrado (sin entidad/controlador de ActivityLog/AuditLog global) | Hay historial **por orden** MES (`ProductionOrderHistoryEntry` / `GetMesDetailAsync`) y de alertas; no auditoría transversal de acciones de usuarios. |

### 1.2 Instructor

| Función | Estado | Evidencia (archivo:método) | Notas |
|---------|--------|----------------------------|-------|
| No puede autoregistrarse (cuenta la crea el Admin) | ✅ Implementado | Login `[AllowAnonymous]` sin Register; `CreateUser` solo Admin L249–260; `CreateUser.cshtml` subtítulo L13 | No hay endpoint de registro público. |
| Puede cambiar su contraseña y foto de perfil | ✅ Implementado | `AccountController.Profile` GET/POST L158–235; `UpdateProfileAsync` L156–205; `SaveProfilePhotoAsync` L396–423 | También forgot/reset password anónimo. |
| Crear/gestionar fichas técnicas SOLO si el Admin lo autorizó | ⚠️ Parcial | Consulta: `MrpController.Index` L27–44 (rol Instructor). CRUD: Create/Edit L62–115 solo Admin+Bodeguero | Instructor **consulta** BOM. **No** existe permiso extendido ni gate para autorizar CRUD de ficha técnica al instructor. |
| Crear órdenes de producción SOLO si tiene el permiso | ❌ No implementado | `OrdenesController.Create` L73 `[Authorize(Roles = Administrador)]`; `ExtendedPermissions` sin clave de crear órdenes | Instructor no puede crear órdenes ni con permiso extendido. |
| Gestionar únicamente las órdenes que tiene asignadas | ⚠️ Parcial | Gate etapas: `ProductionFlowService.EnsureCanActOnStageAsync` L608–624. Listado: `GetOrdersAsync` sin filtro por instructor | Operar etapas exige asignación o `InstructorStagePermission`. **Pero** Index/Detail listan **todas** las órdenes; `AddProduction` no valida asignación. |
| Consultar materiales requeridos por ficha técnica/producto | ✅ Implementado | `MrpController.Index`; hint BOM en órdenes (`BuildMrpHintAsync`); materiales de orden en `Detail` / `OrderMaterialService` | Disponible vía MRP y detalle de orden. |
| Ver estado de solicitudes de materiales de sus propias órdenes | ⚠️ Parcial | Scoped: `SolicitudesMaterialController` + `SolicitudMaterialService.GetListAsync` (filtra por `SolicitanteId`). Legacy: `InventarioController.Index` carga `GetRequestsAsync` **todas** | Flujo ficha: solo las propias. Flujo Inventario/MaterialRequest: instructor ve solicitudes de todos. |
| Control de calidad sobre sus propias órdenes | ⚠️ Parcial | `CalidadController` Index/Create L25–68; `QualityService.GetRecordsAsync/AddRecordAsync` sin filtro por instructor | Puede registrar/ver inspecciones de **cualquier** orden. |
| Reportes limitados a sus fichas/órdenes/procesos | ⚠️ Parcial | `ReportesController` `[Authorize]` sin Roles L10; filtros opcionales en query | Puede exportar reportes globales; el alcance por instructor es opcional, no forzado por rol. |
| Restricción: SIN acceso a inventario general (salvo flujo producción autorizado) | ❌ No implementado | `InventarioController` clase `[Authorize]` L13; Index L30–36 sin exclusión de Instructor; `_Layout.cshtml` L49 enlace Inventario a todos; vista Index muestra materiales a Instructor | Instructor **consulta** stock y solicita material. No ajusta stock (Admin/Bodeguero). La restricción del manual (sin consulta general) **no** se cumple. |

### 1.3 Bodeguero

| Función | Estado | Evidencia (archivo:método) | Notas |
|---------|--------|----------------------------|-------|
| Registrar entradas de materiales (compras, devoluciones, otras fuentes) | ⚠️ Parcial | `InventarioController.AddMaterial` L39–56; `AdjustStock` L59–74; `InventoryService.AddMaterialAsync/AdjustStockAsync` | Puede dar de alta y subir stock. **No** hay tipos de entrada (compra/devolución/otra) ni documento de origen. |
| Registrar reingreso de materiales/productos desde etapas (Trazo, Corte, Confección, Calidad, Terminado) | ❌ No implementado | `OrdenesController.PartialInventoryIn` L225–232 solo Admin+Instructor; Bodeguero sin acción equivalente | Reingreso a inventario terminado es flujo MES de Admin/Instructor, no de Bodeguero. |
| Registrar salidas de materiales entregados a producción | ✅ Implementado | `BodegaOrdenesController.Deliver` L57–80; `OrderMaterialService.DeliverAsync` L234–304; también `InventarioController.ApproveRequest` L111–121 | Descuenta stock al entregar/aprobar. |
| Recibir solicitudes de materiales generadas por órdenes de producción | ✅ Implementado | `BodegaOrdenesController.Index/Detail` L20–45; `BodegaSolicitudesController` L26–55 | Cola de materiales de órdenes + cola de SolicitudMaterial (ficha). |
| Verificar disponibilidad antes de aprobar solicitud | ✅ Implementado | `BodegaOrdenesController.ValidateStock` L47–55; `OrderMaterialService.ValidateStockAsync` L200–232; validación en `SolicitudMaterialApprovalService.ValidarCantidadAprobada` L185–194 | |
| Informar cantidad disponible de cada material solicitado | ✅ Implementado | `Views/BodegaSolicitudes/Detail.cshtml` columna «Stock disponible»; `Views/BodegaOrdenes/Detail.cshtml` columna Stock | UI muestra stock vs solicitado/pendiente. |
| Aprobar o rechazar entrega según existencia | ✅ Implementado | `BodegaSolicitudesController.Resolve` L57–88; `SolicitudMaterialApprovalService.ResolveSolicitudAsync` L75–182; Inventario Approve/Reject | Cantidad 0 = rechazo de ítem; stock insuficiente bloquea. |
| Entrega parcial cuando el stock es insuficiente, dejando pendiente registrado | ✅ Implementado | `OrderMaterialService.DeliverAsync` L298–303 + `RecalcMaterialsStatus` → `EntregaParcial` L331–334; `DetalleSolicitudEstado.AprobadoParcial` / `SolicitudMaterialEstado.AprobadaParcial` | Pendiente queda en `QuantityPending` / estado parcial. |
| Actualización de stock en tiempo real tras cada movimiento | ✅ Implementado | Descuento en `DeliverAsync` L278; `AplicarDecisionDetalle` L220; `ApproveRequestAsync` L152 | Persistido con `SaveChanges` / transacción. |
| Consultar historial de movimientos (fecha, usuario, tipo, cantidad) | ❌ No implementado | no encontrado ledger de inventario (sin `StockMovement`/`MaterialMovement`) | Existen movimientos MES de etapas/terminados por orden, no historial general de bodega (fecha/usuario/tipo/cantidad de materiales). |
| Gestión de su propio perfil (foto, contraseña, ver info de cuenta) | ✅ Implementado | `AccountController.Profile` L158–235 (cualquier autenticado); layout «Mi perfil» | Rol de solo lectura en perfil. |

---

## 2. Gaps críticos (solo ❌ y ⚠️)

Ordenados por rol, listos para convertir en prompts de implementación (un PR por ítem).

### Administrador

1. **⚠️** Crear/editar/eliminar cuentas de los tres roles — Falta: alta de Administrador desde UI; hard-delete de cuentas (hoy solo activate/deactivate).
2. **⚠️** CRUD completo de órdenes — Falta: editar orden, aprobar orden, eliminar/cancelar orden (hay enum `Cancelada` sin flujo).
3. **⚠️** Inventario completo — Falta: edición de metadatos de material (nombre/unidad) y clasificación de entradas/salidas por tipo/origen.
4. **❌** Asignar ficha técnica (BOM) a uno o varios instructores — No hay modelo ni UI; no confundir con asignación de fichas de formación.
5. **❌** Historial/auditoría de actividades de todos los usuarios — No hay registro global append-only de acciones.

### Instructor

6. **⚠️** Gestionar fichas técnicas solo si Admin autorizó — Falta permiso extendido + policy/CRUD condicional (hoy: solo consulta; CRUD reservado Admin/Bodeguero).
7. **❌** Crear órdenes solo con permiso — Falta permiso extendido y endpoint/policy; hoy Create es solo Administrador.
8. **⚠️** Gestionar únicamente órdenes asignadas — Falta filtrar listado/detalle/`AddProduction` por asignación; el gate de etapas ya existe.
9. **⚠️** Ver estado de solicitudes de sus propias órdenes — Falta acotar el listado legacy de `Inventario`/`MaterialRequest` al alcance del instructor.
10. **⚠️** Calidad solo sobre sus propias órdenes — Falta filtrar órdenes/registros por asignación del instructor.
11. **⚠️** Reportes limitados a su alcance — Falta forzar filtro por rol Instructor (hoy reportes globales con filtros opcionales).
12. **❌** Sin acceso a inventario general — Falta bloquear consulta de inventario al Instructor (salvo excepción explícita de flujo autorizado).

### Bodeguero

13. **⚠️** Entradas tipificadas (compras, devoluciones, otras) — Falta tipo/origen de movimiento al registrar entradas.
14. **❌** Reingreso desde etapas de producción — Falta acción/autorización de Bodeguero para reingresos desde Trazo/Corte/Confección/Calidad/Terminado.
15. **❌** Historial de movimientos de bodega — Falta ledger (fecha, usuario, tipo, cantidad) consultable tras cada entrada/salida/ajuste.

---

## 3. Referencias rápidas de autorización actual

| Mecanismo | Ubicación |
|-----------|-----------|
| Roles | `UserRoles` → Administrador, Instructor, Bodeguero |
| Permisos extendidos | `ExtendedPermissions` + claims `permiso` |
| Policies ASP.NET | `SipitexAuthorizationExtensions.AddSipitexPolicies` → RegistrarMateriales, AprobarSolicitudes, SimularMrp, ConfigurarAlertas |
| Reglas | `PermissionRules` |
| Patrón de referencia | `InventarioController` (Roles + Policy) |

*Fin del reporte de auditoría. Sin propuestas de código en este paso.*
