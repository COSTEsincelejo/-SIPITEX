# Especificación de Requisitos de Software (SRS)
## Norma IEEE 830-1998 — SIPITEX

| Campo | Valor |
|-------|--------|
| **Proyecto** | SIPITEX — Sistema Integrado de Aprendizaje, Producción e Inventario Textil |
| **Institución** | SENA CMTC · ADSO |
| **Versión** | 2.0 |
| **Fecha** | 2026-07-23 |
| **Estado** | Alineado con la implementación actual (ASP.NET Core MVC + EF Core + SQLite) |
| **Autores** | Equipo SIPITEX / COSTEsincelejo |

> **Nota:** El documento sigue la estructura recomendada por **IEEE Std 830-1998** (*IEEE Recommended Practice for Software Requirements Specifications*).

---

## 1. Introducción

### 1.1 Propósito

Definir los requisitos funcionales y no funcionales del sistema SIPITEX, servir de contrato entre stakeholders (instructores, bodega, administración del centro) y el equipo de desarrollo, y documentar el diseño de alto nivel mediante diagramas UML y modelo entidad-relación.

### 1.2 Alcance

SIPITEX gestiona, en entorno intranet del centro de formación:

- Inventario de materias primas (cualquier material).
- Órdenes de producción de **cualquier producto/prenda**.
- MRP / ficha técnica (BOM) configurable.
- Solicitudes de material para producción.
- Fichas de aprendices y registro de sesiones.
- Control de calidad (aprobado / reproceso).
- Reportes PDF/Excel (diario, semanal, mensual, anual, por instructor y por ficha).
- Alertas por correo (SMTP o outbox de demostración).
- Autenticación por roles y permisos extendidos otorgados por el administrador.

**Fuera de alcance (v2.0):** facturación, nómina, integración con ERP externo, app móvil nativa.

### 1.3 Definiciones, acrónimos y abreviaturas

| Término | Definición |
|---------|------------|
| **BOM** | *Bill of Materials* — lista de materiales por unidad de producto |
| **MRP** | *Material Requirements Planning* — cálculo de requerimiento neto |
| **RF** | Requisito funcional |
| **RNF** | Requisito no funcional |
| **Ficha** | Grupo / proceso de formación asociado a un instructor |
| **SRS** | *Software Requirements Specification* |

### 1.4 Referencias

- IEEE Std 830-1998.
- Documentación interna: `docs/01-Requisitos.md` … `docs/05-Despliegue.md`.
- Código fuente: repositorio `-SIPITEX` (ramas `main` / `cursor/ui-responsive-b7ea`).

### 1.5 Panorama

Este SRS describe el *qué* del sistema. La arquitectura por capas, despliegue Docker y pruebas se detallan en los documentos de fases 2–5.

---

## 2. Descripción general

### 2.1 Perspectiva del producto

Aplicación web monolítica en capas (Clean Architecture simplificada):

```
Sipitex.Web (MVC + Razor + Cookies)
        ↓
Sipitex.Application (servicios, DTOs)
        ↓
Sipitex.Domain (entidades, enums)
        ↑
Sipitex.Infrastructure (EF Core, SQLite, reportes, correo)
```

### 2.2 Funciones del producto (resumen)

1. Autenticar usuarios y autorizar por rol/permiso.
2. Registrar y consultar materiales con niveles Agotado / Por agotarse / Normal.
3. Crear órdenes para cualquier producto.
4. Mantener BOM y simular MRP.
5. Solicitar / aprobar / rechazar salidas de bodega.
6. Registrar producción por ficha y avance de orden.
7. Registrar inspecciones de calidad.
8. Generar reportes filtrables y alertas.

### 2.3 Características de los usuarios

| Actor | Perfil | Interacción principal |
|-------|--------|------------------------|
| Administrador | Gestión del sistema | Usuarios, permisos, órdenes, reportes, alertas |
| Bodeguero | Almacén | Stock, estado físico, aprobación de solicitudes |
| Instructor | Formación / línea | Solicitudes, sesiones de producción (y funciones admin si se otorgan) |

### 2.4 Restricciones

