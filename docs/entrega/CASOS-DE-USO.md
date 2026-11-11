# Casos de uso por rol — SIPITEX v1.0

Criterios de aceptación tomados del comportamiento documentado en código/manuales/`PermissionRules`/`EXTRACCION` (no de inventarios hipotéticos).  
Actores: **Administrador**, **Instructor**, **Encargado de bodega**.

Fuentes: `docs/MANUAL-*.md`, `README.md`, extracción §3 y §6, IEEE 830 matriz actor↔CU, HANDOVER.

---

## Administrador

### CU-A01 — Gestionar usuarios
- **Pre:** Rol Administrador.
- **Flujo:** `/Account/Users` → crear / editar / activar-desactivar / eliminar.
- **Aceptación:** Solo Administrador (`Roles=Administrador`). Puede crear los tres roles. Encargado de bodega debe tener ≥1 planta asignada (manual admin). Hard-delete sujeto a blockers del servicio.
- **Evidencia:** `AccountController`; `UserAccountServiceTests`.

### CU-A02 — Autenticación y sesión
- **Flujo:** Login / Logout; keep-alive; perfil.
- **Aceptación:** Cookies con expiración (`SeguridadOptions`, default 20 min — no 8 h de docs). Lockout y flujos de correo según configuración.
- **Evidencia:** `LoginLockoutTests`, `Program.cs`.

### CU-A03 — Administrar plantas e inventario
- **Flujo:** Plantas de inventario → ver todas; administrar plantas; alta/edición/baja materiales; ajustar stock; estado físico; ficha técnica de material.
- **Aceptación:** Planta de Inventario 1 no se elimina. Import Excel solo Admin; **sobrescribe stock** al confirmar (HANDOVER). Niveles OK / Bajo / Crítico / Sin mínimo.
- **Evidencia:** `PlantasInventarioController`, `InventarioController` Importar; manuales.

