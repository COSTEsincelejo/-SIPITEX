# Fase 5 — Despliegue y mantenimiento (Cascada)

## 5.1 Publicación en intranet (Windows)

```powershell
cd src/Sipitex.Web
dotnet publish -c Release -o ./publish
```

Copiar carpeta `publish` al servidor IIS o ejecutar:

```powershell
./publish/Sipitex.Web.exe
```

## 5.2 Configuración

`appsettings.json` / `appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=sipitex;Username=sipitex;Password=sipitex"
}
```

El motor es **PostgreSQL 16** (Npgsql + EF Core). La cadena de conexión se lee de `ConnectionStrings:DefaultConnection`. En producción use variables de entorno (`ConnectionStrings__DefaultConnection`) y no deje credenciales en el repositorio.

`SEED_DEMO_DATA` debe quedar sin definir (o en `false`) salvo demos. `Seed:DemoUsers` en `appsettings.json` ya está en `false`. Defina `ADMIN_SEED_PASSWORD` para el administrador inicial (`admin@sipitex.local`) si la base no tiene administradores. Los usuarios `*@sipitex.test` no se crean en este entorno.

`Costing:LaborHourRate` tiene valor de referencia **6500** (COP/hora). El Administrador puede cambiarlo en `/Costos`; el valor queda en `AppSettings`.

## 5.3 IIS (opcional)

1. Instalar [ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/10.0)  
2. Crear sitio apuntando a `publish`  
3. Pool: **Sin código administrado**  
4. PostgreSQL accesible desde el servidor (local, Docker o administrado)

## 5.4 Mantenimiento

| Tarea | Frecuencia |
|-------|------------|
| Respaldo PostgreSQL (`pg_dump`) | Diario |
| Revisión logs | Semanal |
| Actualización paquetes NuGet | Mensual |

```bash
pg_dump -Fc -d sipitex -f sipitex.dump
```

## 5.5 Docker Compose (RNF07)

```bash
# Copiar plantilla de secretos y completar usuario/contraseña SMTP (opcional)
cp .env.example .env

docker compose up --build
```

Levanta **postgres:16** (`db`) y la app. Persistencia en el volumen `sipitex-pgdata`.  
La aplicación queda en `http://localhost:8080`.  
Health check: `http://localhost:8080/health` (sin autenticación).

| Variable de entorno | Config ASP.NET |
|---------------------|----------------|
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | Cadena `ConnectionStrings__DefaultConnection` |
| `EMAIL_PROVIDER` | `Email__Provider` → `Email:Provider` (`Resend`, `Brevo`, `Smtp`, `Outbox`) |
| `EMAIL_API_KEY` | `Email__ApiKey` → `Email:ApiKey` |
| `EMAIL_FROM_ADDRESS` | `Email__FromAddress` → `Email:FromAddress` |
| `EMAIL_FROM_NAME` | `Email__FromName` → `Email:FromName` |
| `EMAIL_SMTP_USER` | `Email__User` → `Email:User` (solo si `Email:Provider=Smtp`) |
| `EMAIL_SMTP_PASSWORD` | `Email__Password` → `Email:Password` (solo si `Email:Provider=Smtp`) |

## 5.6 Reportes y alertas

- **Reportes** (`/Reportes`): PDF (QuestPDF) y Excel (ClosedXML) de Inventario, Órdenes, Calidad y Dashboard.
- **Alertas** (`/Alertas`): cada actor activa/desactiva notificaciones (stock bajo, solicitudes pendientes, órdenes por vencer/atrasadas, reprocesos). Botón de correo de prueba.
- El canal predeterminado es **Resend** (HTTPS, puerto 443). Render en plan gratis bloquea SMTP en los puertos 25, 465 y 587.
- Sin `Email:ApiKey` y `Email:FromAddress` el arranque registra un error y el envío no se da por exitoso. El código de confirmación se invalida y no se consume el minuto de espera.
- `Email:Provider=Smtp` conserva MailKit. `Email:Provider=Outbox` guarda el mensaje en `EmailOutboxMessages` y no lo entrega.
- **Trazabilidad** (`/Trazabilidad`): códigos únicos de prenda y QR.

