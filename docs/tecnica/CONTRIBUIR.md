# Contribuir

**Fuente:** `README.md` (secciones Correr pruebas / Contribución) y convención de ramas Cloud Agent del repo.

## Ramas

- Prefijo usado en el repositorio: `cursor/<descripcion-kebab>`.
- Abrir PR hacia `main` (Render `autoDeploy: true` en `branch: main` según `render.yaml`).
- No pegar secretos en git (`.env.example` solo plantilla).

## Antes del PR

1. Correr pruebas:

```bash
dotnet test Sipitex.slnx
```

2. Si cambió el modelo EF Core:

```bash
dotnet ef migrations add NombreCambio --project src/Sipitex.Infrastructure --startup-project src/Sipitex.Web
dotnet ef migrations has-pending-model-changes --project src/Sipitex.Infrastructure --startup-project src/Sipitex.Web
```

El CI ejecuta `has-pending-model-changes` y **falla** si el modelo no coincide con las migraciones (README).

## Arranque local (resumen)

- .NET SDK 10 + PostgreSQL 16.
- `cd src/Sipitex.Web && dotnet run`, o `docker compose up --build` → `http://localhost:8080`.

## Documentación

- Entrega operativa: `docs/HANDOVER.md`, `docs/DEPLOY.md`, `docs/BACKUP.md`.
- Datos factuales del repo: `docs/entrega/EXTRACCION-DATOS-REPO.md`.
- Esta carpeta técnica: `docs/tecnica/README.md`.