- Base de datos SQLite en desarrollo/intranet (archivo `sipitex.db`).
- Autenticación por cookies (no JWT en la implementación actual).
- Navegadores modernos; UI responsiva.
- Despliegue opcional con Docker Compose.

### 2.5 Supuestos y dependencias

- Red local disponible.
- Usuarios semilla de demo: `admin@sipitex.test` / `Admin123!` (y roles instructor/bodega).
- Para alertas reales se configura SMTP; si no, se usa carpeta `email-outbox/`.

---

## 3. Requisitos específicos

### 3.1 Requisitos funcionales

| ID | Módulo | Descripción | Prioridad |
|----|--------|-------------|-----------|
| RF01 | Usuarios | CRUD de usuarios por rol (Admin / Instructor / Bodeguero) | Alta |
| RF02 | Usuarios | Autenticación con sesión (cookies) y cierre de sesión | Alta |
| RF03 | Usuarios | Permisos extendidos otorgados por el administrador | Alta |
| RF04 | Inventario | Registrar cualquier material (nombre, stock, mínimo, unidad) | Alta |
| RF05 | Inventario | Consultar stock y filtrar por Agotado / Por agotarse / Normal | Alta |
| RF06 | Inventario | Actualizar estado físico (Bueno / Regular / Deteriorado) | Media |
| RF07 | Inventario | Ajustar stock y fecha de última entrada | Media |
| RF08 | Salida | Solicitar cualquier material para una orden (crear material si no existe) | Alta |
| RF09 | Salida | Aprobar / rechazar solicitud y descontar stock al aprobar | Alta |
| RF10 | Órdenes | Crear orden con **cualquier nombre de producto** | Alta |
| RF11 | Órdenes | Registrar avance de producción y finalizar al alcanzar meta | Alta |
| RF12 | MRP | Mantener BOM por producto; material libre (texto) | Alta |
| RF13 | MRP | Simular requerimiento neto vs stock disponible | Alta |
| RF14 | Fichas | Asociar ficha a proceso / instructor / orden | Alta |
| RF15 | Producción | Registrar sesión diaria: ficha, unidades, observaciones | Alta |
| RF16 | Calidad | Registrar inspección; en reproceso exigir motivo y responsable | Media |
| RF17 | Reportes | Exportar inventario, órdenes, calidad y dashboard (PDF/Excel) | Alta |
| RF18 | Reportes | Reporte filtrado: diario, semanal, mensual, anual, instructor, ficha | Alta |
| RF19 | Alertas | Preferencias por usuario y evaluación (stock bajo, órdenes, etc.) | Media |
| RF20 | Estadísticas | Dashboard KPI y gráfico de avance | Alta |

### 3.2 Requisitos no funcionales

| ID | Descripción |
|----|-------------|
| RNF01 | Tiempo de respuesta percibido &lt; 2 s en intranet para pantallas habituales |
| RNF02 | Sesión autenticada con expiración (8 h) |
| RNF03 | Control de acceso por rol y permisos extendidos |
| RNF04 | Acceso desde PCs de la intranet del centro |
| RNF05 | Interfaz responsiva (móvil / escritorio) |
| RNF06 | Código modular por capas y documentado |
| RNF07 | Despliegue reproducible (Docker Compose opcional) |
| RNF08 | Integridad referencial y transacciones vía EF Core / SQLite |

### 3.3 Interfaces externas

- **Usuario:** navegador web (HTML/CSS/JS).
- **Hardware:** servidor HTTP / contenedor Docker.
- **Software:** .NET 10, EF Core, QuestPDF, ClosedXML, MailKit.
- **Comunicaciones:** HTTP(S) local; SMTP opcional para alertas.

---

## 4. Diagramas

### 4.1 Diagrama de casos de uso