### Credenciales SMTP — no guardarlas en appsettings

`Email:ApiKey`, `Email:User` y `Email:Password` **nunca** deben llenarse en `appsettings.json` ni en `appsettings.Development.json`.  
Se configuran por variable de entorno o user-secrets. ASP.NET Core mapea `Email__ApiKey` → `Email:ApiKey` automáticamente hacia `EmailOptions`.

**Docker / producción:** use `.env` (ver `.env.example`) o variables del orquestador.

**Desarrollo local (sin Docker):**

```bash
dotnet user-secrets init --project src/Sipitex.Web
dotnet user-secrets set "Email:Provider" "Resend" --project src/Sipitex.Web
dotnet user-secrets set "Email:ApiKey" "re_..." --project src/Sipitex.Web
dotnet user-secrets set "Email:FromAddress" "notificaciones@tudominio.com" --project src/Sipitex.Web
dotnet user-secrets set "Email:FromName" "SIPITEX" --project src/Sipitex.Web
```

Para volver a SMTP (no funciona en el plan gratis de Render): `Email:Provider=Smtp` más `Email:User` y `Email:Password`.

## 5.7 Base de datos y migraciones EF Core

El esquema se aplica con **migraciones EF Core** (`MigrateAsync` al arrancar), no con `EnsureCreated`. El proveedor es **PostgreSQL** (`UseNpgsql`).

Antes de `MigrateAsync`, `MigrationBaseline.EnsureBaselineAsync` detecta BD legacy (tiene tablas de negocio pero no `__EFMigrationsHistory`) y marca `InitialCreate` como ya aplicada **sin borrar datos**. El flujo es el mismo en PostgreSQL; las consultas de catálogo usan `information_schema` (no `sqlite_master`).

```bash
# Crear una nueva migración (desarrollo)
dotnet ef migrations add NombreCambio \
  --project src/Sipitex.Infrastructure \
  --startup-project src/Sipitex.Web
```

- **Instalación limpia** (Postgres vacío): `MigrateAsync` crea el esquema completo y el seed de demo.
- **BD legacy completa** (EnsureCreated / SQL manual, sin historial): se aplica baseline automático y luego `MigrateAsync` no vuelve a crear tablas.
- **BD legacy incompleta** (faltan columnas/tablas del modelo actual): el baseline **no** se aplica y el arranque falla con mensaje explícito (para no ocultar el desfase).

Las migraciones SQLite anteriores se reemplazaron por un `InitialCreate` limpio para PostgreSQL. No hay ruta automática de conversión de un archivo `sipitex.db` existente: exporte datos si hace falta y arranque contra Postgres vacío.

### Antes de desplegar en CMTC / producción

```bash
pg_dump -Fc -d sipitex -f sipitex.dump
```

Haga el backup **manualmente** antes del primer arranque con una versión nueva. El código de arranque no lo automatiza.

## 5.8 Roadmap post-MVP

- API REST para integraciones  
- Autenticación JWT para clientes externos  

## 5.9 Entregable de fase

Sistema operativo en intranet o Render + PostgreSQL + manual de operación.

## 5.10 Despliegue en Render (app + Postgres)

