# Plan de pruebas — SIPITEX

**Alcance:** solución .NET 10 (`Sipitex.slnx`), capas Domain / Application / Infrastructure / Web.  
**Suite automatizada:** proyecto `tests/Sipitex.Tests` (xUnit + Coverlet XPlat).  
**Fecha de referencia del plan:** 2026-11-11 (UTC).

## 1. Objetivos

1. Verificar regresión funcional de módulos operativos (inventario, solicitudes, órdenes/MES, MRP, calidad, actas, usuarios, privacidad, correo).
2. Medir cobertura de líneas/ramas con Coverlet y conservar el artefacto Cobertura.
3. Registrar vulnerabilidades NuGet conocidas (`dotnet list package --vulnerable --include-transitive`).
4. Dejar explícito qué **no** se ejecutó en esta entrega (rendimiento, pentest, UAT).

## 2. Niveles de prueba

| Nivel | Qué cubre | Dónde | Estado en entrega |
|-------|-----------|-------|-------------------|
| Unitarias / servicios | Dominio, Application, Infrastructure (mocks/SQLite/PG según fixture) | `tests/Sipitex.Tests/*.cs` | Ejecutado |
| Integración / flujo | Flujos multi-paso con host web o BD | `tests/Sipitex.Tests/Integration/` | Ejecutado |
| Funcionales manuales (cascada) | Casos T01–T14 de `docs/04-Pruebas.md` | Manual | No re-ejecutados como UAT formal en esta fecha |
| Rendimiento / carga | RNF01 y perfiles de estrés | — | **No se hicieron** |
| Seguridad (pentest) | Ataques externos, OWASP profundo | — | **No se hicieron** (sí hay escaneo NuGet vulnerable) |
| UAT institucional | Aceptación con usuarios del CMTC | — | **No se hicieron** |

## 3. Inventario de la suite automatizada

- **~118 archivos** `*Tests*.cs` bajo `tests/Sipitex.Tests` (conteo 2026-11-11).
- Integración destacada:
  - `Integration/FlujoCompletoCmtcE2ETests.cs`
  - `Integration/SolicitudMaterialFlowTests.cs` (+ `SolicitudMaterialFlowFixture.cs`)
- Otros flujos relevantes: `EmailConfirmationFlowTests`, `ProductionFlowServiceTests`, `AntiforgeryHttpTests`, `ModuleAccessTests`, `PostgresConnectionStringTests`, actas/PDF, importación de inventario, privacidad, buscador, MES.

## 4. Estrategia de ejecución

### 4.1 Regresión completa

```bash
dotnet test Sipitex.slnx
```

Criterio de paso: **0 fallos**, **0 skipped** (salvo que el entorno declare skips explícitos; en la corrida de referencia no hubo skipped).

### 4.2 Cobertura (Coverlet XPlat)

```bash
dotnet test Sipitex.slnx \
  --collect:"XPlat Code Coverage" \
  --results-directory /tmp/sipitex-coverage
```

Artefacto a conservar: `coverage.cobertura.xml` → `docs/calidad/artifacts/coverage.cobertura.xml`.

### 4.3 Vulnerabilidades NuGet

```bash
dotnet list src/Sipitex.Web/Sipitex.Web.csproj package --vulnerable --include-transitive
dotnet list src/Sipitex.Infrastructure/Sipitex.Infrastructure.csproj package --vulnerable --include-transitive
```

Resultados y GHSA: [`VULNERABILIDADES.md`](VULNERABILIDADES.md). Salidas crudas: `artifacts/sipitex-vuln-web.txt`, `artifacts/sipitex-vuln-infra.txt`.

### 4.4 npm audit

- La aplicación web **no** tiene `package.json` en la raíz ni en `src/Sipitex.Web` (solo vendor estático en `wwwroot/lib`).
- **`npm audit` de la app web: N/A.**
- Existe `docs/diagramas/package.json` (mermaid-cli). Auditar ese árbol queda **PENDIENTE** (no bloquea la app en runtime).

## 5. Ambientes

| Ambiente | Uso en pruebas |
|----------|----------------|
| Local / CI | `dotnet test`; PostgreSQL accesible o fixtures del host de pruebas |
| Docker Compose Production + HTTP | Validar cookies antiforgery (`CookieSecurePolicy.Always` en Production) — ver FAQ operación |
| Render / hosting | Smoke de arranque con `DATABASE_URL` (no loopback) |

## 6. Criterios de aceptación (calidad de entrega)

| Criterio | Umbral / evidencia |
|----------|-------------------|
| Suite automatizada | Failed = 0 |
| Cobertura de líneas (Coverlet) | ≥ 85 % global (referencia medida: **87.06 %**) |
| Artefacto Cobertura | Presente en `docs/calidad/artifacts/` |
| Vulnerabilidades | Documentadas; remediación recomendada (actualizar paquetes) |
| Rendimiento / pentest / UAT | Declarados como no ejecutados |

## 7. Entregables de esta carpeta

| Documento | Contenido |
|-----------|-----------|
| [`INFORME-PRUEBAS.md`](INFORME-PRUEBAS.md) | Resultados numéricos 2026-11-11 |
| [`VULNERABILIDADES.md`](VULNERABILIDADES.md) | Tabla GHSA NuGet |
| [`artifacts/coverage.cobertura.xml`](artifacts/coverage.cobertura.xml) | Cobertura Coverlet |
| [`README.md`](README.md) | Índice |

## 8. Fuera de alcance (explícito)

- Pruebas de rendimiento / carga.
- Pruebas de seguridad ofensivas (pentest).
- UAT con firmas institucionales.
- `npm audit` de `docs/diagramas` (PENDIENTE).
