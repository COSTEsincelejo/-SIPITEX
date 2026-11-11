# Entrega / handover SIPITEX

Documento de traspaso operativo del centro. Complementa el [README](../README.md), [DEPLOY.md](DEPLOY.md) y [BACKUP.md](BACKUP.md).

## Qué se entrega

- Aplicación ASP.NET Core MVC + PostgreSQL (capas Domain / Application / Infrastructure / Web).
- Roles: Administrador, Instructor, Encargado de bodega.
- Módulos de órdenes, MRP, plantas de inventario, solicitudes, consumos, calidad, actas, trazabilidad, reportes y alertas.
- Política de privacidad en `/Privacidad` (versión 1.1) alimentada por configuración `Privacidad__*`.
- Manuales: [MANUAL-ADMINISTRADOR.md](MANUAL-ADMINISTRADOR.md), [MANUAL-BODEGUERO.md](MANUAL-BODEGUERO.md), [MANUAL-INSTRUCTOR.md](MANUAL-INSTRUCTOR.md).
- Datos institucionales de privacidad: [PRIVACIDAD-DATOS-INSTITUCION.md](PRIVACIDAD-DATOS-INSTITUCION.md) y pendientes en [PENDIENTES-PRIVACIDAD.md](../PENDIENTES-PRIVACIDAD.md).
- Variables de entorno de ejemplo: [`.env.example`](../.env.example).
- Inventario inicial: [GUIA-INVENTARIO-INICIAL.md](GUIA-INVENTARIO-INICIAL.md) y plantilla en `docs/plantillas/`.

## Variables de entorno obligatorias (producción)

Definirlas en el panel del hosting (Render) o en el entorno. **No** pegar secretos en git.

| Variable | Obligatoria | Notas |
|----------|-------------|--------|
| `ASPNETCORE_ENVIRONMENT` | Sí | `Production` |
| `DATABASE_URL` **o** `ConnectionStrings__DefaultConnection` | Sí | PostgreSQL remoto. En Production **no** se acepta `localhost` / `127.0.0.1` (protección anti-arranque local accidental). |
| `Seed__DemoUsers` | Sí (valor fijo) | Debe ser `false`. **No** defina `SEED_DEMO_DATA`. |
| `ADMIN_SEED_PASSWORD` | Sí en el primer arranque | ≥ 6 caracteres. Crea `admin@sipitex.local` solo si aún no hay Administrador. Rotar después. |
| `Email__Enabled` | Sí | `true` para enviar; `false` falla con mensaje visible (no simula entrega). |
| `Email__Provider` | Sí | En producción: `Brevo` (HTTPS). También `Resend`, `Smtp`, `Outbox`. |
| `Email__ApiKey` | Sí si Brevo/Resend | Secreto del panel. |
| `Email__FromAddress` | Sí si el correo está activo | Remitente verificado. |
| `Email__FromName` | Recomendada | p. ej. `SIPITEX`. |
| `Email__Host` / `User` / `Password` | Solo si Smtp | Dejar vacíos o `configurar` si no usa SMTP. |
| `Costing__LaborHourRate` | Opcional | Tarifa de referencia; no se suma al costo de la prenda. |
| `Privacidad__*` | Pendientes institucionales | Ver sección abajo. Vacío o `PENDIENTE` = no se publica. |
| `App__BaseUrl` | Recomendada | URL pública (enlaces de correo / verificación). |
| `PORT` | La inyecta Render | No fijar 8080 en el panel de Render. |

Plantilla comentada: [`.env.example`](../.env.example). Detalle de despliegue: [DEPLOY.md](DEPLOY.md).

## Credenciales a rotar (no dejar las de demo en producción)

| Qué | Dónde | Acción |
|-----|--------|--------|
| Contraseñas demo (`Admin123!`, `Instructor123!`, `Bodega123!`) | Solo Development / seed | No usar en producción. Con `Seed__DemoUsers=false` no se siembran. |
| `ADMIN_SEED_PASSWORD` | Panel del hosting | Rotar tras el primer arranque si se usó para crear `admin@sipitex.local`. |
| `Email__ApiKey` (Brevo / Resend) | Panel del hosting | Rotar si se filtró o cambió de proveedor. |
| Contraseña de PostgreSQL | `DATABASE_URL` / Neon / Render | Rotar al cambiar de responsable de operaciones. |
| Cookies / Data Protection | Base PostgreSQL | Tras restaurar un backup antiguo, validar que las sesiones siguen cerrándose bien. |

No hay claves reales en el repositorio. Los tests usan marcadores ficticios de API y correos `@sipitex.test` / fixtures de laboratorio; no son cuentas personales ni de producción.

