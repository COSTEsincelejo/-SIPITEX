# Fase 2 — Diseño del sistema (Cascada)

## 2.1 Arquitectura por capas

```mermaid
flowchart TB
    subgraph presentation [Capa de Presentación]
        Web[Sipitex.Web - MVC Controllers + Razor Views]
    end
    subgraph application [Capa de Aplicación]
        Services[Servicios de negocio]
        DTOs[DTOs y contratos IService]
    end
    subgraph domain [Capa de Dominio]
        Entities[Entidades y enums]
    end
    subgraph infrastructure [Capa de Infraestructura]
        EF[EF Core DbContext]
        Repo[Repositorios]
        DB[(SQLite sipitex.db)]
    end
    Web --> Services
    Web --> Repo
    Services --> Entities
    Services --> Repo
    Repo --> EF
    EF --> DB
```

## 2.2 Modelo de datos (ER simplificado)

```mermaid
erDiagram
    Material ||--o{ BomItem : compone
    Material ||--o{ MaterialRequest : solicita
    ProductionOrder ||--o{ MaterialRequest : origina
    ProductionOrder ||--o{ QualityRecord : inspecciona
    ProductionOrder ||--o{ Ficha : asigna
    BomItem }o--|| Material : usa
    User }o--o| Ficha : asignada
```

## 2.3 Servicios de aplicación

| Servicio | Responsabilidad |
|----------|-----------------|
| `IInventoryService` | Materiales, stock, solicitudes |
| `IProductionOrderService` | CRUD órdenes, consumo BOM |
| `IMrpService` | BOM y simulación MRP |
| `IFichaService` | Registro producción por ficha |
| `IQualityService` | Inspecciones |
| `IStatisticsService` | KPIs y gráficos |
| `IUserAccountService` | Autenticación y CRUD de usuarios |

## 2.4 Patrones aplicados

- **Repository + Unit of Work** — abstracción de persistencia  
- **DTO** — desacoplar entidades de la UI  
- **Dependency Injection** — registro en `Program.cs` / extensiones  

## 2.5 Entregable de fase

Diagramas, contratos de interfaces y esquema de BD → implementación (Fase 3).
