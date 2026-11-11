# Licencias y propiedad intelectual — SIPITEX

## 1. Proyecto SIPITEX

| Ítem | Estado |
|------|--------|
| Licencia del código SIPITEX (repo) | **PENDIENTE: definir con la institución** (no hay archivo `LICENSE` en la raíz) |
| Propiedad intelectual / cesión / uso institucional | **PENDIENTE: definir con la institución** |

Hasta esa definición, no asumir MIT/Apache/propiedad exclusiva en documentación externa.

## 2. Dependencias NuGet (directas)

Versiones según `.csproj` al 2026-11-11. Licencias típicas del paquete (verificar en NuGet.org / NOTICE del paquete al redistribuir).

### Infrastructure (`Sipitex.Infrastructure`)

| Paquete | Versión | Notas de licencia |
|---------|---------|-------------------|
| ClosedXML | 0.105.0 | MIT (paquete) |
| DocumentFormat.OpenXml | 3.3.0 | MIT |
| MailKit | 4.14.1 | MIT |
| Microsoft.Extensions.Http | 10.0.9 | Licencia Microsoft .NET |
| Microsoft.AspNetCore.DataProtection.EntityFrameworkCore | 10.0.9 | Licencia Microsoft .NET |
| Microsoft.EntityFrameworkCore | 10.0.9 | Licencia Microsoft .NET |
| Microsoft.EntityFrameworkCore.Relational | 10.0.9 | Licencia Microsoft .NET |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.0 | PostgreSQL License / licencia del paquete Npgsql |
| Microsoft.Extensions.Options.ConfigurationExtensions | 10.0.9 | Licencia Microsoft .NET |
| QRCoder | 1.6.0 | MIT |
| QuestPDF | 2025.7.1 | **Community** (`QuestPDF.Settings.License = LicenseType.Community` en PDF/reportes) |

### Web (`Sipitex.Web`)

| Paquete | Versión | Notas |
|---------|---------|-------|
| Microsoft.AspNetCore.DataProtection.EntityFrameworkCore | 10.0.9 | Microsoft .NET |
| Microsoft.EntityFrameworkCore.Design | 10.0.9 | Microsoft .NET |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 10.0.9 | Microsoft .NET |
| Serilog.AspNetCore | 10.0.0 | Apache-2.0 |
| Serilog.Formatting.Compact | 3.0.0 | Apache-2.0 |
| Serilog.Sinks.Console | 6.1.1 | Apache-2.0 |

### Application

| Paquete | Versión | Notas |
|---------|---------|-------|
| Microsoft.Extensions.Logging.Abstractions | 10.0.0 | Microsoft .NET |
| Microsoft.Extensions.Options | 10.0.0 | Microsoft .NET |

### Tests (`Sipitex.Tests`)

| Paquete | Versión | Notas |
|---------|---------|-------|
| coverlet.collector | 6.0.4 | MIT |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.0 | Microsoft .NET |
| Npgsql / Npgsql.EF Core | 10.0.0 | Npgsql |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.9 | Microsoft .NET |
| Microsoft.NET.Test.Sdk | 17.14.1 | Microsoft |
| Moq | 4.20.72 | BSD-3-Clause |
| Serilog | 4.3.0 | Apache-2.0 |
| xunit / xunit.runner.visualstudio | 2.9.3 / 3.1.4 | Apache-2.0 |

Dependencias **transitivas** vulnerables: ver [`docs/calidad/VULNERABILIDADES.md`](../calidad/VULNERABILIDADES.md) (MimeKit, Cryptography.Pkcs/Xml, etc.).

## 3. Librerías estáticas en `wwwroot/lib` (vendor)

| Biblioteca | Licencia en árbol |
|------------|-------------------|
| Bootstrap | MIT (`wwwroot/lib/bootstrap/LICENSE`) |
| jQuery | MIT (OpenJS) (`wwwroot/lib/jquery/LICENSE.txt`) |
| jQuery Validation | MIT (`wwwroot/lib/jquery-validation/LICENSE.md`) |
| jQuery Validation Unobtrusive | Licencia del archivo en `wwwroot/lib/jquery-validation-unobtrusive/LICENSE.txt` |

## 4. Herramientas de documentación (no runtime)

| Ámbito | Paquete | Licencia package.json |
|--------|---------|------------------------|
| `docs/diagramas` | `@mermaid-js/mermaid-cli` (devDependency) | árbol npm (p. ej. MIT en lock); auditoría **PENDIENTE** |

## 5. Cumplimiento recomendado

1. Definir licencia e IP del producto con el área jurídica del SENA/CMTC.
2. Conservar avisos de terceros al redistribuir.
3. Revisar términos QuestPDF Community frente al uso institucional.
4. Tras actualizar paquetes, regenerar este inventario si cambian licencias.
