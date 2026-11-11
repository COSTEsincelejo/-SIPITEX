# FAQ / troubleshooting — SIPITEX

Problemas frecuentes de arranque y operación. Complementa [`docs/HANDOVER.md`](../HANDOVER.md) y [`docs/DEPLOY.md`](../DEPLOY.md).

## 1. Conexión a PostgreSQL

### Síntoma: la app no arranca; pide `DATABASE_URL`

En Production la cadena válida debe venir de:

- `DATABASE_URL` (URL `postgres://` / `postgresql://` o cadena Npgsql), **o**
- `ConnectionStrings__DefaultConnection` **si el host no es loopback**.

`appsettings.json` con `Host=localhost` **no** se usa en Production (`PostgresConnectionStrings.SelectRaw`).

### Síntoma: Production con `localhost` / `127.0.0.1` / `::1`

Protección anti-arranque accidental: si `ConnectionStrings__DefaultConnection` apunta a loopback en Production, se trata como ausente y el arranque aborta. Use el host real de Neon/Render/Docker network (`db`, FQDN, etc.).

### Síntoma: URL de Render no abre

`DATABASE_URL` tipo `postgresql://user:pass@host/db` se normaliza a cadena Npgsql y fuerza `SslMode=Require` y `GssEncryptionMode=Disable` (imagen aspnet sin libgssapi).

## 2. Migraciones EF Core

Al iniciar, `DbInitializer` ejecuta `Database.MigrateAsync()`.

| Situación | Qué hacer |
|-----------|-----------|
| Modelo cambió y CI falla `has-pending-model-changes` | Generar migración: `dotnet ef migrations add … --project src/Sipitex.Infrastructure --startup-project src/Sipitex.Web` |
| Base vacía | Arranque aplica el historial de migraciones |
| Tras restore de backup antiguo | Validar Data Protection / sesiones; ver [`BACKUP.md`](../BACKUP.md) |

## 3. Correo Brevo incompleto

Mensaje típico: *«Proveedor Brevo incompleto. Defina Email__ApiKey y Email__FromAddress.»*

Requisitos cuando `Email__Provider=Brevo` y el correo está habilitado:

- `Email__ApiKey` (o `EMAIL_API_KEY`)
- `Email__FromAddress` (o `EMAIL_FROM_ADDRESS`)
- `Email__FromName` recomendado (`SIPITEX`)
- `Email__Enabled=true` si debe enviar de verdad (`false` falla con mensaje visible; no simula entrega)

Plantilla: [`.env.example`](../../.env.example).

## 4. 403 / AccessDenied y tiles de Home

- Rutas con `[Authorize(Roles=…)]` o políticas redirigen a `/Account/AccessDenied` si el rol no alcanza (p. ej. importar inventario solo Administrador).
- En `/Home`, los **tiles** se filtran por rol (`Views/Home/Index.cshtml`):
  - Plantas de inventario → Administrador / Encargado de bodega
  - Fichas y Calidad → Administrador / Instructor
  - Otros módulos visibles según el mismo criterio del menú en `_Layout`
- Si un tile no aparece, suele ser filtrado de UI; si la URL se abre a mano sin rol, esperable **403 / AccessDenied**, no un menú vacío.

## 5. Importador sobrescribe stock

`/Inventario/Importar` **sobrescribe el stock** de un material existente cuando el Excel trae cantidad distinta (clave `código | nombre | planta`). Registra Entrada o Ajuste.

- Úselo en **carga inicial** o con respaldo previo ([`BACKUP.md`](../BACKUP.md), [`GUIA-INVENTARIO-INICIAL.md`](../GUIA-INVENTARIO-INICIAL.md)).
- Si la cantidad coincide, la fila se omite (idempotente).

## 6. Antiforgery / cookies en HTTP + Production (Docker)

En Production (y fuera del host de pruebas) las cookies de autenticación y antiforgery usan `CookieSecurePolicy.Always` (`Program.cs`).

| Escenario | Efecto |
|-----------|--------|
| HTTPS real (Render con TLS) | Cookies Secure OK |
| `docker compose` en **HTTP** (`http://localhost:8080`) con `ASPNETCORE_ENVIRONMENT=Production` | El navegador **no** guarda/envía cookies Secure → login / POST fallan (antiforgery 400 o sesión inexistente) |

Mitigaciones: servir HTTPS, poner un proxy que termine TLS, o usar Development/local con `SameAsRequest` solo en entornos no Production. El host de tests fuerza `SameAsRequest` aunque diga Production.

## 7. Otros atajos

| Problema | Pista |
|----------|-------|
| Seed demo en producción | `Seed__DemoUsers=false`; no definir `SEED_DEMO_DATA` |
| Sin administrador | `ADMIN_SEED_PASSWORD` (≥ 6) crea `admin@sipitex.local` solo si no hay Administrador |
| Sesión corta | `Seguridad__MinutosInactividad` (default **20**) |
| Privacidad vacía en pantalla | Valores vacíos o `PENDIENTE` no se publican — ver [`PRIVACIDAD-DATOS-INSTITUCION.md`](../PRIVACIDAD-DATOS-INSTITUCION.md) |
