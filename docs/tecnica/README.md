# Documentación técnica SIPITEX

Índice de artefactos bajo `docs/tecnica/`. Contenido basado **solo** en código y docs factuales del repo (`EXTRACCION-DATOS-REPO.md`, README, HANDOVER, `Program.cs`, capas `src/`, etc.). Lo no verificado: PENDIENTE.

| Documento | Contenido |
|-----------|-----------|
| [ARQUITECTURA.md](ARQUITECTURA.md) | Capas Domain/Application/Infrastructure/Web, módulos, flujo OP→…→acta, decisiones |
| [ADRs/](ADRs/) | Decisiones cortas 0001–0008 |
| [DIAGRAMAS.md](DIAGRAMAS.md) | Mermaid C4, despliegue Render, secuencia flujo |
| [MODELO-DATOS.md](MODELO-DATOS.md) | ER Mermaid + descripción de entidades |
| [RUTAS.md](RUTAS.md) | Controllers / actions / roles y policies |
| [DISENO-MODULOS.md](DISENO-MODULOS.md) | Por módulo: entidades, servicios, UI |
| [CONTRIBUIR.md](CONTRIBUIR.md) | Ramas, PRs, `dotnet test`, migraciones EF |
| [DEPENDENCIAS.md](DEPENDENCIAS.md) | NuGet top-level, wwwroot/lib, licencias |
| [sbom.json](sbom.json) | SBOM CycloneDX generado con `dotnet CycloneDX` |

## ADRs

| ADR | Título |
|-----|--------|
| [0001](ADRs/0001-aspnet-mvc-capas.md) | ASP.NET MVC por capas |
| [0002](ADRs/0002-postgresql-efcore.md) | PostgreSQL + EF Core |
| [0003](ADRs/0003-plantas-inventario.md) | Plantas de inventario |
| [0004](ADRs/0004-entrega-consumo-stockmovement.md) | Entrega vs consumo / StockMovement |
| [0005](ADRs/0005-bom-snapshot-ordenes.md) | BOM snapshot por OP |
| [0006](ADRs/0006-actas-pdf-verificacion.md) | Actas PDF + verificación |
| [0007](ADRs/0007-seed-condicionado-entorno.md) | Seed condicionado |
| [0008](ADRs/0008-cookies-no-jwt.md) | Cookies (no JWT) |

## Relacionados fuera de esta carpeta

- Operación: [`../DEPLOY.md`](../DEPLOY.md), [`../BACKUP.md`](../BACKUP.md), [`../HANDOVER.md`](../HANDOVER.md)
- Extracción factual: [`../entrega/EXTRACCION-DATOS-REPO.md`](../entrega/EXTRACCION-DATOS-REPO.md)
- Diagramas PNG legacy: [`../diagramas/`](../diagramas/)