Este agente **no crea** el servicio en la consola de Render. Hay que vincular el repositorio en [Render](https://render.com) (Blueprint `render.yaml` o alta manual).

### Alta manual

1. **PostgreSQL** — New → PostgreSQL, plan Starter (o el disponible), versión 16. Anote la **Internal Database URL**.
2. **Web Service** — New → Web Service, repo de SIPITEX, runtime **Docker**, `Dockerfile` en la raíz. Health check: `/healthz`. La guía operativa corta está en `docs/DEPLOY.md`.
3. Variables de entorno del Web Service (sin pegar contraseñas en documentación ni en git):

| Variable | Valor |
|----------|--------|
| `ConnectionStrings__DefaultConnection` | Cadena interna de la BD Render (`postgresql://…` o formato Npgsql). Añada `SSL Mode=Require` si usa la URL externa. |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Seed__DemoUsers` | `true` solo en demo; `false` en operación real |
| `ADMIN_SEED_PASSWORD` | Contraseña del admin bootstrap (si no hay administradores) |
| `Email__Enabled` | `true` para enviar |
| `Email__Provider` | `Resend` (recomendado), `Brevo`, `Smtp` o `Outbox` |
| `Email__ApiKey` | Clave de Resend o Brevo. No va en git |
| `Email__FromAddress` | Remitente verificado en el proveedor |
| `Email__FromName` | `SIPITEX` |
| `Email__Host` / `Email__User` / `Email__Password` | Solo si `Email__Provider=Smtp` |
| `Costing__LaborHourRate` | `6500` (referencia; el admin puede cambiarla en `/Costos`) |

4. El primer arranque ejecuta `MigrateAsync` y crea el esquema en la BD administrada.
5. Compruebe `https://<servicio>.onrender.com/healthz` → `200` y cuerpo `ok`. `/version` devuelve el commit en JSON.
6. La pantalla de login es `https://<servicio>.onrender.com/Account/Login`. Con `SEED_DEMO_DATA=true` valen los usuarios demo; si no, el admin de `ADMIN_SEED_PASSWORD`.

Los datos **persisten** en Postgres administrado entre reinicios del Web Service (a diferencia del SQLite efímero en disco del contenedor).

La URL pública la asigna Render al crear el servicio (`https://<nombre>.onrender.com`). No se publica aquí una URL concreta porque depende de la cuenta y del nombre del servicio.

### Blueprint

`render.yaml` en la raíz describe el Web Service (Dockerfile) + Postgres 16. En el dashboard: New → Blueprint → seleccionar el repo. Las credenciales y el resto de variables listadas abajo van con `sync: false`: Render las pide en el panel y **no** quedan en git.

## 5.11 Variables de entorno a configurar manualmente en Render antes del primer arranque

Completar en el panel del Web Service (Environment). **No** pegue valores reales en el repositorio ni en esta guía.

- [ ] `ConnectionStrings__DefaultConnection` — la genera Render al vincular `sipitex-db` (`fromDatabase.connectionString` en el Blueprint).
- [ ] `SEED_DEMO_DATA` — `true` solo en una demo; en operación no la defina (o `false`).
- [ ] `ADMIN_SEED_PASSWORD` — contraseña del administrador inicial (`admin@sipitex.local`) si la base no tiene administradores.
- [ ] `Email__Enabled` — `true` para enviar códigos y alertas.
- [ ] `Email__Provider` — `Resend` en Render (HTTPS). `Smtp` solo si el plan permite salida 587.
- [ ] `Email__ApiKey` — clave del proveedor. Vacía en el repositorio.
- [ ] `Email__FromAddress` — correo remitente verificado en Resend o Brevo.
- [ ] `Email__FromName` — `SIPITEX`.
- [ ] `Email__User` / `Email__Password` — solo con `Email__Provider=Smtp`.
- [ ] `Costing__LaborHourRate` — tarifa de hora de mano de obra (referencia de negocio: 6500 COP/hora; el admin puede cambiarla luego en `/Costos`).

Si faltan `Email__ApiKey` o `Email__FromAddress`, el arranque escribe un error y la pantalla de confirmación dice que no se pudo enviar el correo. El código no queda vigente y no se consume el minuto de reenvío. El primer arranque aplica `MigrateAsync` sobre la Postgres administrada. Las claves de Data Protection también quedan en esa base (`DataProtectionKeys`), así que las cookies sobreviven a un redeploy.
