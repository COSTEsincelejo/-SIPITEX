# Documentación de entrega — SIPITEX v1.0

Índice de `docs/entrega/`. Versión **v1.0** (etiqueta documental; no hay git tags).  
Referencia `main`: `52e4170` (#162), 2026-11-11.

> **No modificar** `EXTRACCION-DATOS-REPO.md` salvo nueva extracción factual del repo.

---

## Contenido

| # | Documento | Propósito |
|---|-----------|-----------|
| 0 | [EXTRACCION-DATOS-REPO.md](EXTRACCION-DATOS-REPO.md) | Fuente factual (RF/RNF, roles, controllers, paquetes, deuda). **Solo lectura** para el resto. |
| 1 | [CIERRE-PROYECTO.md](CIERRE-PROYECTO.md) | Acta de cierre documental v1.0, métricas y pendientes operativos |
| 2 | [RESUMEN-EJECUTIVO.md](RESUMEN-EJECUTIVO.md) | Resumen de una página para dirección / ADSO |
| 3 | [ALCANCE-FINAL.md](ALCANCE-FINAL.md) | Incluye / excluye + deuda HANDOVER |
| 4 | [CHANGELOG.md](CHANGELOG.md) | Hitos y PRs reales hasta v1.0 |
| 5 | [LECCIONES-APRENDIDAS.md](LECCIONES-APRENDIDAS.md) | **BORRADOR**: SQLite→PG, Bodega→Planta, Resend→Brevo, stock dual, PRs reemplazados |
| 6 | [MATRIZ-ENTREGABLES-REQUISITOS.md](MATRIZ-ENTREGABLES-REQUISITOS.md) | IEEE 830 como numeración principal + notas vs `01-Requisitos` |
| 7 | [MATRIZ-TRAZABILIDAD.md](MATRIZ-TRAZABILIDAD.md) | RF/RNF → módulo → clase/página → tests (desde extracción) |
| 8 | [CASOS-DE-USO.md](CASOS-DE-USO.md) | Casos por rol Admin / Instructor / Encargado |
| 9 | [GLOSARIO.md](GLOSARIO.md) | AyudaCatalogo + dominio |
| 10 | [PLAN-TRANSICION.md](PLAN-TRANSICION.md) | Pasos ordenados; responsables PENDIENTE donde aplique |

---

## Lectura recomendada fuera de esta carpeta

| Documento | Uso |
|-----------|-----|
| [../HANDOVER.md](../HANDOVER.md) | Traspaso operativo, variables, deuda |
| [../../README.md](../../README.md) | Visión técnica y módulos |
| [../01-Requisitos.md](../01-Requisitos.md) | RF/RNF numeración fase 1 / seed |
| [../08-IEEE830-Especificacion.md](../08-IEEE830-Especificacion.md) | SRS alineada al código |
| [../MANUAL-ADMINISTRADOR.md](../MANUAL-ADMINISTRADOR.md) | Operación Admin |
| [../MANUAL-INSTRUCTOR.md](../MANUAL-INSTRUCTOR.md) | Operación Instructor |
| [../MANUAL-BODEGUERO.md](../MANUAL-BODEGUERO.md) | Operación Encargado de bodega |
| [../GUIA-INVENTARIO-INICIAL.md](../GUIA-INVENTARIO-INICIAL.md) | Carga inicial Excel |
| [../DEPLOY.md](../DEPLOY.md) / [../05-Despliegue.md](../05-Despliegue.md) | Despliegue |
| [../BACKUP.md](../BACKUP.md) | Respaldos |
| [../calidad/](../calidad/) | Plan/informe de pruebas, cobertura, GHSA |
| [../operacion/](../operacion/) | FAQ, parametrización, mantenimiento, datos personales |
| [../tecnica/](../tecnica/) | Arquitectura y ADRs |

---

## Métricas de referencia (2026-11-11)

- Tests: **768 passed**, 0 failed, ~19 s  
- Cobertura coverlet: line-rate **87,06 %**, branch **48,05 %**  
- Detalle: [`../calidad/INFORME-PRUEBAS.md`](../calidad/INFORME-PRUEBAS.md)
