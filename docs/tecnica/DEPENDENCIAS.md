# Dependencias

Versiones **top-level** leídas de los `.csproj` (también en `docs/entrega/EXTRACCION-DATOS-REPO.md` §7–8).  
Licencias: SBOM CycloneDX (`sbom.json`) y/o `.nuspec` / `LICENSE*` en caché NuGet o `wwwroot/lib`. Si no hay evidencia local: PENDIENTE / consultar nuget.org.

Target framework de los proyectos de app: `net10.0`.

## Sipitex.Web

| Package | Versión | Licencia |
|---------|---------|----------|
| Microsoft.AspNetCore.DataProtection.EntityFrameworkCore | 10.0.9 | MIT (SBOM) |
| Microsoft.EntityFrameworkCore.Design | 10.0.9 | MIT (SBOM) |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 10.0.9 | MIT (SBOM) |
| Serilog.AspNetCore | 10.0.0 | Apache-2.0 (SBOM) |
| Serilog.Formatting.Compact | 3.0.0 | Apache-2.0 (SBOM) |
| Serilog.Sinks.Console | 6.1.1 | Apache-2.0 (SBOM) |

## Sipitex.Infrastructure

| Package | Versión | Licencia |
|---------|---------|----------|
| ClosedXML | 0.105.0 | MIT (SBOM) |
| DocumentFormat.OpenXml | 3.3.0 | MIT (SBOM) |
| MailKit | 4.14.1 | MIT (SBOM) |
| Microsoft.EntityFrameworkCore | 10.0.9 | MIT (SBOM) |
| Microsoft.EntityFrameworkCore.Relational | 10.0.9 | MIT (SBOM) |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.0 | PostgreSQL (nuspec/SBOM) |
| QRCoder | 1.6.0 | MIT (nuspec/SBOM) |
| QuestPDF | 2025.7.1 | Community / MIT condicional (`PackageLicense.md` en paquete; código fija `LicenseType.Community`) |
| Microsoft.Extensions.Http | 10.0.9 | MIT (SBOM) |
| Microsoft.Extensions.Options.ConfigurationExtensions | 10.0.9 | MIT (SBOM) |
| Microsoft.AspNetCore.DataProtection.EntityFrameworkCore | 10.0.9 | MIT (SBOM) |

## Sipitex.Application

| Package | Versión | Licencia |
|---------|---------|----------|
| Microsoft.Extensions.Logging.Abstractions | 10.0.0 | MIT (SBOM) |
| Microsoft.Extensions.Options | 10.0.0 | MIT (SBOM) |

## Sipitex.Domain

Sin PackageReference.

## tests/Sipitex.Tests

| Package | Versión | Licencia |
|---------|---------|----------|
| coverlet.collector | 6.0.4 | MIT (nuspec) |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.0 | MIT (nuspec) |
| Npgsql | 10.0.0 | PostgreSQL (SBOM) |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.0 | PostgreSQL |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.9 | MIT (nuspec) |
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT (nuspec) |
| Moq | 4.20.72 | BSD-3-Clause (nuspec) |
| Serilog | 4.3.0 | Apache-2.0 (SBOM) |
| xunit | 2.9.3 | Apache-2.0 (nuspec) |
| xunit.runner.visualstudio | 3.1.4 | Apache-2.0 (nuspec) |

## tools/Sipitex.DataMigration

| Package | Versión | Licencia |
|---------|---------|----------|
| Microsoft.Data.Sqlite | 10.0.9 | MIT (nuspec) |
| Npgsql | 10.0.0 | PostgreSQL |

## Front estático (`src/Sipitex.Web/wwwroot/lib`)

No hay `package.json` en la raíz de la app ni en `src/`.

| Librería | Versión en dist | Licencia en árbol |
|----------|-----------------|-------------------|
| bootstrap | 5.3.3 (`bootstrap.min.js`) | MIT (`LICENSE`) |
| jquery | 3.7.1 | MIT (`LICENSE.txt`) |
| jquery-validation | 1.21.0 | MIT (`LICENSE.md`) |
| jquery-validation-unobtrusive | (vendor .NET Foundation; ver `LICENSE.txt`) | MIT (`LICENSE.txt`) |

## npm solo documentación

| Ubicación | Package | Versión lock |
|-----------|---------|--------------|
| `docs/diagramas/package.json` | `@mermaid-js/mermaid-cli` (dev) | 11.16.0 |

## SBOM

Generado con `dotnet CycloneDX` → [`sbom.json`](sbom.json) (CycloneDX 1.7, proyectos de producción; tests excluidos con `-t`).  
Transitives y licencias adicionales: consultar ese archivo o nuget.org.
