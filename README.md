# SIPITEX — Sistema Integrado de Producción e Inventario Textil

Proyecto .NET 10 con **arquitectura por capas** y desarrollo guiado por **metodología cascada** (CMTC · SENA · ADSO).

## Estructura de la solución

```
Sipitex/
├── docs/                          # Documentación cascada (fases 1–5)
├── src/
│   ├── Sipitex.Domain/            # Entidades, enums (capa de dominio)
│   ├── Sipitex.Application/       # Servicios, DTOs, contratos (lógica de negocio)
│   ├── Sipitex.Infrastructure/    # EF Core, SQLite, repositorios (acceso a datos)
│   └── Sipitex.Web/               # ASP.NET Core MVC (presentación)
├── Dockerfile
├── docker-compose.yml
└── Sipitex.slnx
```

### Dependencias entre capas

```
Web → Application → Domain
Web → Infrastructure → Application → Domain
```

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download) o superior
- Docker (opcional, para RNF07)

## Ejecución local

```powershell
cd src/Sipitex.Web
dotnet run
```

Abrir `https://localhost:5xxx` (el puerto se muestra en consola). La ruta por defecto es **Inventario** (requiere autenticación).

Por defecto usa **SQLite** (`sipitex.db`), creada automáticamente con **migraciones EF Core** y datos de demostración.

### PostgreSQL

También soporta **PostgreSQL 16**. Scripts, dump y guía en [`docs/database/postgres/`](docs/database/postgres/).

```powershell
# App local contra PostgreSQL (requiere BD creada; ver docs/database/postgres/README.md)
cd src/Sipitex.Web
dotnet run --environment PostgreSQL
```

### Usuarios demo

| Correo | Contraseña | Rol |
|--------|------------|-----|
| `admin@sipitex.test` | `Admin123!` | Administrador |
| `instructor@sipitex.test` | `Instructor123!` | Instructor |
| `bodega@sipitex.test` | `Bodega123!` | Bodeguero |

## Docker Compose

```bash
docker compose up --build
```

Abre `http://localhost:8080`. Levanta **PostgreSQL** (`db`) + la app; los datos persisten en el volumen `sipitex-pgdata`.

## Módulos

| Módulo | Ruta | Descripción |
|--------|------|-------------|
| Inventario | `/Inventario` | Materiales, stock, solicitudes de bodega |
| Órdenes | `/Ordenes` | Órdenes de producción y avance |
| MRP | `/Mrp` | BOM y simulación de requerimientos |
| Fichas | `/Fichas` | Registro de producción por ficha |
| Calidad | `/Calidad` | Inspecciones de calidad |
| Estadísticas | `/Estadisticas` | KPIs y gráficos |
| Reportes | `/Reportes` | Exportación PDF / Excel |
| Alertas | `/Alertas` | Preferencias de correo por actor |
| Usuarios | `/Account/Users` | CRUD de usuarios (Administrador) |

## Metodología cascada

Ver carpeta [`docs/`](docs/) para el ciclo completo:

1. **Requisitos** — RF01–RF20, RNF01–RNF08  
2. **Diseño** — Arquitectura por capas, ER, contratos  
3. **Implementación** — Código en `src/`  
4. **Pruebas** — Plan de pruebas funcionales  
5. **Despliegue** — Guía de publicación intranet / Docker  

## Tecnologías

- ASP.NET Core MVC  
- Cookie Authentication + roles  
- Entity Framework Core + SQLite  
- Chart.js (estadísticas)  
- Font Awesome + Inter (UI)  
- Docker Compose  
