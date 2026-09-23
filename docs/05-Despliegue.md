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

`Seed:DemoUsers` debe quedar en `false` salvo demos. Defina `ADMIN_SEED_PASSWORD` para el administrador inicial (`admin@sipitex.local`) si la base está vacía. Los usuarios `*@sipitex.test` no se crean en este entorno.

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
| `EMAIL_SMTP_USER` | `Email__User` → `Email:User` |
| `EMAIL_SMTP_PASSWORD` | `Email__Password` → `Email:Password` |

## 5.6 Reportes y alertas

- **Reportes** (`/Reportes`): PDF (QuestPDF) y Excel (ClosedXML) de Inventario, Órdenes, Calidad y Dashboard.
- **Alertas** (`/Alertas`): cada actor activa/desactiva notificaciones (stock bajo, solicitudes pendientes, órdenes por vencer/atrasadas, reprocesos). Botón de correo de prueba.
- Sin usuario SMTP (`Email:User` vacío) los correos se guardan en `email-outbox/` aunque `Email:Enabled=true`.
- **Trazabilidad** (`/Trazabilidad`): códigos únicos de prenda y QR.

### Credenciales SMTP — no guardarlas en appsettings

`Email:User` y `Email:Password` **nunca** deben llenarse en `appsettings.json` ni en `appsettings.Development.json`.  
Se configuran por variable de entorno o user-secrets. ASP.NET Core mapea `Email__Password` → `Email:Password` automáticamente hacia `EmailOptions`.

**Docker / producción:** use `.env` (ver `.env.example`) o variables del orquestador.

**Desarrollo local (sin Docker):**

```bash
dotnet user-secrets init --project src/Sipitex.Web
dotnet user-secrets set "Email:User" "tu-usuario@smtp" --project src/Sipitex.Web
dotnet user-secrets set "Email:Password" "xxxx" --project src/Sipitex.Web
```

Para activar el envío real, defina `EMAIL_SMTP_USER` y `EMAIL_SMTP_PASSWORD` (Compose o user-secrets). `Email:Enabled` queda en `true`; sin usuario SMTP el canal sigue siendo Outbox.

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
2. **Web Service** — New → Web Service, repo de SIPITEX, runtime **Docker**, `Dockerfile` en la raíz. Health check: `/health`.
3. Variables de entorno del Web Service (sin pegar contraseñas en documentación ni en git):

| Variable | Valor |
|----------|--------|
| `ConnectionStrings__DefaultConnection` | Cadena interna de la BD Render (`postgresql://…` o formato Npgsql). Añada `SSL Mode=Require` si usa la URL externa. |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Seed__DemoUsers` | `true` solo en demo; `false` en operación real |
| `ADMIN_SEED_PASSWORD` | Contraseña del admin bootstrap (si no hay administradores) |
| `Email__Enabled` | `false` hasta tener SMTP; `true` en producción con usuario SMTP |
| `Email__Host` / `Email__From` / `Email__User` / `Email__Password` | Solo si hay SMTP |
| `Costing__LaborHourRate` | `6500` (referencia; el admin puede cambiarla en `/Costos`) |

4. El primer arranque ejecuta `MigrateAsync` y crea el esquema en la BD administrada.
5. Compruebe `https://<servicio>.onrender.com/health` → `200` y cuerpo `Healthy`.
6. La pantalla de login es `https://<servicio>.onrender.com/Account/Login`. Con `Seed__DemoUsers=true` valen los usuarios demo; si no, el admin de `ADMIN_SEED_PASSWORD`.

Los datos **persisten** en Postgres administrado entre reinicios del Web Service (a diferencia del SQLite efímero en disco del contenedor).

La URL pública la asigna Render al crear el servicio (`https://<nombre>.onrender.com`). No se publica aquí una URL concreta porque depende de la cuenta y del nombre del servicio.

### Blueprint

`render.yaml` en la raíz describe el Web Service (Dockerfile) + Postgres 16. En el dashboard: New → Blueprint → seleccionar el repo. Las credenciales y el resto de variables listadas abajo van con `sync: false`: Render las pide en el panel y **no** quedan en git.

## 5.11 Variables de entorno a configurar manualmente en Render antes del primer arranque

Completar en el panel del Web Service (Environment). **No** pegue valores reales en el repositorio ni en esta guía.

- [ ] `ConnectionStrings__DefaultConnection` — la genera Render al vincular `sipitex-db` (`fromDatabase.connectionString` en el Blueprint).
- [ ] `Seed__DemoUsers` — `true` solo en una demo; `false` en operación.
- [ ] `ADMIN_SEED_PASSWORD` — contraseña del administrador inicial (`admin@sipitex.local`) si la base no tiene administradores.
- [ ] `Email__Enabled` — `true` cuando haya SMTP; `false` si aún no.
- [ ] `Email__User` — usuario SMTP (vacío en `appsettings.json`; solo aquí).
- [ ] `Email__Password` — contraseña o app password SMTP (vacío en `appsettings.json`; solo aquí).
- [ ] `Costing__LaborHourRate` — tarifa de hora de mano de obra (referencia de negocio: 6500 COP/hora; el admin puede cambiarla luego en `/Costos`).

Sin `Email__User` / `Email__Password`, aunque `Email__Enabled=true`, los correos siguen yendo al outbox (`email-outbox/`). El primer arranque aplica `MigrateAsync` sobre la Postgres administrada.

### SMTP de Gmail para la demo (no pegar secretos aquí)

El proveedor por defecto en `appsettings.json` ya es Gmail: `Email:Host=smtp.gmail.com`, `Email:Port=587`, `Email:UseSsl=true` (MailKit usa STARTTLS en el 587). No haga falta otra variable de host si el buzón es Gmail.

`IsSmtpConfigured` es verdadero solo si `Email:Enabled`, `Email:Host`, `Email:From` y `Email:User` tienen valor. Con `Email:User` vacío el canal es Outbox aunque `Enabled` sea true. La contraseña no entra en esa condición, pero Gmail la exige al autenticar.

En el Web Service de Render (Environment), completar y redeploy:

| Variable | Valor |
|----------|--------|
| `Email__Enabled` | `true` |
| `Email__User` | Correo Gmail que envía (el mismo de la cuenta) |
| `Email__Password` | Contraseña de aplicación de Google, 16 caracteres. La contraseña normal de la cuenta la rechaza SMTP. |
| `Email__From` | El mismo correo que `Email__User`. El `From` de `appsettings.json` (`sipitex@tudominio.com`) no es un buzón real y Gmail lo rechaza. |

No defina `Email__Host`, `Email__Port` ni `Email__UseSsl` en blanco: una variable vacía pisa el valor de `appsettings.json` y el canal vuelve a Outbox. Docker Compose local usa los mismos nombres (`Email__User` / `Email__Password`) a partir de `EMAIL_SMTP_USER` y `EMAIL_SMTP_PASSWORD` en `.env`.

Cómo comprobar el canal, sin abrir el secreto:

- Log de envío real: `channel = SMTP`.
- Log de archivo: `channel = Outbox`.
- En `/Alertas`, el correo de prueba responde «enviado por SMTP» o «enviado por Outbox».
- Recuperar contraseña en `/Account/ForgotPassword` debe dejar un código de 6 dígitos en la bandeja real. El código no se escribe en los logs.
