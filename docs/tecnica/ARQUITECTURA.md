# Arquitectura SIPITEX

**Fuente:** código en `src/`, `README.md`, `docs/HANDOVER.md`, `docs/entrega/EXTRACCION-DATOS-REPO.md`, `Program.cs`, `SipitexDbContext`, `render.yaml`, `docker-compose.yml`.  
**Sin inventar.** Lo no hallado se marca PENDIENTE.

## Capas

Solución .NET 10 (`Sipitex.slnx`) con cuatro proyectos:

| Capa | Proyecto | Responsabilidad (según README / código) |
|------|----------|----------------------------------------|
| Domain | `Sipitex.Domain` | Entidades y enums; **sin** paquetes NuGet |
| Application | `Sipitex.Application` | Servicios de negocio, DTOs, contratos (`Interfaces`) |
| Infrastructure | `Sipitex.Infrastructure` | EF Core + PostgreSQL (`SipitexDbContext`), repositorios, correo, reportes/PDF (`ActaPdfService`), importación |
| Web | `Sipitex.Web` | ASP.NET Core MVC: controllers, vistas, auth cookies, pipeline HTTP (`Program.cs`) |

Dependencias (README):

```
Web → Application → Domain
Web → Infrastructure → Application → Domain
```

Registro DI: `AddApplicationServices()` en `src/Sipitex.Web/ApplicationServiceExtensions.cs`; `AddInfrastructure()` en `src/Sipitex.Infrastructure/DependencyInjection.cs`.

## Módulos (rutas UI)

Tabla del README (coincide con controllers):

| Módulo | Ruta |
|--------|------|
| Órdenes / MES | `/Ordenes` |
| MRP / BOM | `/Mrp` |
| Fichas (grupos SENA) | `/Fichas` |
| Plantas de inventario | `/PlantasInventario` |
| Importar inventario | `/Inventario/Importar` |
| Solicitudes de planta | `/PlantasInventarioSolicitudes` |
| Órdenes de planta (entrega/reingreso) | `/PlantasInventarioOrdenes` |
| Solicitudes (instructor) | `/SolicitudesMaterial` |
| Grupos de confección | `/GruposConfeccion` |
| Consumos | `/Consumos` |
| Costeo | `/Costos` |
| Actas | `/Actas` |
| Verificación pública de acta | `/Verificar/Acta` |
| Trazabilidad | `/Trazabilidad` |
| Calidad | `/Calidad` |
| Auditoría | `/Auditoria` |
| Estadísticas / Reportes / Alertas | `/Estadisticas`, `/Reportes`, `/Alertas` |
| Usuarios | `/Account/Users` |

## Flujo principal: OP → entrega → consumo → MES → calidad → PT → acta

Orden de negocio observado en servicios (no es un único método orquestador):

1. **Orden de producción (OP)** — `ProductionOrderService` / `OrdenesController.Create` (policy `PuedeCrearOrdenes`). Estado inicial `Pendiente` hasta aprobación admin. Al crear se congela BOM en `ProductionOrderBomSnapshot`.
2. **Requisitos de material** — `OrderMaterialService.AddMaterial` / `ImportBomMaterials`: líneas `ProductionOrderMaterialRequirement`. MRP **no** corre solo al crear OP (HANDOVER #1): simular en `/Mrp` e importar.
3. **Entrega desde planta** — `PlantasInventarioOrdenesController.Deliver` → `OrderMaterialService`: descuenta `Material.Stock` y escribe `StockMovement`. Es el **único** descuento de stock de insumos de la orden (`MaterialConsumptionService` lo documenta en comentario).
4. **Consumo** — `MaterialConsumptionService.RegisterAsync`: anota `ConsumoMaterial` ligado a la línea entregada; **no** crea otro `StockMovement` ni vuelve a bajar stock.
5. **MES** — `ProductionFlowService`: etapas por defecto Trazo → Corte → Confección → Control de Calidad → Terminado; WIP en `ProductionOrderStage` / movimientos append-only. Comentario del servicio: flujo aditivo; no altera consumo BOM ni materiales de planta.
6. **Calidad** — `QualityService` / `CalidadController`: `QualityRecord` (Aprobado / Reproceso / …).
7. **Producto terminado (PT)** — `FinishedGoodStock` / `FinishedGoodMovement` vía flujo MES / reingreso parcial. HANDOVER #2: sin pantalla propia en el menú.
8. **Acta** — `ActaMovimientoService` + PDF `ActaPdfService` (QuestPDF + QRCoder); verificación anónima `VerificarController.Acta` con rate limit.

Rutas paralelas: `SolicitudMaterial` (multi-ítem a planta) con aprobación y `EntregaMaterial` 1:1 — distinto del flujo de materiales de OP.

## Decisiones clave (ver ADRs)

| Tema | Decisión en código |
|------|-------------------|
| Presentación | ASP.NET Core MVC por capas (ADR 0001) |
| Persistencia | PostgreSQL 16 + EF Core (ADR 0002) |
| Inventario | Stock por `PlantaInventario` + `Material.PlantaInventarioId` (ADR 0003) |
| Stock vs consumo | Entrega descuenta; consumo solo anota (ADR 0004) |
| BOM de orden | Snapshot al crear OP (ADR 0005) |
| Actas | PDF + QR + `/Verificar/Acta` (ADR 0006) |
| Seed | Condicionado a `SEED_DEMO_DATA` / `Seed:DemoUsers` (ADR 0007) |
| Auth | Cookies `sipitex.auth`, no JWT (ADR 0008; RF02 en `01-Requisitos` decía JWT) |

## Arranque (`Program.cs`)

- Cadena PostgreSQL: `DATABASE_URL` o `ConnectionStrings__DefaultConnection` (Production rechaza localhost).
- Cookie auth + Data Protection keys en `DataProtectionKeys` (EF / PG).
- Migraciones + seed: `DbInitializer.InitializeAsync` al arrancar; fallo → exit 1.
- Health: `/healthz` (ok sin BD), `/health` (EF), `/version`.
- Hosted: `AlertEvaluationHostedService`.

## Despliegue

- **Render:** `render.yaml` — web Docker + DB `sipitex-db` PostgreSQL 16.
- **Local:** `docker-compose.yml` — servicios `db` (postgres:16) + `sipitex` (puerto 8080).
