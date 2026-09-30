# Despliegue en Render

La app se construye con el `Dockerfile` de la raíz (runtime Docker, no .NET nativo). La etapa de build usa el SDK 10.0.401, publica en Release y la imagen final es `aspnet:10.0.9`. Al arrancar, `dotnet Sipitex.Web.dll` lee `PORT` y escucha en `http://0.0.0.0:${PORT}`. Render inyecta `PORT` (por defecto 10000). En local el valor es 8080.

`render.yaml` fija `branch: main`, `autoDeploy: true` y `healthCheckPath: /healthz`. `/healthz` responde `200` y `ok` sin autenticación y sin consultar la base. `/health` sigue existiendo y sí revisa PostgreSQL.

## Variables de entorno (solo nombres)

- `PORT` — la inyecta Render. No la fije en 8080 en el panel.
- `ASPNETCORE_ENVIRONMENT` — `Production`.
- `DATABASE_URL` — cadena de PostgreSQL en producción. Acepta `postgresql://usuario:clave@host/db?sslmode=require` (se convierte a Npgsql con SSL Mode=Require) o `Host=...;Database=...;Username=...;Password=...`. Si falta, el proceso termina y no usa 127.0.0.1. `ConnectionStrings__DefaultConnection` sigue valiendo cuando no apunta a localhost.
- `RENDER_GIT_COMMIT` — la inyecta Render en build y en runtime. El footer muestra los primeros 7 caracteres. Sin valor, el texto es `local`.
- `SEED_DEMO_DATA` — `true` solo en una demo. Si no está, manda `Seed:DemoUsers` (apagado en producción).
- `ADMIN_SEED_PASSWORD` — admin inicial si la base no tiene administradores. No se escribe en el log.
- `Email__Enabled`, `Email__User`, `Email__Password` — SMTP. Sin usuario, el correo queda en `EmailOutboxMessages`.
- `Costing__LaborHourRate` — tarifa de referencia.

No guarde valores en git. El workflow de CI llama al Deploy Hook con el secret `RENDER_DEPLOY_HOOK_URL` en cada push a `main` que pase `build-and-test`. Si el secret no existe, el job se omite y el pipeline sigue en verde.

## Comprobar la versión

- Footer (login y app): `SIPITEX v1.0 · build {hash}`.
- `GET /version` (sin login) devuelve JSON: `commit`, `build` (hash corto) y `builtAt` (fecha UTC del publish).

Si el hash no es el de `main`, producción no está corriendo ese commit.

## Si un deploy falla

1. En el panel de Render, Events del servicio: el build o el health check quedaron en rojo y el tráfico sigue en la instancia anterior.
2. Logs de arranque: un `LogCritical` y la línea `SIPITEX: arranque abortado (exit 1)` indican cadena de conexión o Postgres inalcanzable. Si la base aún no acepta conexiones, hay hasta 5 intentos con 2 s de espera. Un esquema a medias detiene el proceso con código 1 (no 139).
3. Health check path: `/healthz`.
4. Clear build cache & deploy, luego `GET /version`.

**SQLite sobre el disco efímero de Render se borra en cada deploy.** El proveedor de esta app es PostgreSQL administrado. No despliegue durante una demo si alguien está usando una base local o un disco que no persiste.
