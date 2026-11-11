# Rutas MVC — controllers, actions y Authorize

**Fuente:** `docs/entrega/EXTRACCION-DATOS-REPO.md` §6 (extraído de `src/Sipitex.Web/Controllers/*`).  
Convención: `/{Controller}/{Action}` salvo `BusquedaController` → `[Route("api/busqueda")]`.  
`hereda` = atributo de clase. Roles = `UserRoles.*`; policies = `AuthorizationPolicyNames.*`.

| Controller | Authorize clase | Action | HTTP | Authorize action |
|------------|-----------------|--------|------|------------------|
| AccountController | `[Authorize]` | Login | GET, POST | AllowAnonymous |
| | | ForgotPassword | GET, POST | AllowAnonymous |
| | | ForgotPasswordConfirmation | GET | AllowAnonymous |
| | | ResetPassword | GET, POST | AllowAnonymous |
| | | ConfirmEmail | GET, POST | AllowAnonymous |
| | | ResendEmailConfirmation | POST | AllowAnonymous |
| | | Logout | POST | hereda |
| | | KeepAlive | POST | hereda |
| | | Profile | GET, POST | hereda |
| | | Users | GET | Roles=Administrador |
| | | CreateUser | GET, POST | Roles=Administrador |
| | | EditUser | GET, POST | Roles=Administrador |
| | | ToggleUserStatus | POST | Roles=Administrador |
| | | DeleteUser | POST | Roles=Administrador |
| | | DescargarFuncionalidades | GET | Roles=Administrador |
| | | AccessDenied | GET | AllowAnonymous |
| | | Photo | GET | AllowAnonymous |
| ActasController | Roles=Admin,Instructor,Encargado | Index, Create, Details, Firmar, Pdf, EnviarCorreo | GET/POST | hereda |
| | | Anular | POST | Roles=Administrador |
| AlertasController | `[Authorize]` | Index, SavePreferences, Probar | GET/POST | hereda |
| | | Evaluar | POST | Policy=PuedeConfigurarAlertas |
| AuditoriaController | Roles=Administrador | Index | GET | hereda |
| AyudaController | `[Authorize]` | Index | — | hereda |
| | | Buscar | GET | hereda |
| BuscarController | `[Authorize]` | Sugerencias | GET | hereda |
| BusquedaController | `[Authorize]` + Route `api/busqueda` | Get | GET `/api/busqueda` | hereda |
| CalidadController | `[Authorize]` | Index | GET | Roles=Admin,Instructor |
| | | Create | POST | Roles=Admin,Instructor |
| ConsumosController | Roles=Admin,Instructor | Index, Create | GET, POST | hereda |
| CostosController | Roles=Admin,Instructor | Index | GET | hereda |
| | | Tarifa | POST | Roles=Administrador |
| EstadisticasController | `[Authorize]` | Index | GET | hereda |
| FichasController | `[Authorize]` | Index, CreateFicha, AssignInstructor, RemoveInstructor, UpdateInstructorProceso, Register, QuickRegister | GET/POST | Roles=Admin,Instructor |
| GruposConfeccionController | Roles=Admin,Instructor | Index, Create, Cerrar | GET/POST | hereda |
| HomeController | — | Index | — | `[Authorize]` |
| | | Privacy | — | AllowAnonymous (redirige Privacidad) |
| | | Error | — | AllowAnonymous |
| InventarioController | `[Authorize]` | Importar, DescargarPlantilla, Confirmar | GET/POST | Roles=Administrador |
| | | Index | GET | hereda (redirect → PlantasInventario) |
| | | Movimientos | GET | Roles=Admin,Encargado |
| MrpController | `[Authorize]` | Index, Materials, MaterialsByCodigo | GET | Roles=Admin,Encargado,Instructor |
| | | Simulate | POST | Policy=PuedeSimularMrp |
| | | Create, Edit | GET, POST | Policy=PuedeGestionarFichasTecnicas |
| | | AssignInstructor, RemoveInstructor, Delete | POST | Roles=Administrador |
| OrdenesController | `[Authorize]` | Index, Detail | GET | Roles=Admin,Encargado,Instructor |
| | | Edit, Approve, Cancel, AddStage, RemoveStage, MoveStage, AssignInstructor, SetStagePermission | GET/POST | Roles=Administrador |
| | | Create | POST | Policy=PuedeCrearOrdenes |
| | | AddProduction, AddMaterial, RemoveMaterial, ImportBomMaterials, StartStage, PauseStage, ResumeStage, CompleteStage, ProcessUnits, SendToNext, PartialInventoryIn, PartialWithdraw | POST | Roles=Admin,Instructor |
| PlantasInventarioController | `[Authorize]` | Index, Consultar | GET | Policy=PuedeConsultarInventario |
| | | Detalle, Movimientos, Material, GuardarFicha, AdjustStock, UpdateStatus | GET/POST | Roles=Admin,Encargado |
| | | AddMaterial | POST | Policy=PuedeRegistrarMateriales |
| | | EditMaterial, DeleteMaterial, Create, Edit, Delete, Reactivar | GET/POST | Roles=Administrador |
| PlantasInventarioOrdenesController | Roles=Admin,Instructor,Encargado | Index, Detail | GET | hereda |
| | | Reingreso, ValidateStock, Deliver | GET/POST | Roles=Admin,Encargado |
| PlantasInventarioSolicitudesController | Roles=Admin,Instructor,Encargado | Index, Detail | GET | hereda |
| | | Resolve | POST | Policy=PuedeAprobarSolicitudes |
| PrivacidadController | — | Index | GET | AllowAnonymous |
| | | Descargar, Aceptar, Solicitar | GET/POST | `[Authorize]` |
| ReportesController | `[Authorize]` | Index, Inventario, Ordenes, Calidad, Dashboard, ActividadInstructor, Actividad, ActividadPdf, ActividadExcel | GET | hereda |
| SolicitudesMaterialController | Roles=Admin,Instructor | Index, Detail, SolicitarInsumos, Create | GET/POST | hereda |
| TrazabilidadController | Roles=Admin,Instructor,Encargado | Index, Generar, Details | GET/POST | hereda |
| VerificarController | `[AllowAnonymous]` | Acta | GET | hereda (+ rate limit `verificar-acta`) |

## Endpoints mínimos fuera de controllers MVC

| Ruta | Auth | Notas (`Program.cs`) |
|------|------|----------------------|
| `GET /healthz` | Anónimo | Texto `ok`; no consulta BD |
| `GET /health` | Anónimo | HealthChecks + EF |
| `GET /version` | Anónimo | JSON commit/build/builtAt |

## Policies (resumen)

Fuente: `PermissionRules` / EXTRACCION §3.3.

| Policy | Quién pasa |
|--------|------------|
| PuedeRegistrarMateriales | Admin **o** Encargado **o** Instructor + `Inventario.Registrar` |
| PuedeAprobarSolicitudes | Admin **o** Encargado **o** Instructor + `Solicitudes.Aprobar` |
| PuedeSimularMrp | Admin **o** Encargado **o** Instructor + `Mrp.Simular` |
| PuedeGestionarFichasTecnicas | Admin **o** Encargado **o** Instructor + `Mrp.GestionarFichas` (Delete BOM: solo Admin) |
| PuedeCrearOrdenes | Admin **o** Instructor + `Ordenes.Crear` (Encargado **no**) |
| PuedeConfigurarAlertas | Admin **o** claim `Alertas.Configurar` |
| PuedeConsultarInventario | Solo Admin **o** Encargado |