```mermaid
flowchart LR
  subgraph Actores
    A[Administrador]
    B[Bodeguero]
    I[Instructor]
  end

  subgraph Sistema["SIPITEX"]
    UC1((Iniciar sesión))
    UC2((Gestionar usuarios y permisos))
    UC3((Registrar / ajustar materiales))
    UC4((Consultar stock y niveles))
    UC5((Solicitar material))
    UC6((Aprobar / rechazar solicitud))
    UC7((Crear orden de producción))
    UC8((Registrar producción))
    UC9((Gestionar BOM / MRP))
    UC10((Registrar calidad))
    UC11((Descargar reportes))
    UC12((Configurar / evaluar alertas))
  end

  A --> UC1
  B --> UC1
  I --> UC1
  A --> UC2
  A --> UC3
  B --> UC3
  I -.->|si permiso| UC3
  A --> UC4
  B --> UC4
  I --> UC4
  I --> UC5
  A --> UC5
  B --> UC6
  A --> UC6
  A --> UC7
  I --> UC8
  A --> UC8
  A --> UC9
  B --> UC9
  A --> UC10
  I --> UC10
  A --> UC11
  B --> UC11
  I --> UC11
  A --> UC12
```

#### Matriz actor ↔ caso de uso

| Caso de uso | Admin | Bodeguero | Instructor |
|-------------|:-----:|:---------:|:----------:|
| Iniciar sesión | ✓ | ✓ | ✓ |
| Gestionar usuarios/permisos | ✓ | | |
| Registrar materiales | ✓ | ✓ | permiso |
| Consultar stock | ✓ | ✓ | ✓ |
| Solicitar material | ✓ | | ✓ |
| Aprobar/rechazar | ✓ | ✓ | permiso |
| Crear orden | ✓ | | |
| Registrar producción | ✓ | | ✓ |
| BOM / MRP | ✓ | ✓ | permiso |
| Calidad | ✓ | | ✓ |
| Reportes | ✓ | ✓ | ✓ |
| Alertas | ✓ | parcial | parcial |

---

### 4.2 Diagrama de clases (dominio)

```mermaid
classDiagram
  direction TB

  class User {
    +int Id
    +string Nombre
    +string Email
    +string PasswordHash
    +string Rol
    +string PermisosExtendidos
    +bool IsActive
    +int? FichaAsignadaId
  }

  class Material {
    +int Id
    +string Code
    +string Name
    +MaterialUnit Unit
    +decimal Stock
    +decimal MinStock
    +MaterialStatus Status
    +DateOnly LastEntryDate
  }

  class BomItem {
    +int Id
    +string ProductName
    +int MaterialId
    +decimal QuantityPerUnit
    +MaterialUnit Unit
  }

  class ProductionOrder {
    +int Id
    +string OrderNumber
    +string ProductName
    +int TotalQuantity
    +int ProducedQuantity
    +OrderStatus Status
    +DateOnly Deadline
  }

  class MaterialRequest {
    +int Id
    +int MaterialId
    +int ProductionOrderId
    +decimal Quantity
    +RequestStatus Status
    +DateTime CreatedAt
  }

  class Ficha {
    +int Id
    +string FichaCode
    +string ProcessName
    +string InstructorName
    +int? ProductionOrderId
  }

  class ProductionSession {
    +int Id
    +int FichaId
    +int ProductionOrderId
    +int Units
    +string Observations
    +DateTime SessionDate
  }

  class QualityRecord {
    +int Id
    +int ProductionOrderId
    +int UnitsInspected
    +QualityResult Result
    +DateOnly InspectionDate
    +string? MotivoReproceso
    +string? Responsable
  }

  class AlertPreference {
    +int Id
    +int UserId
    +AlertType AlertType
    +bool Enabled
  }

  class AlertDelivery {
    +int Id
    +int UserId
    +AlertType AlertType
    +string Subject
    +string Body
    +DateTime SentAt
    +string Channel
  }

  User "0..1" --> "0..1" Ficha : FichaAsignada
  Material "1" --> "*" BomItem : compone
  Material "1" --> "*" MaterialRequest : solicita
  ProductionOrder "1" --> "*" MaterialRequest : origina
  ProductionOrder "1" --> "*" QualityRecord : inspecciona
  ProductionOrder "1" --> "*" Ficha : asigna
  Ficha "1" --> "*" ProductionSession : registra
  ProductionOrder "1" --> "*" ProductionSession : avanza
  User "1" --> "*" AlertPreference : configura
  User "1" --> "*" AlertDelivery : recibe
```

#### Capas de aplicación (vista lógica)

