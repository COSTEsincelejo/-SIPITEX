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

## Credenciales a rotar (no dejar las de demo en producción)

| Qué | Dónde | Acción |
|-----|--------|--------|
| Contraseñas demo (`Admin123!`, `Instructor123!`, `Bodega123!`) | Solo Development / seed | No usar en producción. Con `Seed__DemoUsers=false` no se siembran. |
| `ADMIN_SEED_PASSWORD` | Panel del hosting | Rotar tras el primer arranque si se usó para crear `admin@sipitex.local`. |
| `Email__ApiKey` (Brevo / Resend) | Panel del hosting | Rotar si se filtró o cambió de proveedor. |
| Contraseña de PostgreSQL | `ConnectionStrings__DefaultConnection` / Neon / Render | Rotar al cambiar de responsable de operaciones. |
| Cookies / Data Protection | Base PostgreSQL | Tras restaurar un backup antiguo, validar que las sesiones siguen cerrándose bien. |

No hay claves reales en el repositorio. Los tests usan marcadores (`re_super_secret`, `xkeysib-secret`) que no son credenciales de producción.

## Deuda conocida

1. **Privacidad institucional incompleta.** NIT, correo, dirección, teléfono, proveedores, plazo de conservación y URL institucional siguen vacíos o por confirmar. Ver `PENDIENTES-PRIVACIDAD.md`. No inventar datos: vacío o `PENDIENTE` no se publican.
2. **Ciudad del responsable.** Hoy puede figurar «Bogotá D.C.» en configuración; la sede operativa se ha citado como Sincelejo. Confirmar con el centro qué ciudad debe verse.
3. **Menores de edad.** El registro no pide fecha de nacimiento ni autorización del representante; la política lo declara.
4. **RNBD y excepciones legales.** Pendiente de área jurídica (Ley 1581).
5. **Correo sin credenciales.** Con Brevo/Resend incompleto el sistema arranca y registra error en el log; Alertas muestra aviso. `Email:Host` ya no trae `smtp.gmail.com`.
6. **Instructor sin consulta de inventario consolidado.** `PuedeConsultarInventario` solo Admin/Encargado. El instructor ve materiales por MRP, solicitudes y materiales de órdenes.
7. **Eliminación de BOM.** Solo Administrador; Create/Edit siguen con `PuedeGestionarFichasTecnicas`.
8. **Importación Excel / plantillas.** Trabajo en curso o reciente en otra línea de la misma entrega; no mezclar sin revisar `GUIA-INVENTARIO-INICIAL.md` si existe.
9. **Seed demo.** No reactivar `SEED_DEMO_DATA` en el entorno real del centro.

## Arranque rápido post-entrega

1. Copiar `.env.example` → `.env` (o variables del panel) y completar secretos.
2. Definir `Privacidad__*` confirmados por el área jurídica.
3. `Email__Provider=Brevo`, `Email__ApiKey`, `Email__FromAddress`, `Email__FromName`.
4. `Seed__DemoUsers=false` y `ADMIN_SEED_PASSWORD` solo si aún no hay administrador.
5. Respaldo antes de importar inventario: [BACKUP.md](BACKUP.md).
6. `dotnet test` en CI / local con PostgreSQL disponible.
