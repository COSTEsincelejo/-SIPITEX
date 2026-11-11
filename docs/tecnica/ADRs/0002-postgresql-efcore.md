# ADR 0002 — PostgreSQL 16 + Entity Framework Core

- **Estado:** aceptada
- **Fecha:** 2026-09-16 (`9a60663` — Migrar SIPITEX de SQLite a PostgreSQL 16; consolidación migraciones `57a2512`)

## Contexto

La app necesitaba persistencia durable en despliegue (Render / Docker). SQLite en disco efímero de Render se pierde en cada deploy (`docs/DEPLOY.md`).

## Decisión

- Motor: **PostgreSQL 16** (Npgsql EF Core `10.0.0`, EF Core `10.0.9`).
- Contexto: `SipitexDbContext` en Infrastructure; migraciones en `src/Sipitex.Infrastructure/Migrations`.
- Arranque: `MigrateAsync` vía `DbInitializer.InitializeAsync`.
- Blueprint: `render.yaml` DB `sipitex-db`; Compose: imagen `postgres:16`.

## Consecuencias

- Respaldo con `pg_dump` / restauración documentados en `docs/BACKUP.md`.
- Data Protection, fotos y outbox de correo viven en la misma base.
- Tests usan también Sqlite en proyecto de tests; producción no.
