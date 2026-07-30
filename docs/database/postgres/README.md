# Base de datos SIPITEX en PostgreSQL

## Credenciales por defecto (desarrollo / Docker)

| Parámetro | Valor |
|-----------|--------|
| Host | `localhost` (o `db` en Docker Compose) |
| Puerto | `5432` |
| Base de datos | `sipitex` |
| Usuario | `sipitex` |
| Contraseña | `sipitex` |

Cadena de conexión:

```
Host=localhost;Port=5432;Database=sipitex;Username=sipitex;Password=sipitex
```

## Archivos de este directorio

| Archivo | Uso |
|---------|-----|
| `00_create_database.sql` | Crea rol y BD (ejecutar como `postgres`) |
| `01_init_roles.sql` | Extensiones al crear el contenedor Docker |
| `02_schema_ef.sql` | Esquema idempotente generado por EF Core |
| `sipitex_postgres_full.sql` | **Dump completo** (esquema + datos demo) — recomendado |
| `sipitex_postgres_schema.sql` | Solo esquema |
| `sipitex_postgres_data.sql` | Solo datos |
| `sipitex_postgres.dump` | Dump binario (`pg_restore`) |

## Restaurar dump completo (recomendado)

```bash
# 1) Crear BD (una vez)
sudo -u postgres psql -f docs/database/postgres/00_create_database.sql

# 2) Restaurar esquema + datos demo
psql -h localhost -U sipitex -d sipitex -f docs/database/postgres/sipitex_postgres_full.sql
```

O con dump custom:

```bash
pg_restore -h localhost -U sipitex -d sipitex --clean --if-exists docs/database/postgres/sipitex_postgres.dump
```

## Docker Compose

```bash
docker compose up --build
```

Levanta PostgreSQL (`db`) + la app (`sipitex` en http://localhost:8080).
EF Core aplica migraciones y el seed de demo al arrancar.

## Ejecutar la app local contra PostgreSQL

```bash
cd src/Sipitex.Web
dotnet run --environment PostgreSQL
```

Usa `appsettings.PostgreSQL.json`.

## Usuarios demo (seed)

| Correo | Contraseña | Rol |
|--------|------------|-----|
| `admin@sipitex.test` | `Admin123!` | Administrador |
| `instructor@sipitex.test` | `Instructor123!` | Instructor |
| `bodega@sipitex.test` | `Bodega123!` | Bodeguero |

## Notas

- SQLite sigue disponible por defecto en desarrollo (`Database:Provider=Sqlite`).
- Las migraciones PostgreSQL viven en `src/Sipitex.Infrastructure.Migrations.PostgreSQL`.
- Las migraciones SQLite siguen en `src/Sipitex.Infrastructure/Migrations`.
