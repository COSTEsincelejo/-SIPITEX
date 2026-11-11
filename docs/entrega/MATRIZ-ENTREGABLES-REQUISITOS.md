# Matriz entregables ↔ requisitos — SIPITEX v1.0

**Numeración principal:** `docs/08-IEEE830-Especificacion.md` (alineada al código).  
**Columna “Nota vs 01-Requisitos / seed”:** diferencia con `docs/01-Requisitos.md` y seed `DbInitializer` (`EXTRACCION-DATOS-REPO.md` §1).  
Estado de cumplimiento orientativo según extracción + seed + HANDOVER (no inventa evidencias nuevas).

---

## RF (IEEE 830)

| ID IEEE | Módulo | Requisito (IEEE 830) | Prioridad | Estado entrega | Entregable / evidencia | Nota vs 01-Requisitos / seed |
|---------|--------|----------------------|-----------|----------------|------------------------|------------------------------|
| RF01 | Usuarios | Crear, editar y desactivar usuarios por rol | Alta | Cumple | `AccountController` Users/CRUD; `UserAccountService` | Texto similar en 01 RF01 |
| RF02 | Usuarios | Iniciar y cerrar sesión con cookies | Alta | Cumple | Login/Logout cookies `Program.cs` | **01 RF02 pide JWT** — supersedido |
| RF03 | Usuarios | Otorgar permisos extendidos desde el administrador | Alta | Cumple | `ExtendedPermissions` / claims `permiso` | En 01 no hay RF03 de permisos; 01 RF03 = entradas inventario |
| RF04 | Inventario | Registrar cualquier material (nombre, stock, mínimo, unidad) | Alta | Cumple | `PlantasInventarioController.AddMaterial` | ≈ 01 RF03 (entradas) |
| RF05 | Inventario | Consultar stock y filtrar por nivel | Alta | Cumple | Consultar/Index plantas; policy `PuedeConsultarInventario` | ≈ 01 RF04 |
| RF06 | Inventario | Actualizar estado físico (Bueno/Regular/Deteriorado) | Media | Cumple | `UpdateStatus` | ≈ 01 RF05 |
| RF07 | Inventario | Ajustar stock y fecha de última entrada | Media | Cumple | `AdjustStock` / import | En 01 no hay ID dedicado equivalente |
| RF08 | Salida | Solicitar material para una orden (crear material si no existe) | Alta | Cumple | `SolicitudesMaterialController` | ≈ 01 RF06 |
| RF09 | Salida | Aprobar o rechazar; al aprobar descontar stock | Alta | Cumple | `PlantasInventarioSolicitudesController.Resolve` | ≈ 01 RF07 |
| RF10 | Órdenes | Crear orden con cualquier nombre de producto | Alta | Cumple | `OrdenesController.Create` + policy | ≈ 01 RF09 |
| RF11 | Órdenes | Registrar avance y finalizar al alcanzar la meta | Alta | Cumple | MES / AddProduction / estados | ≈ 01 RF10 + RF15 (partes) |
| RF12 | MRP | Mantener BOM por producto | Alta | Cumple | `MrpController` BOM CRUD | 01 RF11/RF12 textos distintos (MRP auto / ficha↔proceso) |
| RF13 | MRP | Simular requerimiento neto frente al stock | Alta | Cumple (manual) | `MrpController.Simulate` | **01 RF11 “MRP automático”** — HANDOVER: no auto al crear OP |
| RF14 | Fichas | Asociar ficha a proceso, instructor y orden | Alta | Cumple / parcial UI | `FichasController`; `FichaInstructor` | ≈ 01 RF12–RF13; seed RF13 Parcial |
| RF15 | Producción | Registrar sesión diaria (ficha, unidades, observaciones) | Alta | Cumple | `FichasController.Register` | ≈ 01 RF14 |
| RF16 | Calidad | Registrar inspección; en reproceso exigir motivo y responsable | Media | Cumple (inspección genérica) | `CalidadController` / `QualityService` | **01/seed RF16 = criterios por tipo de prenda → Ausente.** IEEE RF16 ≠ ese texto. Ver fila destacada abajo. |
| RF17 | Reportes | Exportar inventario, órdenes, calidad y dashboard (PDF/Excel) | Alta | Cumple | `ReportesController` | 01 RF18 es reportes período/instructor (Parcial en seed) |
| RF18 | Reportes | Filtrar por día, semana, mes, año, instructor o ficha | Alta | Parcial / Cumple según filtros | `ReportFilterHelper` + reportes | Numeración distinta vs 01 |
| RF19 | Alertas | Preferencias por usuario y evaluación de eventos | Media | Cumple | `AlertasController` | En 01 RF19 = consumo materiales |
| RF20 | Estadísticas | Dashboard con KPIs y gráfico de avance | Alta | Cumple | `EstadisticasController` | Alineado con 01 RF20 |

### Destacado — criterios por prenda

| ID origen | Texto | Estado |
|-----------|-------|--------|
| **01-Requisitos / seed RF16** | Criterios diferenciales por tipo de prenda | **Ausente** (solo Aprobado/Reproceso genérico — extracción §1.3 / §9) |
| IEEE RF16 | Inspección + motivo/responsable en reproceso | Implementado (no cubre criterios por tipo de prenda) |

---

## RNF (IEEE 830)

| ID IEEE | Requisito | Estado | Nota vs 01 / seed / código |
|---------|-----------|--------|----------------------------|
| RNF01 | Tiempo de respuesta percibido &lt; 2 s en intranet | PENDIENTE evidencia | Mismo espíritu en 01; sin benchmark en repo |
| RNF02 | Sesión autenticada con expiración (docs: 8 h) | Cumple con TTL distinto | 01 pedía JWT; código default **20 min** (`SeguridadOptions`) |
| RNF03 | Control de acceso por rol y permisos extendidos | Cumple | 01: solo “por rol” |
| RNF04 | Acceso desde cualquier PC de la intranet | Parcial (ops) | Requiere servidor HTTP / despliegue |
| RNF05 | Interfaz responsiva (móvil y escritorio) | Cumple (sin test auto) | Sin prueba automatizada localizada |
| RNF06 | Código modular por capas y documentado | Cumple / Parcial docs | Capas Domain/Application/Infrastructure/Web |
| RNF07 | Despliegue reproducible con Docker Compose | Cumple | `Dockerfile` + `docker-compose.yml` |
| RNF08 | Integridad de datos con EF Core / PostgreSQL | Cumple / Parcial seed | Post-#97; seed aún marca Parcial |

---

## Requisitos solo en 01 / seed (sin ID IEEE equivalente 1:1)

| ID 01/seed | Texto corto | Estado seed / nota |
|------------|-------------|-------------------|
| RF08 | Salida trazada por orden | Parcial (seed); código con movimientos/ledger |
| RF11 | Cálculo MRP automático | Deuda HANDOVER #1 |
| RF16 | Criterios por tipo de prenda | **Ausente** |
| RF17 | Trazabilidad reproceso | Cumple (motivo/responsable) — en IEEE entra en RF16 |
| RF19 | Consumo de materiales | Cumple vía consumos/BOM — en IEEE no es RF19 |

---

## Módulos posteriores (fuera de RF01–RF20 originales)

Incluidos en alcance de entrega de código: plantas, solicitudes/órdenes de planta, grupos, consumos, costeo, actas, trazabilidad, auditoría, ayuda, privacidad — ver `ALCANCE-FINAL.md` e `01-Requisitos` §1.6.
