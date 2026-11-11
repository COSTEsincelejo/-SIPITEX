# Parametrización — SIPITEX

Fuentes canónicas: [`src/Sipitex.Web/appsettings.json`](../../src/Sipitex.Web/appsettings.json), [`.env.example`](../../.env.example), y claves en BD (`AppSettings`). Las variables de entorno usan el prefijo con `__` (doble guion bajo).

## 1. Conexión y arranque

| Clave | Origen | Default / ejemplo | Notas |
|-------|--------|-------------------|-------|
| `ConnectionStrings:DefaultConnection` | appsettings | `Host=localhost;Port=5432;Database=sipitex;Username=sipitex;Password=sipitex` | Solo útil fuera de Production loopback |
| `DATABASE_URL` | entorno | — | Preferida en producción (Render) |
| `ConnectionStrings__DefaultConnection` | entorno | — | Alternativa; rechazada si es loopback en Production |
| `ASPNETCORE_ENVIRONMENT` | entorno | `Development` / `Production` | Afecta cookies Secure y selección de cadena |
| `Seed:DemoUsers` / `Seed__DemoUsers` | appsettings / entorno | **`false`** en appsettings | Demo apagada por defecto |
| `SEED_DEMO_DATA` | entorno | no definir en prod | Solo demos locales |
| `ADMIN_SEED_PASSWORD` | entorno | — | ≥ 6 caracteres; bootstrap `admin@sipitex.local` |
| `App:BaseUrl` / `App__BaseUrl` | appsettings / entorno | `""` | URL pública (enlaces correo) |
| `PORT` | hosting | inyectado | No fijar 8080 en panel Render |

## 2. Correo (`Email` / `Email__*` / aliases `.env.example`)

| Clave | Default appsettings | Notas |
|-------|---------------------|-------|
| `Email:Enabled` | `true` | `false` → fallo visible, no simula envío |
| `Email:Provider` | `Brevo` | `Brevo`, `Resend`, `Smtp`, `Outbox` |
| `Email:ApiKey` / `EMAIL_API_KEY` | `""` | Obligatorio Brevo/Resend |
| `Email:FromAddress` / `EMAIL_FROM_ADDRESS` | `""` | Remitente verificado |
| `Email:FromName` / `EMAIL_FROM_NAME` | `SIPITEX` | |
| `Email:Host` / `Port` / `User` / `Password` / `From` / `UseSsl` | vacíos / 587 / true | Solo SMTP |
| `Email:OutboxPath` | `email-outbox` | Legacy disco; outbox también en BD (`EmailOutboxMessage`) |

Brevo incompleto sin ApiKey + FromAddress → error de configuración (ver FAQ).

## 3. Costeo — `LaborHourRate` y `AppSetting`

| Clave | Default | Persistencia |
|-------|---------|--------------|
| `Costing:LaborHourRate` / `Costing__LaborHourRate` | **6500** (COP/hora de referencia) | appsettings / entorno |
| Clave BD `Costing.LaborHourRate` | — | Tabla `AppSettings` vía `CostingSettingsService`; UI `/Costos` (Administrador) |

La tarifa de referencia **no se suma** automáticamente al costo de la prenda hasta que el centro active esa regla de negocio (ver HANDOVER).

## 4. Seguridad

| Clave | Default | Efecto |
|-------|---------|--------|
| `Seguridad:MinutosInactividad` / `Seguridad__MinutosInactividad` | **20** | `ExpireTimeSpan` de la cookie `sipitex.auth` (mínimo efectivo 2) |

Cookies auth/antiforgery: `SameAsRequest` en Development/tests; **`Always`** en Production.

## 5. Privacidad (`Privacidad` / `Privacidad__*`)

Valores vacíos o literal `PENDIENTE` **no se publican**. Tabla completa: [`docs/PRIVACIDAD-DATOS-INSTITUCION.md`](../PRIVACIDAD-DATOS-INSTITUCION.md).

| Clave (appsettings) | Default actual en repo |
|---------------------|------------------------|
| `Version` | `1.1` |
| `FechaActualizacion` | `2026-11-06` |
| `ResponsableNombre` | `SENA, Centro de Manufactura en Textil y Cuero (CMTC)` |
| `Ciudad` | `Bogotá D.C.` (confirmar sede) |
| `CorreoContacto`, `Direccion`, `Telefono`, `NIT`, `HorarioAtencion`, `PlazoConservacion`, `ProveedorCorreo`, `PaisProveedorCorreo`, `ProveedorHosting`, `PaisProveedorHosting`, `AreaSolicitudes`, `UrlPoliticaInstitucional` | `""` |

## 6. Soporte (Ayuda)

| Clave | Default |
|-------|---------|
| `Soporte:Responsable` | `Coordinación CMTC SENA` |
| `Soporte:Correo` | `""` |

## 7. Logging

`Logging:LogLevel` — Default `Information`; `Microsoft.AspNetCore` / `Microsoft.EntityFrameworkCore` en `Warning`; `Sipitex` en `Information`.

## 8. Docker / compose (recordatorio)

`docker-compose` fija `Seed__DemoUsers=false`. PostgreSQL de compose: usuario/clave/base `sipitex` (ver `.env.example` `POSTGRES_*`).
