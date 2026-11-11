# Diagramas SIPITEX

Imágenes PNG generadas desde Mermaid para el documento IEEE 830.

## Contenido

| Archivo | Descripción |
|---------|-------------|
| `00-arquitectura.png` | Capas de la solución |
| `01-casos-de-uso.png` | Actores y casos de uso |
| `02-clases-dominio.png` | Modelo de clases |
| `03-capas-aplicacion.png` | Flujo MVC → servicios → repositorio |
| `04-secuencia-login.png` | Secuencia de login |
| `05-secuencia-solicitar.png` | Solicitud de material |
| `06-secuencia-aprobar.png` | Aprobación y descuento de stock |
| `07-secuencia-crear-orden.png` | Creación de orden |
| `08-secuencia-reportes.png` | Exportación de reportes |
| `09-secuencia-bom.png` | Alta de ítem BOM |
| `10-entidad-relacion.png` | Diagrama ER |
| `11-gantt.png` | Cronograma / Gantt |
| `12-flujo-solicitud.png` | Diagrama de flujo |
| `13-distribucion.png` | Diagrama de distribución |

## Regenerar

```bash
npm install
./node_modules/.bin/mmdc -i src/NOMBRE.mmd -o NOMBRE.png -b white -s 2 -w 1400
```
