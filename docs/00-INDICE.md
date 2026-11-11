# Índice de documentación — SIPITEX

| Documento | Contenido |
|-----------|-----------|
| [01-Requisitos.md](01-Requisitos.md) | Fase 1 — Análisis (cascada) |
| [02-Diseno.md](02-Diseno.md) | Fase 2 — Diseño y arquitectura |
| [03-Implementacion.md](03-Implementacion.md) | Fase 3 — Implementación |
| [04-Pruebas.md](04-Pruebas.md) | Fase 4 — Pruebas |
| [05-Despliegue.md](05-Despliegue.md) | Fase 5 — Despliegue |
| [BACKUP.md](BACKUP.md) | Respaldo, restauración y copia antes de importar inventario |
| [HANDOVER.md](HANDOVER.md) | Entrega operativa, credenciales a rotar y deuda conocida |
| [entrega/EXTRACCION-DATOS-REPO.md](entrega/EXTRACCION-DATOS-REPO.md) | Extracción factual: RF/RNF, mapeo código, roles, controllers, NuGet/npm |
| [PRIVACIDAD-DATOS-INSTITUCION.md](PRIVACIDAD-DATOS-INSTITUCION.md) | Tabla de datos institucionales y variables `Privacidad__*` |
| [MANUAL-ADMINISTRADOR.md](MANUAL-ADMINISTRADOR.md) | Manual del administrador |
| [MANUAL-BODEGUERO.md](MANUAL-BODEGUERO.md) | Manual del encargado de bodega |
| [MANUAL-INSTRUCTOR.md](MANUAL-INSTRUCTOR.md) | Manual del instructor |
| **[08-IEEE830-Especificacion.md](08-IEEE830-Especificacion.md)** | **SRS IEEE 830** (texto claro + diagramas en imagen) |

## Diagramas (imágenes)

Carpeta: [`diagramas/`](diagramas/)

| Imagen | Diagrama |
|--------|----------|
| `00-arquitectura.png` | Arquitectura por capas |
| `01-casos-de-uso.png` | Casos de uso |
| `02-clases-dominio.png` | Clases del dominio |
| `03-capas-aplicacion.png` | Controllers → servicios → BD |
| `04` … `09-secuencia-*.png` | Secuencias (login, solicitudes, órdenes, reportes, BOM) |
| `10-entidad-relacion.png` | Modelo entidad-relación |

Los diagramas se ven directamente en GitHub, Codespaces, VS Code y al exportar a PDF/Word.  
Fuentes Mermaid editables: [`diagramas/src/`](diagramas/src/).

El flujo MES (etapas de orden) es aditivo al alta de órdenes documentada en `07-secuencia-crear-orden.png`. No se regeneraron PNG en el cierre de módulos de planta, costeo y actas: la semántica de estados de orden (Pendiente / En proceso / Finalizada / Cancelada) no cambió.
