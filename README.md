# SIPITEX — Sistema Integrado de Aprendizaje Producción e Inventario Textil

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

Motor de datos: **SQLite** (`sipitex.db`), creado al iniciar con migraciones EF Core (`MigrateAsync`).

### Usuarios demo (solo Development)

Los tres usuarios de demostración se siembran **únicamente** cuando `Seed:DemoUsers` es `true` (`appsettings.Development.json`). En producción ese flag está en `false` y las credenciales **no** se muestran en la pantalla de login.

| Correo | Contraseña | Rol |
|--------|------------|-----|
| `admin@sipitex.test` | `Admin123!` | Administrador |
| `instructor@sipitex.test` | `Instructor123!` | Instructor |
| `bodega@sipitex.test` | `Bodega123!` | Encargado de bodega |

Si un entorno de producción arranca **sin ningún Administrador**, se crea `admin@sipitex.local` con la clave de `ADMIN_SEED_PASSWORD` (o una aleatoria, impresa una sola vez en el log de arranque). Cámbiela en el perfil tras el primer ingreso.

Para el demo público (p. ej. Render) puede reactivarse el seed con `Seed__DemoUsers=true`.

## Docker Compose

```bash
docker compose up --build
```

Abrir `http://localhost:8080`. La base de datos persiste en el volumen `sipitex-data`.

## Módulos

| Módulo | Ruta | Descripción |
|--------|------|-------------|
| Inventario | `/Inventario` | Materiales, stock, movimientos |
| Órdenes | `/Ordenes` | Órdenes de producción, MES y avance |
| MRP | `/Mrp` | BOM y simulación de requerimientos |
| Fichas | `/Fichas` | Registro de producción por grupo SENA |
| Plantas de inventario | `/PlantasInventario` | Catálogo de plantas |
| Solicitudes de planta | `/PlantasInventarioSolicitudes` | Cola de solicitudes de material |
| Órdenes de planta | `/PlantasInventarioOrdenes` | Entrega / reingreso de materiales de orden |
| Grupos de confección | `/GruposConfeccion` | Horas y prendas por grupo |
| Consumos | `/Consumos` | Consumo real de materiales por orden |
| Costeo | `/Costos` | Costo estimado de prenda (materiales + mano de obra) |
| Actas | `/Actas` | Actas de ingreso/egreso con conformidad simple |
| Calidad | `/Calidad` | Inspecciones de calidad |
| Auditoría | `/Auditoria` | Activity log (Administrador) |
| Estadísticas | `/Estadisticas` | KPIs y gráficos |
| Reportes | `/Reportes` | Exportación PDF / Excel |
| Alertas | `/Alertas` | Preferencias de correo por actor |
| Usuarios | `/Account/Users` | CRUD de usuarios (Administrador) |

## Políticas de negocio documentadas

- **Actas:** la validez formal es **conformidad simple** (nombre, cargo y timestamp UTC de quien entrega y quien recibe). No se exige firma manuscrita ni gráfica.
- **Costeo:** `Costing:LaborHourRate` permanece en `0` hasta que CMTC confirme la tarifa real. `/Costos` muestra un aviso y no presenta la mano de obra en $0 como costo válido.
- **Login:** 5 intentos fallidos (correo + IP) bloquean 15 minutos. El mensaje no indica si el correo existe.

## Metodología cascada

Ver carpeta [`docs/`](docs/) para el ciclo completo:

1. **Requisitos** — RF01–RF20, RNF01–RNF08  
2. **Diseño** — Arquitectura por capas, ER, contratos  
3. **Implementación** — Código en `src/`  
4. **Pruebas** — Plan de pruebas funcionales  
5. **Despliegue** — Guía de publicación intranet / Docker  

## Contribución

Antes de abrir un PR:

1. `dotnet test Sipitex.slnx`
2. Si cambió el modelo EF (entidades, `DbContext`), genere la migración:

```bash
dotnet ef migrations add NombreCambio --project src/Sipitex.Infrastructure --startup-project src/Sipitex.Web
```

El CI ejecuta `dotnet ef migrations has-pending-model-changes` y **falla** si el modelo no coincide con las migraciones.

## Tecnologías

- ASP.NET Core MVC  
- Cookie Authentication + roles  
- Entity Framework Core + SQLite  
- Chart.js (estadísticas)  
- Font Awesome + Inter (UI)  
- Docker Compose  
