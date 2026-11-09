# SIPITEX — Sistema Integrado de Aprendizaje Producción e Inventario Textil

Proyecto .NET 10 con **arquitectura por capas** y desarrollo guiado por **metodología cascada** (CMTC · SENA · ADSO).

## Estructura de la solución

```
Sipitex/
├── docs/                          # Documentación cascada (fases 1–5)
├── src/
│   ├── Sipitex.Domain/            # Entidades, enums (capa de dominio)
│   ├── Sipitex.Application/       # Servicios, DTOs, contratos (lógica de negocio)
│   ├── Sipitex.Infrastructure/    # EF Core, PostgreSQL, repositorios (acceso a datos)
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
- PostgreSQL 16 (local, Docker Compose o instancia administrada)

## Ejecución local

```powershell
cd src/Sipitex.Web
dotnet run
```

Abrir `https://localhost:5xxx` (el puerto se muestra en consola). La raíz abre el panel de inicio (requiere autenticación). El stock se consulta en **Plantas de inventario**.

Motor de datos: **PostgreSQL 16** (`ConnectionStrings:DefaultConnection`), creado al iniciar con migraciones EF Core (`MigrateAsync`). Para desarrollo local: `docker compose up db` o un Postgres en `localhost:5432` (usuario/clave/base `sipitex`).

### Usuarios demo (solo Development)

Los usuarios, materiales, órdenes y fichas de demostración se siembran solo si `SEED_DEMO_DATA=true`. Si esa variable no está, se usa `Seed:DemoUsers` (`true` en Development, `false` en producción). En producción las credenciales **no** se muestran en la pantalla de login.

| Correo | Contraseña | Rol |
|--------|------------|-----|
| `admin@sipitex.test` | `Admin123!` | Administrador |
| `instructor@sipitex.test` | `Instructor123!` | Instructor |
| `bodega@sipitex.test` | `Bodega123!` | Encargado de bodega |

Si un entorno de producción arranca **sin ningún Administrador** y `ADMIN_SEED_PASSWORD` tiene al menos 6 caracteres, se crea `admin@sipitex.local`. La contraseña no se escribe en el log. Si la variable falta, no se crea la cuenta.

Para el demo público puede reactivarse el seed con `SEED_DEMO_DATA=true`. En la operación real de Render esa variable no se define.

### Producción (Render)

El blueprint `render.yaml` deja estos valores. Los secretos siguen en `sync: false`: se escriben en el panel y no van en git.

| Variable | Valor en producción |
|----------|---------------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Seed__DemoUsers` | `false` (igual que `Seed:DemoUsers` en `appsettings.json`). No defina `SEED_DEMO_DATA`. |
| `ADMIN_SEED_PASSWORD` | Obligatoria en el panel, 6 caracteres o más. Crea `admin@sipitex.local` solo si todavía no hay un administrador. |
| `Email__Enabled` | `true` |
| `Email__Provider` | `Brevo` (`appsettings.json` también trae Brevo; Development sigue en Resend) |
| `Email__ApiKey` | Clave de la API de Brevo. Obligatoria en el panel. |
| `Email__FromAddress` | Remitente verificado en Brevo. Obligatoria en el panel. |
| `Email__FromName` | `SIPITEX` |

Si faltan la clave o el remitente, el arranque registra que el proveedor Brevo está incompleto y el envío responde «No pudimos enviar el correo, intente de nuevo». `Resend`, `Smtp` y `Outbox` siguen aceptados si se cambia `Email__Provider`.

## Docker Compose

```bash
docker compose up --build
```

Abrir `http://localhost:8080`. Postgres persiste en el volumen `sipitex-pgdata`.

Fotos de perfil, correos sin SMTP y claves de cookie viven en PostgreSQL. El respaldo con `pg_dump` y las ramas de Neon están en [`docs/BACKUP.md`](docs/BACKUP.md). Para copiar una base anterior se usa `tools/Sipitex.DataMigration` (`SOURCE_CONNECTION`, `DATABASE_URL`, `--dry-run`); no corre al arrancar la aplicación.

## Módulos

| Módulo | Ruta | Descripción |
|--------|------|-------------|
| Órdenes | `/Ordenes` | Órdenes de producción, MES y avance |
| MRP | `/Mrp` | BOM de la prenda y simulación de requerimientos |
| Fichas | `/Fichas` | Registro de producción por grupo SENA |
| Plantas de inventario | `/PlantasInventario` | Stock, filtros, ficha técnica del material y catálogo |
| Importar inventario | `/Inventario/Importar` | Carga Excel del inventario inicial (solo administrador). `/Inventario` redirige a Plantas de inventario |
| Solicitudes de planta | `/PlantasInventarioSolicitudes` | Cola de solicitudes de material |
| Órdenes de planta | `/PlantasInventarioOrdenes` | Entrega / reingreso de materiales de orden |
| Grupos de confección | `/GruposConfeccion` | Horas y prendas por grupo |
| Consumos | `/Consumos` | Consumo real de materiales por orden |
| Costeo | `/Costos` | Costo de materiales de la prenda. Si falta un costo, muestra «costo incompleto». La mano de obra no entra en el total |
| Actas | `/Actas` | Actas de ingreso/egreso con firma gráfica |
| Trazabilidad | `/Trazabilidad` | Código único de prenda y línea de tiempo |
| Calidad | `/Calidad` | Inspecciones de calidad |
| Auditoría | `/Auditoria` | Activity log (Administrador) |
| Estadísticas | `/Estadisticas` | KPIs y gráficos |
| Reportes | `/Reportes` | Exportación PDF / Excel |
| Alertas | `/Alertas` | Preferencias de correo por actor |
| Usuarios | `/Account/Users` | CRUD de usuarios (Administrador) |

