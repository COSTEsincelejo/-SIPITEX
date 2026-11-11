# Diagramas técnicos SIPITEX

Diagramas Mermaid derivados de `README`, `Program.cs`, `render.yaml`, `docker-compose.yml` y servicios de flujo. Sin inventar nodos no presentes en el código.

## C4 — Contexto

```mermaid
C4Context
    title SIPITEX — contexto del sistema
    Person(admin, "Administrador")
    Person(inst, "Instructor")
    Person(enc, "Encargado de bodega")
    Person(anon, "Verificador externo (QR acta)")
    System(sipitex, "SIPITEX", "ASP.NET Core MVC + PostgreSQL")
    System_Ext(email, "Proveedor de correo", "Brevo / Resend / SMTP / Outbox")
    System_Ext(browser, "Navegador", "HTML/CSS/JS estático")
    Rel(admin, sipitex, "HTTPS / cookies")
    Rel(inst, sipitex, "HTTPS / cookies")
    Rel(enc, sipitex, "HTTPS / cookies")
    Rel(anon, sipitex, "GET /Verificar/Acta (anónimo)")
    Rel(sipitex, email, "API HTTPS o SMTP")
    Rel(browser, sipitex, "UI MVC")
```

## C4 — Contenedores

```mermaid
C4Container
    title SIPITEX — contenedores
    Person(user, "Usuario autenticado")
    Container(web, "Sipitex.Web", "ASP.NET Core MVC", "Controllers, vistas, cookies, policies")
    Container(app, "Sipitex.Application", ".NET", "Servicios de negocio")
    Container(infra, "Sipitex.Infrastructure", ".NET", "EF Core, repositorios, PDF, correo")
    ContainerDb(pg, "PostgreSQL 16", "BD", "SipitexDbContext + DataProtectionKeys")
    Rel(user, web, "HTTPS")
    Rel(web, app, "DI / interfaces")
    Rel(web, infra, "DI")
    Rel(infra, app, "implementa contratos")
    Rel(infra, pg, "Npgsql / EF Core")
```

## Despliegue — Render (web + PostgreSQL)

Fuente: `render.yaml`, `docs/DEPLOY.md`, `Dockerfile`.

```mermaid
flowchart LR
  subgraph Internet
    U[Usuarios / navegador]
  end
  subgraph Render
    W[Web service sipitex<br/>runtime docker<br/>healthCheckPath /healthz<br/>branch main autoDeploy]
    DB[(PostgreSQL 16<br/>sipitex-db<br/>database sipitex)]
  end
  subgraph Externos
    M[Brevo / Resend<br/>Email__Provider]
  end
  U -->|HTTPS PORT inyectado| W
  W -->|ConnectionStrings__DefaultConnection| DB
  W -->|API correo| M
```

Compose local (`docker-compose.yml`): servicio `db` (`postgres:16`, volumen `sipitex-pgdata`) + `sipitex` (`8080:8080`, healthcheck `/healthz`).

## Secuencia — flujo completo OP → entrega → consumo → MES → calidad → PT → acta

```mermaid
sequenceDiagram
  actor Admin
  actor Instructor
  actor Encargado
  participant Ordenes as OrdenesController
  participant POS as ProductionOrderService
  participant OMS as OrderMaterialService
  participant MCS as MaterialConsumptionService
  participant PFS as ProductionFlowService
  participant QS as QualityService
  participant Actas as ActaMovimientoService
  participant PDF as ActaPdfService
  participant DB as PostgreSQL

  Admin->>Ordenes: Create (policy PuedeCrearOrdenes)
  Ordenes->>POS: crear OP + BomSnapshot
  POS->>DB: ProductionOrder + ProductionOrderBomSnapshot
  Admin->>Ordenes: Approve / etapas / ImportBomMaterials
  Ordenes->>OMS: requisitos ProductionOrderMaterialRequirement
  Encargado->>OMS: Deliver (PlantasInventarioOrdenes)
  OMS->>DB: Material.Stock - / StockMovement / QuantityDelivered
  Instructor->>MCS: Register consumo
  MCS->>DB: ConsumoMaterial (sin nuevo StockMovement)
  Instructor->>PFS: Start/Process/Send etapas MES
  PFS->>DB: ProductionOrderStage + movimientos / FinishedGood*
  Instructor->>QS: Create QualityRecord
  QS->>DB: QualityRecords
  Note over Actas,PDF: Acta ingreso/egreso + firma
  Admin->>Actas: Create / Firmar
  Actas->>PDF: Render QuestPDF + QR
  Actas->>DB: ActaMovimiento
```