## Importador de inventario — advertencia

El importador de `/Inventario/Importar` **sobrescribe el stock** de un material existente cuando la fila del Excel trae una cantidad distinta (misma clave `código | nombre | planta`). Ajusta al valor del archivo y registra un movimiento de Entrada o Ajuste.

**Úselo solo en la carga inicial** (o con un respaldo previo: [BACKUP.md](BACKUP.md)). No lo ejecute sobre una planta en operación diaria sin revisar la simulación. Si la cantidad del archivo coincide con la base, la fila se omite (idempotente). Guía: [GUIA-INVENTARIO-INICIAL.md](GUIA-INVENTARIO-INICIAL.md).

## Variables `Privacidad__*` pendientes

Completar con el área jurídica. Vacío o el literal `PENDIENTE` **no se publican** en `/Privacidad` ni en el PDF de actas. Tabla completa: [PRIVACIDAD-DATOS-INSTITUCION.md](PRIVACIDAD-DATOS-INSTITUCION.md). Checklist: [PENDIENTES-PRIVACIDAD.md](../PENDIENTES-PRIVACIDAD.md).

| Variable | Estado típico al entregar |
|----------|---------------------------|
| `Privacidad__ResponsableNombre` | Puede venir prellenado (SENA / CMTC); confirmar |
| `Privacidad__NIT` | Pendiente |
| `Privacidad__Ciudad` | Confirmar (hoy puede figurar Bogotá D.C.; sede citada: Sincelejo) |
| `Privacidad__CorreoContacto` | Pendiente |
| `Privacidad__Direccion` | Pendiente |
| `Privacidad__Telefono` | Pendiente |
| `Privacidad__HorarioAtencion` | Pendiente |
| `Privacidad__AreaSolicitudes` | Pendiente |
| `Privacidad__PlazoConservacion` | Pendiente |
| `Privacidad__ProveedorCorreo` / `Privacidad__PaisProveedorCorreo` | Pendiente (p. ej. Brevo) |
| `Privacidad__ProveedorHosting` / `Privacidad__PaisProveedorHosting` | Pendiente (p. ej. Render) |
| `Privacidad__UrlPoliticaInstitucional` | Pendiente (http/https) |

## Deuda conocida y trabajo futuro

### Operativa / producto
1. **MRP no se calcula automáticamente** al crear una orden: hay que simular en `/Mrp` e importar requisitos a la OP.
2. **Producto terminado sin pantalla propia** en el menú: el stock PT vive en el flujo MES / reingreso; falta un módulo de consulta dedicado.
3. **Cancelar orden o anular acta no revierte stock** ni consumos ya aplicados.
4. **Nomenclatura no unificada:** «Ficha» (grupo SENA) vs ficha técnica BOM vs ficha del material; «Bodega» (rol) vs «Planta de inventario» (UI); «material» vs «insumo».
5. **`User.FichaAsignadaId` es solo informativo:** el alcance real del instructor va por `FichaInstructor` / etapas MES.
6. **Costeo excluye mano de obra del total** hasta que el centro defina y active la tarifa en el cálculo (hoy solo materiales; la tarifa de referencia se guarda pero no se suma).

### Técnica / legado
7. **Código legacy sin uso de producto:** `MaterialRequest` (solicitudes 1-ítem), RF/RNF + `RequirementService` (sin pantalla / DI incompleto), búsqueda antigua `/api/busqueda` frente a `Buscar/Sugerencias`.
8. **`BomItem → Material` en Cascade** y blockers de borrado de usuario incompletos (no listan consumos/actas/grupos/prendas) — riesgo si se borra fuera del servicio.
9. **Privacidad institucional incompleta** (NIT, contacto, proveedores, RNBD, menores). Ver `PENDIENTES-PRIVACIDAD.md`. Vacío o `PENDIENTE` no se publican.
10. **Seed demo:** no reactivar `SEED_DEMO_DATA` en el entorno real. Inventario inicial: [GUIA-INVENTARIO-INICIAL.md](GUIA-INVENTARIO-INICIAL.md).

## Arranque rápido post-entrega

1. Copiar `.env.example` → variables del panel y completar secretos (tabla de obligatorias arriba).
2. Definir `Privacidad__*` confirmados por el área jurídica.
3. `Email__Provider=Brevo`, `Email__ApiKey`, `Email__FromAddress`, `Email__FromName`.
4. `Seed__DemoUsers=false` y `ADMIN_SEED_PASSWORD` solo si aún no hay administrador.
5. Respaldo antes de importar inventario: [BACKUP.md](BACKUP.md). Advertencia de sobrescritura de stock arriba.
6. `dotnet test` en CI / local con PostgreSQL disponible.
