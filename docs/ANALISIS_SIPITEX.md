# Análisis de SIPITEX

Auditoría de solo lectura del código en `main` (commit `50eb1fe`). No propone cambios ya aplicados: describe lo que el repositorio hace hoy y una hoja de ruta. Cada hallazgo cita archivo y línea. No incluye secretos ni datos personales.

## Índice

1. [Mapa del sistema](#1-mapa-del-sistema)
2. [Funcionalidad repetida o solapada](#2-funcionalidad-repetida-o-solapada)
3. [Incoherencias de lógica](#3-incoherencias-de-lógica)
4. [UX y UI](#4-ux-y-ui)
5. [Interactividad y valor para el usuario](#5-interactividad-y-valor-para-el-usuario)
6. [Calidad técnica](#6-calidad-técnica)
7. [Datos de demostración](#7-datos-de-demostración)
8. [Priorización y hoja de ruta](#8-priorización-y-hoja-de-ruta)
9. [Resumen](#9-resumen)

## 1. Mapa del sistema

### 1.1 Roles

Los tres roles viven como texto en `User.Rol` (`src/Sipitex.Domain/Entities/User.cs`, líneas 67-72):

| Rol | Texto guardado |
|---|---|
| Administrador | `Administrador` |
| Instructor | `Instructor` |
| Encargado de bodega | `Encargado de bodega` |

Hay permisos extra en el claim `permiso` (`src/Sipitex.Domain/Entities/ExtendedPermissions.cs`, líneas 7-15): registrar inventario, aprobar solicitudes, simular MRP, gestionar fichas técnicas, crear órdenes y configurar alertas. Las reglas están en `src/Sipitex.Application/Authorization/PermissionRules.cs`.

### 1.2 Módulos del menú

El menú está en `src/Sipitex.Web/Views/Shared/_Layout.cshtml` (líneas 54-125).

| Ítem del menú | Ruta | Quién lo ve en el menú |
|---|---|---|
| Órdenes de producción | `Ordenes/Index` | Cualquier sesión (línea 56, sin `IsInRole`) |
| MRP / Materiales | `Mrp/Index` | Cualquier sesión (línea 57) |
| Plantas de inventario | `PlantasInventario/Index` | Administrador y encargado (líneas 59-62) |
| Inventario por bodega | `PlantasInventario/Consultar` | Los tres roles (líneas 64-67) |
| Solicitudes de materiales | `PlantasInventarioSolicitudes/Index` | Los tres roles (líneas 70-73) |
| Materiales de órdenes | `PlantasInventarioOrdenes/Index` | Los tres roles (líneas 73) |
| Reingreso desde etapas | `PlantasInventarioOrdenes/Reingreso` | Administrador y encargado (líneas 75-78) |
| Movimientos de stock | `PlantasInventario/Movimientos` | Administrador y encargado (líneas 80-83) |
| Fichas y producción, Mis solicitudes, Solicitar insumos, Calidad, Grupos, Consumo | varios | Administrador e instructor (líneas 86-94) |
| Actas, Trazabilidad | `Actas`, `Trazabilidad` | Los tres roles (líneas 96-99) |
| Costeo | `Costos` | Administrador e instructor (líneas 104-106) |
| Estadísticas, Reportes, Alertas | análisis | Cualquier sesión (líneas 108-110) |
| Auditoría, Usuarios | `Auditoria`, `Account/Users` | Solo administrador (líneas 111-119) |
| Mi perfil | `Account/Profile` | Sesión iniciada (líneas 121-124) |

El inicio (`Home/Index`) es una grilla de accesos, no un tablero con cifras (`src/Sipitex.Web/Views/Home/Index.cshtml`, líneas 7-58).

### 1.3 Entidades y relaciones principales

| Entidad | Papel | Relación |
|---|---|---|
| `PlantaInventario` | Bodega lógica (el seed crea dos plantas) | 1—N `Material` |
| `Material` | Insumo con stock, mínimo, unidad y estado físico | Pertenece a una planta |
| `BomProduct` + `BomItem` | Ficha técnica (receta) | Ítem apunta a `Material` |
| `ProductionOrder` | Orden OP | Estado `OrderStatus`; snapshot en `ProductionOrderBomSnapshot` |
| `ProductionOrderMaterialRequirement` | Material a entregar de esa orden | Cantidad pedida y entregada |
| `ProductionOrderStage` | Etapa MES | Movimientos e historial |
| `Ficha` | Grupo SENA / proceso | N—N instructores (`FichaInstructor`); puede apuntar a una orden |
| `SolicitudMaterial` + `DetalleSolicitudMaterial` | Pedido multi-ítem del instructor | Estados en `SolicitudMaterialEstado` |
| `MaterialRequest` | Pedido legado de un solo ítem | Sigue mapeado (`SipitexDbContext.cs`, línea 45) |
| `StockMovement` | Ledger de entradas, salidas y ajustes | Apunta a `Material` y usuario |
| `ConsumoMaterial` | Consumo real en confección | Orden + material |
| `User` | Cuenta | Rol texto; encargado N—N plantas (`UserPlantaInventario`) |

El filtro global de planta está en `Material`, `SolicitudMaterial` y `StockMovement` (`SipitexDbContext.cs`, líneas 118, 505 y 698). Si el accessor no trae lista de plantas (`null`), no restringe. El instructor entra en ese caso: `VisiblePlantas` devuelve todas las plantas cuando `PlantaInventarioIds` es null (`PlantasInventarioController.cs`, líneas 527-531).

### 1.4 Flujos de punta a punta

**Ficha técnica y MRP**

1. El administrador (o quien tenga `Mrp.GestionarFichas`) arma `BomProduct` y `BomItem` en `MrpController`.
2. `MrpService.SimulateAsync` multiplica `QuantityPerUnit * quantity` y resta el stock del material (`MrpService.cs`, líneas 42-51). No reserva stock: solo informa el déficit.
3. Al crear la orden se congela la receta en `ProductionOrderBomSnapshot` (el seed de demo lo hace en `DbInitializer.cs`, líneas 96-104).

**Orden**

Estados en `src/Sipitex.Domain/Enums/OrderStatus.cs` (líneas 5-9): `Pendiente`, `EnProceso`, `Finalizada`, `Cancelada`.

- Crear: administrador, o instructor con `Ordenes.Crear` (`PermissionRules.cs`, líneas 35-38). El encargado no crea órdenes.
- Aprobar (pasa a producción) y cancelar: acciones de administrador en `OrdenesController`.
- `OrderMaterialService` rechaza entrega si la orden está `Pendiente` o cerrada (`OrderMaterialService.cs`, líneas 250-253).

**Solicitud de materiales (flujo vigente)**

1. El instructor crea una `SolicitudMaterial` desde `SolicitudesMaterial/SolicitarInsumos` o desde Fichas (el texto vacío de `SolicitudesMaterial/Index.cshtml`, línea 27, nombra ambos caminos).
2. La cola operativa es `PlantasInventarioSolicitudes`. Ahí el encargado o el administrador resuelven ítems.
3. `SolicitudMaterialApprovalService` descuenta stock dentro de `ExecuteInTransactionAsync` (`SolicitudMaterialApprovalService.cs`, líneas 58 y 134).

**Materiales de la orden y reingreso**

1. `PlantasInventarioOrdenes/Index` lista lo que la orden debe recibir.
2. La entrega descuenta `Material.Stock` y sube `QuantityDelivered` en la misma transacción (`OrderMaterialService.cs`, líneas 268-289). Si el resultado quedara negativo, lanza excepción.
3. `PlantasInventarioOrdenes/Reingreso` devuelve material desde una etapa. El menú lo limita a administrador y encargado (layout, líneas 75-78). El controlador de reingreso exige esos mismos roles (`PlantasInventarioOrdenesController.cs`, líneas 76 y 84). La entrega de materiales usa la misma restricción (línea 129).

**Stock y movimientos**

- Alta, edición y ajuste viven en `PlantasInventario/Detalle` (solo administrador y encargado: `PlantasInventarioController.cs`, línea 115).
- El ajuste no admite negativo: `Math.Max(0, dto.NewStock)` (`InventoryService.cs`, líneas 166 y 186-187). Si el stock sube, el origen es obligatorio (líneas 171-172).
- El historial es `PlantasInventario/Movimientos` (`PlantasInventarioController.cs`, líneas 178-184).

**Ficha de grupo, consumo y calidad**

El instructor registra sesiones en `Fichas`, consumos en `Consumos` y calidad en `Calidad`. Esos ítems no aparecen en el menú del encargado (layout, líneas 86-94).

### 1.5 Qué hace cada rol

| Acción | Administrador | Encargado de bodega | Instructor |
|---|---|---|---|
| Ver todas las plantas y su inventario editable | Sí (`Detalle`, línea 115) | Solo plantas asignadas; si tiene una, el índice lo manda al detalle (líneas 46-50) | No entra a `Detalle` |
| Consultar inventario (lectura) | Sí | Sí | Sí: el menú y `Consultar` lo permiten (layout 64-67; controlador línea 69) |
| Ajustar stock y ver movimientos | Sí | Sí | No |
| Aprobar solicitudes y entregar materiales de orden | Sí | Sí | Ve las pantallas; las acciones de entrega y resolución piden administrador o encargado (`PlantasInventarioOrdenesController.cs`, líneas 76-129; `PlantasInventarioSolicitudesController.cs`, línea 141) |
| Reingreso desde etapas | Sí | Sí | No |
| Crear orden | Sí | No (`PermissionRules.cs`, líneas 35-38) | Solo con permiso `Ordenes.Crear` |
| Fichas, calidad, grupos, consumos, costeo | Sí | El menú los oculta | Sí |
| Usuarios y auditoría | Sí | No | No |
| Estadísticas y reportes | Sí, alcance amplio | Sí, el controlador no filtra al encargado (`EstadisticasController.cs`, líneas 22-23) | Sí, acotado a sus órdenes (`EstadisticasController.cs`, líneas 10-11 y 22) |

## 2. Funcionalidad repetida o solapada

| Qué se repite | Dónde | Recomendación |
|---|---|---|
| Dos pantallas de inventario de la misma planta | `PlantasInventario/Detalle` (edición, filtros `busqueda`/`categoria`/`nivel`) y `PlantasInventario/Consultar` (lectura, filtros `q`/`nivel`). Vistas `Detalle.cshtml` y `Consultar.cshtml`. | Fusionar la consulta dentro del detalle, con modo solo lectura para el instructor. Hoy el instructor no puede abrir el detalle y el encargado tiene dos listas del mismo stock. |
| Controlador y vistas de `Inventario` que ya no operan | `InventarioController.cs` líneas 11-12: las rutas responden redirección y no modifican stock. Las vistas `Views/Inventario/Index.cshtml` y `Movimientos.cshtml` siguen en el proyecto, con formularios de alta, ajuste y solicitudes. | Eliminar las vistas muertas cuando los tests dejen de depender del controlador. El controlador puede quedar solo como redirección 301, que es lo que ya declara. |
| Dos modelos de solicitud | Vigente: `SolicitudMaterial` + cola `PlantasInventarioSolicitudes`. Legado: `MaterialRequest`, aún creado y aprobado en `InventoryService.cs` (líneas 258-358) y leído por las alertas (`AlertService.cs`, líneas 282-290). | Dejar de alimentar `MaterialRequest`. Hacer que la alerta de pendientes lea `SolicitudMaterial`. Conservar la tabla solo como historial. |
| Filtros de nivel de stock duplicados | `InventarioConsultaFilter` en Consultar y `PlantaDetalleConsulta` + `CoincideNivel` en Detalle (`PlantasInventarioController.cs`, líneas 100 y 129-134). | Un solo helper de filtro. |
| Icono `fa-clipboard-list` en Órdenes, Inventario por bodega, Mis solicitudes y Auditoría | `_Layout.cshtml`, líneas 56, 67, 89 y 113. `fa-clipboard-check` en Materiales de órdenes y en Calidad (líneas 73 y 92). | Dejar iconos distintos. No cambia reglas de negocio. |
| Grilla de inicio y menú lateral | `Home/Index.cshtml` líneas 14-57 repite módulos que el sidebar ya lista, y sin las mismas condiciones de rol (Fichas y Calidad se muestran a cualquiera que abra Inicio). | Hacer que el inicio respete el mismo filtro del menú, o convertirlo en tablero. |

## 3. Incoherencias de lógica

### 3.1 Stock, alertas y nivel “crítico”

`StockNivelHelper.Classify` (`StockNivelHelper.cs`, líneas 5-16):

- stock `<= 0` → `Critico`, aunque el mínimo sea 0;
- stock mayor que 0 y menor que el mínimo (si el mínimo es mayor que 0) → `Bajo`;
- en otro caso → `Ok`.

La alerta de stock usa otra regla: `m.Stock < m.MinStock` (`AlertService.cs`, líneas 268-270). Con stock 0 y mínimo 0 la pantalla dice Crítico y el correo no sale, porque `0 < 0` es falso. El catálogo CMTC nace así (sección 7).

El ajuste manual trunca a cero (`InventoryService.cs`, línea 166) en lugar de rechazar un número negativo. El usuario puede creer que guardó −5 y la base queda en 0.

No hay token de concurrencia en el modelo (`SipitexDbContext.cs` no declara `IsConcurrencyToken` ni `xmin`). Dos ajustes simultáneos del mismo material hacen última escritura gana. La entrega de orden y la aprobación de solicitud sí van en transacción (`OrderMaterialService.cs`, línea 268; `SolicitudMaterialApprovalService.cs`, líneas 58 y 134), pero leen el stock sin bloqueo de fila: dos transacciones paralelas pueden descontar el mismo saldo.

El MRP no reserva. `SimulateAsync` solo calcula déficit (`MrpService.cs`, líneas 49-51). Dos órdenes pueden “caber” en el mismo stock.

### 3.2 Solicitudes y alertas desalineadas

La cola visible es `SolicitudMaterial`. La alerta `SolicitudPendiente` recorre `MaterialRequest` y el texto dice “Apruebe o rechace en Inventario” (`AlertService.cs`, líneas 282-290). `Inventario/Index` ya no es la pantalla de trabajo: el controlador redirige (`InventarioController.cs`, líneas 11-12 y 30-39).

Al resolver una solicitud, si hace falta un material nuevo, se inserta con `SaveChanges` **antes** de la transacción de descuento. El propio código lo documenta: si la transacción falla, queda un material con stock 0 (`SolicitudMaterialApprovalService.cs`, líneas 233-248).

### 3.3 Órdenes

Los estados existen (`OrderStatus.cs`). La alerta de “por vencer” incluye toda orden `EnProceso` cuyo plazo sea hoy o anterior, porque la condición es `Deadline <= today.AddDays(7)` (`AlertService.cs`, líneas 298-299). Una orden ya vencida entra en “por vencer” y, si el avance es menor al 50 % y el plazo cae en 14 días, también en “atrasada” (líneas 309-313). No hay un tipo de alerta distinto para “ya venció”.

Cancelar una orden no revierte el stock entregado. La vista lo dice: “El stock entregado no se revierte” (`Ordenes/Detail.cshtml`, línea 38).

### 3.4 Permisos que no coinciden

| Superficie | Qué dice | Qué hace el servidor |
|---|---|---|
| Política `PuedeConsultarInventario` | “solo Admin/Encargado. Instructor consulta materiales vía MRP/órdenes” (`PermissionRules.cs`, líneas 44-48) | El menú y `Consultar` abren el inventario al instructor (layout 64-67; controlador línea 69). `VisiblePlantas` no lo recorta si no tiene plantas asignadas (líneas 527-531). |
| Inicio | Muestra Fichas y Calidad a cualquier rol (`Home/Index.cshtml`, líneas 33-42) | `FichasController` y `CalidadController` exigen administrador o instructor. El encargado recibe acceso denegado. |
| Menú Estadísticas y Reportes | Visibles para los tres roles (layout 108-109) | El instructor queda acotado; el encargado ve el conjunto sin filtro de planta (`EstadisticasController.cs`, líneas 22-23). |
| Alerta de solicitudes | Apunta a Inventario | La cola real es `PlantasInventarioSolicitudes`. |

### 3.5 Datos huérfanos al borrar

- Borrar usuario está bloqueado si hay movimientos, solicitudes, fichas u otras FK (`UserRepository.GetDeletionBlockersAsync`). La vista lo advierte (`Account/Users.cshtml`, línea 84).
- Crear material durante la resolución de una solicitud puede dejarlo huérfano (líneas 244-246 de `SolicitudMaterialApprovalService.cs`).
- `MaterialRequest` puede quedar como historial sin pantalla. Sigue impidiendo borrar al usuario solicitante (`UserRepository.cs`, línea 82).

## 4. UX y UI

### 4.1 Vocabulario

En la misma consulta conviven tres nombres:

- título “Inventario por bodega” (`Consultar.cshtml`, líneas 3-4 y 13);
- subtítulo “planta de inventario” (línea 14);
- vacío “Esta bodega no tiene insumos” (línea 124).

El detalle de la planta usa la miga “Administración / Plantas de inventario” (`Detalle.cshtml`, línea 5) aunque es la pantalla de trabajo del encargado, no solo un catálogo.

`PlantasInventarioOrdenes` usa la miga “PlantaInventario · Órdenes” (`Index.cshtml`, `Detail.cshtml` y `Reingreso.cshtml`, línea 4 de cada una): es el nombre del tipo, no el del menú (“Materiales de órdenes” / “Reingreso desde etapas”).

“Consultar”, “Ver” y “Detalle” nombran la misma idea de abrir una planta. El menú dice “Inventario por bodega”; la acción se llama `Consultar`.

### 4.2 Navegación y migas

Casi todas las vistas asignan `ViewData["Breadcrumb"]`. El layout lo pinta como texto (`_Layout.cshtml`, líneas 20 y 144): no son enlaces. No se puede volver al padre desde la miga.

El ítem activo del menú usa solo el controlador (`NavClass`, líneas 6). En `PlantasInventario` hay cuatro destinos (índice, consultar, detalle, movimientos) y el código compensa con condiciones extra (líneas 7-14, 66 y 82). En `PlantasInventarioOrdenes`, Índice y Reingreso comparten controlador: Reingreso se marca aparte (líneas 77-78), pero Índice queda activo también en el detalle.

### 4.3 Estados vacíos, errores y confirmación

Hay un partial reutilizable `_EmptyState.cshtml` y muchas pantallas lo usan (inventario, fichas, MRP, solicitudes, actas, estadísticas). Eso es consistente.

Los mensajes de éxito van por `TempData["Message"]` y un toast del layout (líneas 27-28). No hay deshacer.

Las confirmaciones son `confirm()` del navegador o `data-confirm` (por ejemplo `Ordenes/Detail.cshtml` líneas 29 y 38, `PlantasInventario/Detalle.cshtml` línea 300). No hay un cuadro propio ni una acción de revertir.

### 4.4 Formularios y tablas

Validación de cliente: el proyecto incluye jQuery Validate (`wwwroot/lib/jquery-validation`) y `_ValidationScriptsPartial.cshtml`. No todas las vistas lo referencian; varias tablas editan en línea con `required` en el input (Detalle, líneas 262-263) y el servidor vuelve a validar en el servicio.

No hay paginación de listas. Los `Take` del código son topes de búsqueda (5 por categoría, `src/Sipitex.Infrastructure/Search/BusquedaService.cs` línea 11), de historial en el detalle de orden (`Ordenes/Detail.cshtml`, líneas 98, 353, 378) o de sesiones. Un inventario grande se renderiza entero.

Filtros sí existen en Consultar y en Detalle (texto y nivel). Exportar está en Reportes (PDF/Excel), no en la tabla de la planta.

Orden de columnas: las consultas ordenan en servidor en algunos listados (la búsqueda ordena por nombre, `BusquedaService.cs` línea 28). Las tablas de inventario no exponen orden por clic.

### 4.5 Responsive, accesibilidad y carga

`sipitex.css` tiene cortes en 980px (líneas 107 y 283), 899px (línea 153), 639px (línea 156), 560px (línea 295) y 640px (línea 323). El botón de menú tiene `aria-label` y `aria-expanded` (`_Layout.cshtml`, líneas 134-139). El buscador también (línea 158).

La foto de perfil usa `alt=""` (línea 176): el nombre ya está al lado, así que el vacío es aceptable, pero el icono decorativo del menú no marca `aria-hidden` en todos los `<i>`.

No hay indicador de carga al filtrar inventario ni al simular MRP: el formulario hace submit completo (`Consultar.cshtml`, línea 29, `onchange="this.form.submit()"`).

Contraste: la hoja usa Plus Jakarta Sans y una paleta propia en `sipitex.css`. Este informe no midió ratios WCAG en el navegador.

## 5. Interactividad y valor para el usuario

Propuestas a partir de lo que ya existe (alertas, estadísticas, búsqueda, ledger). No están implementadas.

**Administrador**

- Sustituir la grilla de `Home/Index.cshtml` por los mismos conteos que ya calcula `StatisticsService` (stock crítico, órdenes `EnProceso` con plazo vencido, solicitudes `Pendiente`). Cada cifra enlaza a la cola real: `PlantasInventarioSolicitudes`, `Ordenes`, `PlantasInventario/Detalle`.
- Unificar la alerta de stock con `StockNivelHelper`, para que “crítico” en pantalla y en correo sea la misma regla.
- Historial ya existe en `StockMovement` y en `ActivityLog` (auditoría solo admin). Enlazar el material del detalle con sus movimientos, que hoy son otra pantalla.

**Encargado de bodega**

- Un solo inventario de su planta (el detalle), con el resumen de faltantes que Consultar ya calcula (`PlantasInventarioController.cs`, líneas 86-94).
- Atajo “entregar pendientes” desde el inicio hacia `PlantasInventarioOrdenes` filtrado a su planta.
- Confirmación de ajuste de stock que muestre el delta antes de guardar. Hoy el ajuste es un número nuevo en la fila (`Detalle.cshtml`, campos `NewStock`).

**Instructor**

- Inicio con sus fichas y solicitudes propias, no con accesos que el controlador rechaza.
- El buscador ya devuelve fichas y solicitudes (`BusquedaService.cs`, líneas 47-70), pero la orden abre `/Ordenes` (línea 44) y no el detalle de esa orden. Apuntar a `Ordenes/Detail/{id}` y a la ficha concreta.
- Flujo guiado: ficha → solicitar insumos → ver estado en `SolicitudesMaterial`. Esas tres pantallas ya existen y no se encadenan.

**Los tres**

- Buscador: funciona contra la base (`/api/busqueda`, `BusquedaController.cs` líneas 8-26) con tope de 5 por categoría. No busca plantas ni usuarios. Las órdenes no llevan al registro encontrado.
- Notificación en el icono de alertas del header (`_Layout.cshtml`, línea 169): hoy es un enlace, sin contador.
- Edición en línea del stock ya está en el detalle. Falta aviso claro cuando el servidor trunca un negativo a cero.

## 6. Calidad técnica

### 6.1 Datos y consultas

- `MrpService.SimulateAsync` llama `GetByIdAsync` por cada ítem de la receta (`MrpService.cs`, líneas 42-45): una consulta extra por material.
- `AlertService.BuildAlertEventsAsync` carga todos los materiales, todas las `MaterialRequest` y todas las órdenes (`AlertService.cs`, líneas 268, 282 y 294) y filtra en memoria.
- `Consultar`, cuando no hay planta elegida, trae los materiales y luego los reparte por planta en memoria (`PlantasInventarioController.cs`, líneas 80-88).
- Índices presentes y útiles: `Material.PlantaInventarioId` (línea 116), `StockMovement.FechaUtc` y `MaterialId` (líneas 699-700), `SolicitudMaterial.Estado` (línea 498), `ProductionOrder.OrderNumber` único (línea 242), `User.Email` único (línea 394). No hay índice compuesto de búsqueda por `Material.Name`/`Code` para el `Contains` del buscador (líneas 27-28 de `BusquedaService.cs`); con el volumen de un centro SENA el costo es bajo, y el `ToLower().Contains` no usa un índice B-tree normal.

### 6.2 Dónde vive la lógica

Los servicios concentran stock, MRP, aprobación y entrega. Los controladores de plantas e inventario arman ViewModels grandes y algo de filtrado (`PlantasInventarioController.Consultar` y `Detalle`). Las vistas de inventario aún contienen reglas de presentación duplicadas (formularios de `Inventario/Index.cshtml` y de `Detalle.cshtml`).

`InventarioController` conserva servicios en el constructor “para no romper los tests” y no los usa (líneas 17-27).

### 6.3 Pruebas, errores y configuración

`tests/Sipitex.Tests` cubre permisos, alcance por planta, MRP, solicitudes, órdenes, inventario, login y reportes: hay más de 400 métodos `[Fact]` en ese directorio. Quedan finos huecos respecto de este informe: la alerta de pendientes contra `SolicitudMaterial` (el test de alertas no sustituye esa fuente) y la carrera de dos ajustes.

Errores de arranque se registran con `LogCritical` y un mensaje en consola sin cadena de conexión (`Program.cs`, líneas 225-229). El fallo de login escribe el correo (`UserAccountService.cs`, líneas 49 y 55; `MemoryLoginAttemptGuard.cs`, líneas 42-55). El bloqueo es 5 intentos en 15 minutos, solo en memoria del proceso (líneas 6-10): en Render se reinicia con cada deploy.

La contraseña exige 6 caracteres y nada más (`PasswordRules.cs`, líneas 7-20). Los POST revisados de cuentas, plantas, órdenes, fichas y MRP llevan `[ValidateAntiForgeryToken]`; el logout también incluye el token (`_Layout.cshtml`, líneas 184-185).

Semilla y entorno: `ResolveSeedDemoData` (`Program.cs`, líneas 232-240). Producción lee `DATABASE_URL` y no usa el loopback de appsettings (documentado en `docs/DEPLOY.md`).

### 6.4 Seguridad ya resuelta en el código reciente

Fotos, outbox de correo y claves de cookie están en PostgreSQL (`docs/BACKUP.md` y migración `PersistenciaEnBase`). No dependen del disco efímero de Render. Este análisis no reabre ese diseño.

## 7. Datos de demostración

Hay dos siembras distintas.

**Demostración opcional** (`DbInitializer.cs`, a partir de la línea 36, solo si `seedDemoUsers` es verdadero):

| Código | Stock | Mínimo | Nivel según `StockNivelHelper` |
|---|---|---|---|
| mat1 Tela Jersey | 280 | 80 | Ok |
| mat2 Hilo Poliéster | 3200 | 500 | Ok |
| mat3 Cremallera | 95 | 40 | Ok |
| mat4 Forro Satín | 120 | 50 | Ok (estado físico `Regular`, no es nivel de stock) |

Órdenes `OP-001` (Camisa, 45/120, plazo 2025-04-15) y `OP-002` (Pantalón, 20/80, plazo 2025-04-20), ambas `EnProceso` (líneas 75-91). Fichas `FICHA-T1`, `FICHA-C2`, `FICHA-E3` (líneas 107-110). Con la fecha de hoy posterior a abril de 2025, esas órdenes cumplen la condición de “por vencer” y, por avance bajo 50 %, también la de “atrasada” (`AlertService.cs`, líneas 298-313). Ningún material de este bloque está en Crítico. La cremallera de la camisa (120 unidades pedidas por la orden contra 95 en stock) sí daría déficit en el MRP, pero la pantalla de nivel la muestra Ok porque 95 ≥ 40.

**Catálogo CMTC, siempre** (`CmtcBomCatalogSeed.cs`, comentario en las líneas 11 y 21; asignación en las líneas 170-171): cada material nuevo se crea con stock 0 y mínimo 0. `Classify` los marca `Critico`. La alerta de stock no los incluye (`Stock < MinStock` es falso). En producción, sin `SEED_DEMO_DATA`, el inventario visible es ese catálogo en cero: la proporción de “crítico” es el 100 % de esos insumos, y no representa una bodega con faltantes reales.

Cómo se apaga la demo: variable `SEED_DEMO_DATA` en `true`/`1`/`yes` fuerza la demo; si no está, manda `Seed:DemoUsers` (`Program.cs`, líneas 232-240). En `appsettings.json` ese flag está en falso (línea 6); `appsettings.Development.json` lo pone en verdadero. El CMTC no depende de ese flag.

Usuarios de demo (`admin@sipitex.test`, `instructor@sipitex.test`, `bodega@sipitex.test`) solo entran con esa misma bandera (`DbInitializer.cs`, líneas 115-120). El administrador de producción, si no hay ninguno, sale de `ADMIN_SEED_PASSWORD` y el log no imprime la clave (mismo archivo, método `EnsureProductionAdminAsync`).

## 8. Priorización y hoja de ruta

Impacto: alto = dato o permiso incorrecto en el uso diario. Esfuerzo: S = un módulo acotado, M = dos o tres pantallas, L = recorrido de varios flujos.

| Hallazgo | Impacto | Esfuerzo | Riesgo | Rol |
|---|---|---|---|---|
| Alerta de solicitudes lee `MaterialRequest` y apunta a Inventario, no a la cola vigente | Alto | M | Medio: hay que no perder historial legado | Encargado, administrador |
| Stock 0 y mínimo 0 se ve Crítico y no dispara alerta | Alto | S | Bajo | Encargado, administrador |
| Material creado fuera de la transacción de aprobación | Alto | S | Bajo | Encargado |
| Ajuste de stock sin concurrencia y truncado silencioso a cero | Alto | M | Medio | Encargado |
| Instructor consulta todo el inventario pese a la política escrita | Medio | S | Bajo | Instructor |
| Inicio muestra módulos que el encargado no puede abrir | Medio | S | Bajo | Encargado |
| Dos pantallas de inventario (Detalle y Consultar) más vistas muertas de `Inventario` | Medio | M | Medio: tests del controlador legado | Los tres |
| Migas y nombres planta/bodega/PlantaInventario | Medio | S | Bajo | Los tres |
| Buscador abre la lista, no el registro | Medio | S | Bajo | Los tres |
| MRP consulta el material una vez por ítem y no reserva | Medio | M | Medio | Administrador, instructor |
| Sin paginación en inventario y solicitudes | Medio | M | Bajo | Encargado |
| Contraseña de 6 caracteres, sin más reglas | Medio | S | Bajo | Administrador |
| Bloqueo de login solo en memoria | Bajo | M | Bajo | Todos |
| Correo de login en logs | Bajo | S | Bajo | Todos |
| Demo con plazos de 2025 y cero ítems críticos; CMTC todo en cero | Medio | S | Bajo | Quien hace la demo |
| Iconos repetidos | Bajo | S | Bajo | Todos |

### PRs propuestos, en orden

1. **Alertas sobre el flujo real.** Leer `SolicitudMaterial` pendiente. Alinear el umbral de stock con `StockNivelHelper`. Corregir el texto que cita Inventario. Pruebas de los tres casos (stock 0/mínimo 0, solicitud vigente, `MaterialRequest` vieja que no debe disparar sola).
2. **Transacción de aprobación.** Crear el material nuevo dentro de `ExecuteInTransactionAsync`. Prueba de fallo a mitad de resolución.
3. **Un inventario.** Instructor en solo lectura dentro de `Detalle` (o Consultar deja de ser otra lista). Quitar enlaces del inicio que el rol no puede usar. Redirigir y, en un PR siguiente, retirar vistas `Inventario/*` cuando los tests lo permitan.
4. **Ajuste de stock.** Rechazar negativos en lugar de truncar. Documentar en UI el delta. Valorar `xmin` de PostgreSQL solo en `Material.Stock` (cambio de esquema aditivo, no destructivo).
5. **Vocabulario y migas.** Un glosario en pantalla: planta de inventario. Migas clicables. Breadcrumb de órdenes de planta igual al menú.
6. **Buscador.** URL al detalle de orden, ficha y solicitud. Mantener el tope de 5.
7. **Inicio por rol.** Tarjetas con conteos ya disponibles en estadísticas y en el resumen de Consultar. Sin gráficos nuevos en el primer corte.
8. **Paginación** de materiales y de la cola de solicitudes (página de 25 o 50).
9. **Demo creíble.** Fechas de plazo relativas a “hoy”. Al menos un material bajo mínimo y uno en cero con mínimo mayor que cero, solo si `SEED_DEMO_DATA=true`. El CMTC en cero debe etiquetarse “pendiente de conteo”, no como crítico operativo, o la alerta y la pantalla deben tratar mínimo 0 como “sin umbral”.
10. **Contraseña y logs.** Subir la regla mínima (longitud y una clase de carácter) solo para altas nuevas. Dejar de escribir el correo completo en el log de intentos; conservar el conteo.

## 9. Resumen

SIPITEX ya separa plantas, órdenes, ficha técnica, MRP, solicitudes multi-ítem, entrega, reingreso y ledger. El menú de operación coincide en lo esencial con esos controladores. Las cinco acciones de mayor impacto son estas.

1. Hacer que las alertas miren `SolicitudMaterial` y el mismo criterio de stock que la pantalla, porque hoy el encargado puede no enterarse de la cola real ni del catálogo en cero.
2. Meter la creación de materiales de una aprobación en la misma transacción que el descuento, para no dejar insumos huérfanos.
3. Unificar Detalle y Consultar, y alinear el inicio con los permisos, para que cada rol vea una sola lista y ningún acceso que termina en denegado.
4. Rechazar el stock negativo y tratar el ajuste concurrente, porque dos encargados pueden pisarse el saldo.
5. Arreglar nombres (planta, no “bodega” y “PlantaInventario” a la vez) y el buscador que no abre el registro, que es lo que más se nota al usar el sistema en el taller.

El catálogo CMTC en stock 0 no sirve para demostrar un inventario crítico real. La demo de Camisa y Pantalón sí tiene cantidades coherentes con “hay tela”, y sus plazos de 2025 ya están vencidos.