```mermaid
classDiagram
  direction LR
  class InventarioController
  class OrdenesController
  class MrpController
  class ReportesController
  class IInventoryService
  class IProductionOrderService
  class IMrpService
  class IReportService
  class IMaterialRepository
  class SipitexDbContext

  InventarioController --> IInventoryService
  OrdenesController --> IProductionOrderService
  MrpController --> IMrpService
  ReportesController --> IReportService
  IInventoryService --> IMaterialRepository
  IMrpService --> IMaterialRepository
  IMaterialRepository --> SipitexDbContext
```

---

### 4.3 Diagramas de secuencia

#### 4.3.1 Login

```mermaid
sequenceDiagram
  actor U as Usuario
  participant V as Account/Login
  participant S as UserAccountService
  participant R as UserRepository
  participant DB as SQLite

  U->>V: POST email + password
  V->>S: AuthenticateAsync
  S->>R: GetByEmailAsync
  R->>DB: SELECT Users
  DB-->>R: User
  R-->>S: User
  S->>S: Verificar PasswordHash
  S-->>V: User / null
  alt credenciales válidas
    V->>V: Emitir claims (rol + permisos)
    V-->>U: Cookie + redirect Inventario
  else inválidas
    V-->>U: "Credenciales inválidas"
  end
```

#### 4.3.2 Solicitar material (cualquier nombre)

```mermaid
sequenceDiagram
  actor I as Instructor
  participant V as InventarioController
  participant S as InventoryService
  participant MR as MaterialRepository
  participant RR as RequestRepository
  participant DB as SQLite

  I->>V: POST MaterialName, Orden, Cantidad
  V->>S: CreateRequestAsync
  S->>MR: GetByNameAsync(MaterialName)
  alt material no existe
    S->>MR: AddAsync(nuevo Material stock 0)
    MR->>DB: INSERT Materials
  end
  S->>RR: AddAsync(MaterialRequest Pendiente)
  RR->>DB: INSERT MaterialRequests
  S-->>V: Ok
  V-->>I: Toast "Solicitud creada"
```

#### 4.3.3 Aprobar solicitud y descontar stock

```mermaid
sequenceDiagram
  actor B as Bodeguero
  participant V as InventarioController
  participant S as InventoryService
  participant DB as SQLite

  B->>V: POST ApproveRequest(id)
  V->>S: ApproveRequestAsync
  S->>S: Validar Pendiente y stock >= cantidad
  alt stock insuficiente
    S-->>V: Fail
    V-->>B: Error
  else ok
    S->>S: Stock -= cantidad; Status = Aprobada
    S->>DB: UPDATE Materials, MaterialRequests
    S-->>V: Ok
    V-->>B: "Solicitud aprobada"
  end
```

#### 4.3.4 Crear orden de cualquier producto

```mermaid
sequenceDiagram
  actor A as Administrador
  participant V as OrdenesController
  participant S as ProductionOrderService
  participant BOM as BomRepository
  participant OR as OrderRepository
  participant DB as SQLite

  A->>V: POST ProductName libre, Cantidad, Deadline
  V->>S: CreateOrderAsync
  S->>BOM: GetByProductAsync(nombre)
  Note over S: Si no hay BOM, igual se crea la orden
  S->>OR: AddAsync(ProductionOrder)
  OR->>DB: INSERT ProductionOrders
  S-->>V: Ok + mensaje BOM/sin BOM
  V-->>A: Redirect listado órdenes
```

#### 4.3.5 Descargar reporte filtrado

```mermaid
sequenceDiagram
  actor U as Usuario
  participant V as ReportesController
  participant R as ReportService
  participant DB as SQLite

  U->>V: GET Filtrado(period, date/year/month, instructor, fichaId, format)
  V->>R: ExportFilteredAsync
  R->>R: ResolvePeriod (día/semana/mes/año)
  R->>DB: Materials, Sessions, Quality, Orders, Fichas
  R->>R: Filtrar por instructor / ficha
  alt format = pdf
    R->>R: QuestPDF GeneratePdf
  else excel
    R->>R: ClosedXML workbook
  end
  R-->>V: ReportFileDto
  V-->>U: Descarga archivo
```

#### 4.3.6 Agregar material libre al BOM (MRP)