### CU-A04 — Órdenes de producción y MES
- **Flujo:** Crear/editar/aprobar/cancelar OP; etapas; asignar instructores; materiales; avance.
- **Aceptación:** Crear con policy `PuedeCrearOrdenes`. Cancelar **no** revierte stock (HANDOVER #3). Instructor ve solo su alcance; Admin ve global.
- **Evidencia:** `OrdenesController`; tests de approve/edit/cancel/flow.

### CU-A05 — MRP / BOM
- **Flujo:** Mantener BOM; simular; asignar instructores a ficha técnica; eliminar BOM.
- **Aceptación:** Simular/gestionar según policies. Delete BOM solo Admin. MRP **no** corre solo al crear OP (HANDOVER #1): hay que simular e importar.
- **Evidencia:** `MrpController`; `MrpServiceTests`.

### CU-A06 — Aprobar solicitudes y entregar materiales
- **Flujo:** Cola solicitudes; resolver; materiales de órdenes (entrega/reingreso).
- **Aceptación:** Policy `PuedeAprobarSolicitudes`. No entregar a orden cerrada ni pendiente de aprobación (manual bodega). Consumo no redescuenta stock entregado (#152).
- **Evidencia:** controllers de planta; `StockUnicoDescuentoTests`.

### CU-A07 — Calidad, actas, trazabilidad, reportes, alertas, auditoría, costeo
- **Aceptación:** Acceso Admin a calidad, actas (anular solo Admin), trazabilidad, reportes PDF/Excel, evaluar alertas (`PuedeConfigurarAlertas`), `/Auditoria`, costeo (tarifa POST solo Admin; MO no suma al total).
- **Evidencia:** controllers listados en extracción §6.

---

## Instructor

### CU-I01 — Ver y avanzar órdenes de su alcance
- **Pre:** Instructor autenticado; asignación por etapa MES o ficha.
- **Aceptación:** Lista restringida a su alcance. Crear OP solo con permiso `Ordenes.Crear`. Sin permiso, alta no disponible (manual instructor).
- **Evidencia:** `OrdenesCrearPermissionTests`, scope tests.

### CU-I02 — Solicitar insumos / materiales
- **Flujo:** Mis solicitudes / Solicitar insumos (descripción libre + planta destino).
- **Aceptación:** Roles Admin|Instructor en `SolicitudesMaterialController`. No consulta stock consolidado en Plantas de inventario (`PuedeConsultarInventario` = Admin|Encargado).
- **Evidencia:** `SolicitarInsumosPlantasTests`; manual instructor.

### CU-I03 — MRP con permisos extendidos
- **Aceptación:** Consulta materiales/BOM según rol. `Mrp.Simular` / `Mrp.GestionarFichas` para simular o editar. **No** elimina BOM aunque gestione fichas.
- **Evidencia:** `MrpGestionarFichasPermissionTests`.

### CU-I04 — Fichas y sesión de producción
- **Flujo:** Fichas & producción; Register / QuickRegister; grupos de confección.
- **Aceptación:** Registro por ficha/grupo asignado. `User.FichaAsignadaId` solo informativo (HANDOVER #5).
- **Evidencia:** `FichaInstructorAssignmentTests`, `FichaAssignedOrderTests`.

### CU-I05 — Consumo de materiales
- **Aceptación:** Registrar hasta entregado − consumido − reingresado. Costo unitario congelado. **No** consumir en OP finalizada/cancelada (#159).
- **Evidencia:** `MaterialConsumptionServiceTests`; manual.

### CU-I06 — Control de calidad
- **Aceptación:** Solo órdenes de su acceso. Bueno en etapa Calidad → ciclo a producto terminado (sin pantallazo PT dedicado). Malo → Confección con justificación. Reproceso: motivo y responsable obligatorios. **Sin** criterios por tipo de prenda (RF16 seed Ausente).
- **Evidencia:** `QualityInstructorScopeTests`; manual instructor.

### CU-I07 — Actas y trazabilidad
- **Aceptación:** Puede ver/crear actas y consultar trazabilidad (roles Admin|Instructor|Encargado en controllers). No ve reingreso ni movimientos de stock (manual).

### CU-I08 — Aprobar solicitudes (opcional)
- **Aceptación:** Solo si Admin asigna `Solicitudes.Aprobar`.

---

## Encargado de bodega

### CU-E01 — Consultar stock de plantas asignadas
- **Pre:** Usuario con ≥1 planta; si ninguna, lista vacía (manual).
- **Aceptación:** Policy `PuedeConsultarInventario`. Solo plantas asignadas. Filtros por nivel/categoría/tipo. No importa Excel ni administra catálogo de plantas ni usuarios ni costeo.
- **Evidencia:** `PlantasInventarioController`; manual bodeguero.

### CU-E02 — Registrar / editar operación de materiales en planta
- **Aceptación:** Puede registrar materiales vía `PuedeRegistrarMateriales` (Admin|Encargado|Instructor+permiso). Detalle/ajustes/estado/ficha según Authorize de actions (Admin|Encargado en varias). Create/Edit/Delete planta y Edit/Delete material: Admin.

### CU-E03 — Aprobar / rechazar solicitudes
- **Flujo:** Cola de su planta → detalle → resolver eligiendo material del catálogo.
- **Aceptación:** No aprobar más de lo disponible. Policy `PuedeAprobarSolicitudes`.
- **Evidencia:** `SolicitudMaterialApprovalServiceTests`.

### CU-E04 — Entregar y reingresar materiales de orden
- **Aceptación:** Entrega solo materiales pedidos; no a orden cerrada ni pendiente de aprobación. Reingreso: sobrante ≤ disponible; también en OP finalizada/cancelada (#160). PT en unidades de etapa; no altera stock de insumos. Instructor no ve reingreso.
- **Evidencia:** `PlantasInventarioOrdenesController`; `StageReentryTests`.

### CU-E05 — Movimientos de stock
- **Aceptación:** Consulta historial; no edita líneas. Ajuste desde inventario de planta genera otra línea.
- **Evidencia:** manual; `StockMovementLedgerTests`.

### CU-E06 — MRP / simulación
- **Aceptación:** Encargado puede simular (`PuedeSimularMrp`) y gestionar fichas técnicas según policy; delete BOM solo Admin. **No** crea órdenes (`PuedeCrearOrdenes` excluye Encargado).

### CU-E07 — Actas y trazabilidad
- **Aceptación:** Acceso compartido Admin|Instructor|Encargado. Anular acta: solo Administrador.

---

## Matriz rápida (IEEE 830 §4.1, contrastada con código)

| Caso de uso | Admin | Encargado | Instructor |
|-------------|:-----:|:---------:|:----------:|
| Iniciar sesión | ✓ | ✓ | ✓ |
| Gestionar usuarios | ✓ | | |
| Registrar materiales | ✓ | ✓ | con permiso |
| Consultar stock consolidado | ✓ | ✓ | **no** (policy) |
| Solicitar material | ✓ | | ✓ |
| Aprobar / rechazar | ✓ | ✓ | con permiso |
| Crear orden | ✓ | | con permiso `Ordenes.Crear` |
| Registrar producción | ✓ | | ✓ |
| BOM / MRP | ✓ | ✓ | con permiso |
| Control de calidad | ✓ | | ✓ |
| Descargar reportes | ✓ | ✓ | ✓ (alcance) |
| Configurar / evaluar alertas | ✓ | parcial | parcial |
| Auditoría | ✓ | | |
| Importar inventario | ✓ | | |
