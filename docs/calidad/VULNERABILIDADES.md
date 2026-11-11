# Vulnerabilidades conocidas (NuGet) — SIPITEX

**Fecha del escaneo:** 2026-11-11 (UTC)  
**Comandos:**

```bash
dotnet list src/Sipitex.Web/Sipitex.Web.csproj package --vulnerable --include-transitive
dotnet list src/Sipitex.Infrastructure/Sipitex.Infrastructure.csproj package --vulnerable --include-transitive
```

**Salidas crudas:** [`artifacts/sipitex-vuln-web.txt`](artifacts/sipitex-vuln-web.txt), [`artifacts/sipitex-vuln-infra.txt`](artifacts/sipitex-vuln-infra.txt).

> Severidades según el reporte de NuGet (`Moderate` / `High`). **No se inventan puntuaciones CVSS** en este documento; consulte cada advisory GHSA.

## 1. Tabla de hallazgos

| Paquete | Versión(es) resuelta(s) | Severidad (NuGet) | Advisory | Origen observado |
|---------|-------------------------|-------------------|----------|------------------|
| MailKit | 4.14.1 | Moderate | [GHSA-9j88-vvj5-vhgr](https://github.com/advisories/GHSA-9j88-vvj5-vhgr) | Top-level en Infrastructure; transitivo vía Web |
| MimeKit | 4.14.0 | Moderate | [GHSA-g7hc-96xr-gvvx](https://github.com/advisories/GHSA-g7hc-96xr-gvvx) | Transitivo (MailKit) en Web e Infrastructure |
| System.Security.Cryptography.Pkcs | 8.0.1 | High | [GHSA-846f-f764-76mx](https://github.com/advisories/GHSA-846f-f764-76mx) | Transitivo en Web |
| System.Security.Cryptography.Pkcs | 8.0.1 | Moderate | [GHSA-2fmv-rv2c-59x7](https://github.com/advisories/GHSA-2fmv-rv2c-59x7) | Transitivo en Web |
| System.Security.Cryptography.Pkcs | 10.0.9 | High | [GHSA-846f-f764-76mx](https://github.com/advisories/GHSA-846f-f764-76mx) | Transitivo en Infrastructure (y warning en restore Web) |
| System.Security.Cryptography.Pkcs | 10.0.9 | Moderate | [GHSA-2fmv-rv2c-59x7](https://github.com/advisories/GHSA-2fmv-rv2c-59x7) | Transitivo en Infrastructure |
| System.Security.Cryptography.Xml | 10.0.9 | High | [GHSA-cvvh-rhrc-wg4q](https://github.com/advisories/GHSA-cvvh-rhrc-wg4q) | Transitivo en Infrastructure |
| System.Security.Cryptography.Xml | 10.0.9 | High | [GHSA-g8r8-53c2-pm3f](https://github.com/advisories/GHSA-g8r8-53c2-pm3f) | Transitivo en Infrastructure |
| System.Security.Cryptography.Xml | 10.0.9 | High | [GHSA-23rf-6693-g89p](https://github.com/advisories/GHSA-23rf-6693-g89p) | Transitivo en Infrastructure |
| System.Security.Cryptography.Xml | 10.0.9 | High | [GHSA-8q5v-6pqq-x66h](https://github.com/advisories/GHSA-8q5v-6pqq-x66h) | Transitivo en Infrastructure |
| System.Security.Cryptography.Xml | 10.0.9 | High | [GHSA-mmjf-rqrv-855v](https://github.com/advisories/GHSA-mmjf-rqrv-855v) | Transitivo en Infrastructure |
| System.Security.Cryptography.Xml | 10.0.9 | High | [GHSA-r955-8q5j-3h83](https://github.com/advisories/GHSA-r955-8q5j-3h83) | Transitivo en Infrastructure |
| System.Security.Cryptography.Xml | 10.0.9 | High | [GHSA-7hqp-7c5m-4vq6](https://github.com/advisories/GHSA-7hqp-7c5m-4vq6) | Transitivo en Infrastructure |
| System.Security.Cryptography.Xml | 10.0.9 | High | [GHSA-c399-9853-6jmj](https://github.com/advisories/GHSA-c399-9853-6jmj) | Transitivo en Infrastructure |

## 2. Recomendación de remediación

1. **MailKit / MimeKit:** actualizar MailKit (y la MimeKit que arrastre) a la versión mínima que cierre GHSA-9j88-vvj5-vhgr y GHSA-g7hc-96xr-gvvx; re-ejecutar `dotnet list package --vulnerable --include-transitive`.
2. **System.Security.Cryptography.Pkcs:** subir a un parche que cierre GHSA-846f-f764-76mx y GHSA-2fmv-rv2c-59x7 (hoy aparecen **8.0.1** y **10.0.9** según grafo Web/Infrastructure). Preferir alinear con el runtime .NET 10 del proyecto.
3. **System.Security.Cryptography.Xml:** actualizar **10.0.9** a la versión parcheada que cierre el conjunto de GHSA High listados arriba (suele venir como dependencia transitiva de firma/XML o del stack de correo/documentos).
4. Tras actualizar: `dotnet restore`, `dotnet test Sipitex.slnx`, repetir el listado `--vulnerable` y archivar la nueva salida en `artifacts/`.

## 3. npm / frontend

- **App web:** no aplica `npm audit` (sin `package.json` de aplicación).
- **`docs/diagramas`:** auditoría npm **PENDIENTE** (solo herramienta de generación Mermaid; no forma parte del runtime desplegado).

## 4. Alcance de seguridad fuera de este escaneo

No se realizó **pentest** ni batería ofensiva OWASP en la fecha del informe. Este documento solo refleja el inventario NuGet vulnerable.