```mermaid
sequenceDiagram
  actor A as Admin/Bodega
  participant V as MrpController
  participant S as MrpService
  participant MR as MaterialRepository
  participant BR as BomRepository
  participant DB as SQLite

  A->>V: POST ProductName, MaterialName, Qty, Unit
  V->>S: AddBomItemAsync
  S->>MR: GetByNameAsync
  alt no existe
    S->>MR: AddAsync(Material nuevo)
    MR->>DB: INSERT Materials
  end
  S->>BR: AddAsync(BomItem)
  BR->>DB: INSERT BomItems
  S-->>V: Ok
  V-->>A: Ficha actualizada
```

---

### 4.4 Diagrama entidad-relación (ER)

```mermaid
erDiagram
  USERS ||--o| FICHAS : "ficha_asignada"
  USERS ||--o{ ALERT_PREFERENCES : configura
  USERS ||--o{ ALERT_DELIVERIES : recibe

  MATERIALS ||--o{ BOM_ITEMS : "usado_en"
  MATERIALS ||--o{ MATERIAL_REQUESTS : "pedido_en"

  PRODUCTION_ORDERS ||--o{ MATERIAL_REQUESTS : origina
  PRODUCTION_ORDERS ||--o{ QUALITY_RECORDS : inspecciona
  PRODUCTION_ORDERS ||--o{ FICHAS : asigna
  PRODUCTION_ORDERS ||--o{ PRODUCTION_SESSIONS : avanza

  FICHAS ||--o{ PRODUCTION_SESSIONS : registra

  USERS {
    int Id PK
    string Nombre
    string Email UK
    string PasswordHash
    string Rol
    string PermisosExtendidos
    bool IsActive
    int FichaAsignadaId FK
  }

  MATERIALS {
    int Id PK
    string Code
    string Name
    int Unit
    decimal Stock
    decimal MinStock
    int Status
    date LastEntryDate
  }

  BOM_ITEMS {
    int Id PK
    string ProductName
    int MaterialId FK
    decimal QuantityPerUnit
    int Unit
  }

  PRODUCTION_ORDERS {
    int Id PK
    string OrderNumber UK
    string ProductName
    int TotalQuantity
    int ProducedQuantity
    int Status
    date Deadline
  }

  MATERIAL_REQUESTS {
    int Id PK
    int MaterialId FK
    int ProductionOrderId FK
    decimal Quantity
    int Status
    datetime CreatedAt
  }

  FICHAS {
    int Id PK
    string FichaCode
    string ProcessName
    string InstructorName
    int ProductionOrderId FK
  }

  PRODUCTION_SESSIONS {
    int Id PK
    int FichaId FK
    int ProductionOrderId FK
    int Units
    string Observations
    datetime SessionDate
  }

  QUALITY_RECORDS {
    int Id PK
    int ProductionOrderId FK
    int UnitsInspected
    int Result
    date InspectionDate
    string MotivoReproceso
    string Responsable
  }

  ALERT_PREFERENCES {
    int Id PK
    int UserId FK
    int AlertType
    bool Enabled
  }

  ALERT_DELIVERIES {
    int Id PK
    int UserId FK
    int AlertType
    string Subject
    string Body
    datetime SentAt
    string Channel
  }
```

---

## 5. Apéndices

### 5.1 Credenciales de demostración

| Rol | Correo | Contraseña |
|-----|--------|------------|
| Administrador | `admin@sipitex.test` | `Admin123!` |
| Instructor | `instructor@sipitex.test` | `Instructor123!` |
| Bodeguero | `bodega@sipitex.test` | `Bodega123!` |

### 5.2 Ejecución local

```bash
cd /workspaces/-SIPITEX   # o raíz del repo
dotnet watch run --project src/Sipitex.Web
```

### 5.3 Historial de versiones del SRS

| Versión | Fecha | Cambios |
|---------|-------|---------|
| 1.0 | 2025 | Requisitos iniciales RF01–RF20 |
| 2.0 | 2026-07-23 | Auth cookies, permisos, productos/materiales libres, reportes filtrables, diagramas UML/ER actualizados |

---

*Fin del documento IEEE 830 — SIPITEX v2.0*
