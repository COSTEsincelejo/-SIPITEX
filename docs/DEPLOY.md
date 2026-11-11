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
- `Email__Enabled` — `true` para enviar. En `false` el envío falla con un error visible; no se simula una entrega.
- `Email__Provider` — en producción `Brevo` (HTTPS, puerto 443). También se aceptan `Resend`, `Smtp` y `Outbox`.
- `Email__ApiKey` — clave del proveedor API. No va en git.
- `Email__FromAddress` — remitente verificado en Resend o Brevo.
- `Email__FromName` — nombre que ve el destinatario (`SIPITEX`).
- `Email__Host`, `Email__User`, `Email__Password` — solo si `Email__Provider=Smtp`. El plan gratis de Render bloquea los puertos 25, 465 y 587.
- `Email__Provider=Outbox` guarda el mensaje en `EmailOutboxMessages` y no lo entrega.
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

## Rollback

Procedimiento operativo a partir del comportamiento documentado de Render + migraciones EF:

1. **Redeploy a un commit anterior** en el servicio web de Render (panel: Events / Manual Deploy / seleccionar commit, o redeploy del deploy verde previo). `autoDeploy` sigue ligado a `main` en `render.yaml`; un rollback de código no cambia la base por sí solo.
2. **Comprobar versión** con `GET /version` (hash del commit) y el footer `build {hash}`.
3. **Migraciones EF:** al arrancar la app aplica `MigrateAsync` hacia el esquema del binario desplegado. Si el commit anterior tiene **menos** migraciones que las ya aplicadas en PostgreSQL, un simple redeploy **no** ejecuta `Down` automáticamente.
   - Opción A (preferida si el esquema nuevo es compatible hacia atrás): redeploy del commit anterior sin tocar la BD.
   - Opción B (esquema incompatible): restaurar un volcado anterior de PostgreSQL ([BACKUP.md](BACKUP.md)) **o** aplicar `dotnet ef migrations` `Down`/script SQL con cuidado en una ventana de mantenimiento, luego redeploy. Un esquema a medias aborta el arranque (exit 1).
4. Tras rollback, validar `/healthz` y `/health`, login (cookies / Data Protection keys en PG) y un flujo corto (p. ej. consulta de planta).

No hay botón de “rollback de migraciones” en el código de arranque.

## Recuperación ante desastres

- **Datos:** todo lo persistente vive en PostgreSQL (usuarios, inventario, órdenes, fotos, outbox de correo, **claves Data Protection**). El disco del Web Service en Render **no** conserva datos entre deploys.
- **Respaldo / restore:** procedimiento `pg_dump` / `pg_restore` y ramas Neon en [BACKUP.md](BACKUP.md). Tras restaurar un backup antiguo, validar sesiones (HANDOVER: cookies / Data Protection en PG).
- **Claves de cookie:** `PersistKeysToDbContext<SipitexDbContext>` (`Program.cs`). Si se restaura una BD sin las keys actuales, las cookies firmadas con keys nuevas/antiguas pueden invalidarse; los usuarios vuelven a iniciar sesión.
- **Correo / secretos:** rotar `Email__ApiKey`, `ADMIN_SEED_PASSWORD` y cadena DB según [HANDOVER.md](HANDOVER.md) si hubo filtración o cambio de responsable.
- La aplicación **no** hace respaldos sola.

## Diagrama de infraestructura

Fuente: `render.yaml`, `Dockerfile`, este documento.

```mermaid
flowchart TB
  subgraph Clientes
    Nav[Navegador]
  end
  subgraph Render_cloud["Render"]
    Web[Web service sipitex<br/>Docker Dockerfile<br/>SDK 10.0.401 → aspnet 10.0.9<br/>PORT / healthCheckPath /healthz]
    PG[(PostgreSQL 16<br/>sipitex-db / sipitex)]
  end
  subgraph Externos
    Mail[Brevo u otro Email__Provider]
    GH[GitHub main + Deploy Hook CI]
  end
  Nav -->|HTTPS| Web
  Web -->|Npgsql ConnectionStrings / DATABASE_URL| PG
  Web -->|API correo| Mail
  GH -->|build / autoDeploy| Web
```
