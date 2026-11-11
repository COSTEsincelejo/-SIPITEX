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

1. Copiar `.env.example` → `.env` (o variables del panel) y completar secretos.
2. Definir `Privacidad__*` confirmados por el área jurídica.
3. `Email__Provider=Brevo`, `Email__ApiKey`, `Email__FromAddress`, `Email__FromName`.
4. `Seed__DemoUsers=false` y `ADMIN_SEED_PASSWORD` solo si aún no hay administrador.
5. Respaldo antes de importar inventario: [BACKUP.md](BACKUP.md).
6. `dotnet test` en CI / local con PostgreSQL disponible.
