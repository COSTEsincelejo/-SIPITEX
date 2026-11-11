# Documentación de calidad — SIPITEX

Índice de plan, informe de pruebas automatizadas, vulnerabilidades NuGet y artefactos medidos el **2026-11-11 (UTC)**.

| Documento | Contenido |
|-----------|-----------|
| [PLAN-PRUEBAS.md](PLAN-PRUEBAS.md) | Alcance, niveles, comandos, criterios; declara lo no ejecutado |
| [INFORME-PRUEBAS.md](INFORME-PRUEBAS.md) | 768 passed / 0 failed; cobertura 87.06 % líneas; tiempos ~19 s / ~17 s |
| [VULNERABILIDADES.md](VULNERABILIDADES.md) | Tabla GHSA MailKit, MimeKit, Cryptography.Pkcs/Xml |
| [artifacts/coverage.cobertura.xml](artifacts/coverage.cobertura.xml) | Coverlet XPlat |
| [artifacts/sipitex-vuln-web.txt](artifacts/sipitex-vuln-web.txt) | Salida `dotnet list … --vulnerable` (Web) |
| [artifacts/sipitex-vuln-infra.txt](artifacts/sipitex-vuln-infra.txt) | Salida `dotnet list … --vulnerable` (Infrastructure) |

Relacionados: [`docs/04-Pruebas.md`](../04-Pruebas.md) (plan cascada), [`docs/operacion/`](../operacion/) (FAQ y parametrización).