## Políticas de negocio documentadas

- **Actas:** conformidad con nombre, cargo, timestamp UTC y **firma gráfica** dibujada en pantalla (incluida en el PDF).
- **Costeo:** tarifa de hora de mano de obra con valor de referencia `Costing:LaborHourRate` (6500 COP/h). El Administrador puede ajustarla en `/Costos`. Esa tarifa no se suma al costo de la prenda. El total usa solo lo consumido, con el costo congelado en la entrega. Si hay entregas sin consumo, avisa «Hay materiales entregados sin consumo registrado» y no muestra un total en cero. Si una entrega, un consumo o un material de la ficha no tiene costo, muestra «costo incompleto».
- **Correo:** en producción el canal es **Brevo** por HTTPS (puerto 443). El plan gratis de Render bloquea SMTP (puertos 25, 465 y 587). `Resend` sigue disponible cambiando `Email__Provider`. SMTP queda como alternativa con `Email__Provider=Smtp`.

### Variables de correo en Render

Crearlas en el panel del Web Service. No pegue los valores en el repositorio.

| Variable | Para qué sirve |
|----------|----------------|
| `Email__Enabled` | `true` para enviar. En `false` el envío falla con un mensaje visible y no se simula una entrega. |
| `Email__Provider` | En producción `Brevo`. También `Resend`, `Smtp` o `Outbox`. |
| `Email__ApiKey` | Clave del proveedor API. En producción, la de Brevo. |
| `Email__FromAddress` | Correo remitente verificado en ese proveedor. |
| `Email__FromName` | Nombre que ve el destinatario, por ejemplo `SIPITEX`. |
| `Email__Host` | Servidor SMTP. Solo si `Email__Provider=Smtp`. |
| `Email__User` | Usuario SMTP. Solo si `Email__Provider=Smtp`. |
| `Email__Password` | Contraseña SMTP. Solo si `Email__Provider=Smtp`. |

Si faltan `Email__ApiKey` o `Email__FromAddress`, el arranque escribe un error en el log y la pantalla dice «No pudimos enviar el correo, intente de nuevo». Ese intento no deja el código vigente ni consume el minuto de reenvío. `Email__Provider=Outbox` guarda el mensaje en `EmailOutboxMessages` y no lo entrega.
- **Login:** 5 intentos fallidos (correo + IP) bloquean 15 minutos. El mensaje no indica si el correo existe.

## Privacidad y datos personales

La política publicada está en `/Privacidad` (versión 1.1, 2026-11-06). **Este texto no es asesoría legal:** debe revisarlo el área jurídica o el oficial de datos del centro antes de tratarlo como definitivo. Lo que falta por decidir está en [`PENDIENTES-PRIVACIDAD.md`](PENDIENTES-PRIVACIDAD.md).

Quien aceptó una versión anterior no entra a las funciones hasta aceptar la vigente. Cada aceptación guarda versión, fecha UTC e IP, y no borra las anteriores. En Mi perfil, la sección Mis datos muestra la cuenta, el historial de aceptación y permite pedir rectificación, supresión o revocatoria. Esa solicitud queda en Auditoría con fecha, tipo y estado `recibida`.

La sección `Privacidad` se lee al arrancar. Si un dato está vacío, la página no lo muestra y el log escribe una advertencia. La página no dice el nombre del archivo de configuración.

| Clave | Uso |
|-------|-----|
| `Version` | Versión vigente. Hoy `1.1`. Si cambia, hay que aceptar de nuevo. |
| `FechaActualizacion` | Fecha que muestra la política. Hoy `2026-11-06`. |
| `ResponsableNombre` | Nombre del responsable. |
| `Ciudad` | Ciudad que se muestra junto al responsable. |
| `CorreoContacto` | Correo para ejercer derechos. |
| `Direccion` | Dirección de notificación. |
| `Telefono` | Teléfono de contacto. |
| `NIT` | NIT, solo si el centro lo confirma. |
| `HorarioAtencion` | Horario de atención. |
| `PlazoConservacion` | Plazo posterior al cierre de la cuenta. |
| `ProveedorCorreo` | Nombre del proveedor de correo. |
| `PaisProveedorCorreo` | País de ese proveedor. |
| `ProveedorHosting` | Nombre de quien aloja el sistema y la base. |
| `PaisProveedorHosting` | País de ese proveedor. Si no es Colombia, la política informa la transmisión o transferencia internacional. |
| `AreaSolicitudes` | Área o cargo que atiende las solicitudes. |
| `UrlPoliticaInstitucional` | Enlace http(s) a la política de datos del SENA. |

En desarrollo los ejemplos van vacíos en `appsettings.Development.json`, salvo el nombre del responsable, la ciudad, la versión y la fecha. En Render se pueden definir como variables `Privacidad__CorreoContacto`, `Privacidad__Direccion`, `Privacidad__Telefono` y el resto de claves con el mismo prefijo.

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
- Entity Framework Core + PostgreSQL (Npgsql)  
- Chart.js (estadísticas)  
- Font Awesome + Inter (UI)  
- Docker Compose  
