# Informe de pruebas — SIPITEX

**Fecha de ejecución:** 2026-11-11 (UTC)  
**Comando principal:** `dotnet test Sipitex.slnx`  
**Cobertura:** Coverlet collector XPlat (`--collect:"XPlat Code Coverage"`)  
**Artefacto:** [`artifacts/coverage.cobertura.xml`](artifacts/coverage.cobertura.xml)

> Cifras de este informe: **medidas en la fecha indicada**. No se inventan totales ni tasas distintas.

## 1. Resumen ejecutivo

| Ítem | Resultado |
|------|-----------|
| Pruebas automatizadas | **Passed 768 / Failed 0 / Skipped 0 / Total 768** |
| Duración (1.ª corrida sin énfasis en cobertura) | **~19 s** |
| Duración (2.ª corrida con recolección de cobertura) | **~17 s** |
| Cobertura líneas (global) | **87.06 %** (57 202 / 65 702) |
| Cobertura ramas (global) | **48.05 %** |
| Rendimiento / pentest / UAT | **No se hicieron** |

**Veredicto:** la suite automatizada pasa completa. Quedan pendientes pruebas no funcionales de carga, pentest y aceptación de usuario institucional.

## 2. Resultados `dotnet test`

| Métrica | Valor |
|---------|-------|
| Failed | **0** |
| Passed | **768** |
| Skipped | **0** |
| Total | **768** |
| Duration (corrida de regresión) | **~19 s** |
| Duration (corrida con cobertura) | **~17 s** |

Proyecto de pruebas: `tests/Sipitex.Tests` (~118 archivos `*Tests*.cs`).

### Integración destacada (existen en el repo)

- `tests/Sipitex.Tests/Integration/FlujoCompletoCmtcE2ETests.cs`
- `tests/Sipitex.Tests/Integration/SolicitudMaterialFlowTests.cs`

## 3. Cobertura Coverlet XPlat

Fuente del XML: atributo raíz  
`line-rate="0.8706" branch-rate="0.4805" lines-covered="57202" lines-valid="65702"`.

### 3.1 Global

| Indicador | Valor |
|-----------|-------|
| Line-rate | **87.06 %** |
| Branch-rate | **48.05 %** |
| Lines covered / valid | **57 202 / 65 702** |

### 3.2 Por paquete (line-rate del XML)

| Paquete | Line-rate | Nota |
|---------|-----------|------|
| Sipitex.Application | **81.3 %** (0.8134) | |
| Sipitex.Domain | **91.3 %** (0.9135) | |
| Sipitex.Infrastructure | **97.2 %** (0.9718) | |
| Sipitex.Web.Views | **20.9 %** (0.2094) | Razor; baja cobertura esperable |
| Sipitex.Web | **46.1 %** (0.4609) | Controllers / Program / UI host |

## 4. npm audit

| Ámbito | Resultado |
|--------|-----------|
| App web (`src/Sipitex.Web`) | **N/A** — no hay `package.json` de la aplicación (solo librerías vendor en `wwwroot/lib`) |
| `docs/diagramas` (mermaid-cli) | **PENDIENTE** si se decide auditar el árbol de generación de diagramas |

## 5. Vulnerabilidades NuGet

Ejecutado: `dotnet list package --vulnerable --include-transitive` sobre Web e Infrastructure.  
Detalle GHSA: [`VULNERABILIDADES.md`](VULNERABILIDADES.md).

Hallazgos (severidades NuGet, sin CVSS inventado):

- MailKit 4.14.1 — Moderate — GHSA-9j88-vvj5-vhgr  
- MimeKit 4.14.0 — Moderate — GHSA-g7hc-96xr-gvvx  
- System.Security.Cryptography.Pkcs 8.0.1 / 10.0.9 — High + Moderate  
- System.Security.Cryptography.Xml 10.0.9 — múltiples High  

## 6. Pruebas no ejecutadas (explícito)

| Tipo | Estado |
|------|--------|
| Pruebas de rendimiento / carga | **NO se hicieron** |
| Pruebas de seguridad (pentest) | **NO se hicieron** |
| UAT (aceptación con el centro) | **NO se hicieron** |

El plan funcional manual de cascada permanece en [`docs/04-Pruebas.md`](../04-Pruebas.md); este informe no lo sustituye como acta UAT firmada.

## 7. Evidencias

| Archivo | Descripción |
|---------|-------------|
| `artifacts/coverage.cobertura.xml` | Cobertura Coverlet (misma corrida documentada) |
| `artifacts/sipitex-vuln-web.txt` | Salida vulnerable — proyecto Web |
| `artifacts/sipitex-vuln-infra.txt` | Salida vulnerable — proyecto Infrastructure |
