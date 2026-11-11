# Datos personales en SIPITEX

Inventario factual de datos personales / identificables en el modelo y dónde se muestran.  
Política publicada: `/Privacidad` (versión configurada). Datos del responsable institucional: [`docs/PRIVACIDAD-DATOS-INSTITUCION.md`](../PRIVACIDAD-DATOS-INSTITUCION.md). Pendientes jurídicos: [`PENDIENTES-PRIVACIDAD.md`](../../PENDIENTES-PRIVACIDAD.md).

> Este documento describe el sistema; **no es asesoría legal**.

## 1. Entidades y campos

### 1.1 `User` (`Users`)

| Campo | Contenido |
|-------|-----------|
| `Nombre` | Nombre completo (UI y claims) |
| `Email` | Correo único; identificador de login |
| `PasswordHash` | Hash de contraseña (nunca texto plano) |
| `Rol` | Administrador / Instructor / Encargado de bodega |
| `PhotoPath` + `UserProfilePhotos` | Foto de perfil (ruta y/o bytes) |
| `FuncionDescripcion` | Texto libre del perfil |
| `FichaAsignadaId`, plantas, permisos extendidos, `IsActive`, `EmailConfirmed` | Operativos / control de acceso |

**Finalidad:** autenticación, autorización, identificación en operaciones del taller, perfil y auditoría.

**Dónde se muestran:** login; `/Account` (perfil, usuarios admin); menú (nombre/foto); listados de instructores/bodegueros; actas (como creador/anulador); «Mis datos».

### 1.2 `AceptacionTratamiento`

| Campo | Contenido |
|-------|-----------|
| `UserId` | Usuario que aceptó |
| `VersionPolitica` | Versión (`Privacidad:Version`) |
| `AceptadaEnUtc` | Fecha/hora UTC |
| `DireccionIp` | IP vista por la app (puede ser del proxy) |

**Finalidad:** constancia de aceptación de la política vigente.  
**Dónde:** flujo `/Privacidad/Aceptar`; historial en «Mis datos»; no se borra al aceptar una versión nueva.

### 1.3 `ActivityLog`

| Campo | Contenido |
|-------|-----------|
| `UserId` | Quién ejecutó la acción |
| `UserName` | Snapshot del nombre |
| `Action`, `Entity`, `EntityId`, `Timestamp`, `Details` | Evento de auditoría |

**Finalidad:** trazabilidad administrativa (no sustituye `OrderChangeLog` / `StockMovement`).  
**Dónde:** módulo Auditoría (Administrador).

### 1.4 `PasswordResetToken`

| Campo | Contenido |
|-------|-----------|
| `UserId` | Dueño del código |
| `TokenHash` | SHA-256 del código de 6 dígitos (nunca el claro en BD) |
| `Purpose` | `PasswordReset` o `EmailConfirmation` |
| `FailedAttempts`, `ExpiresAtUtc`, `UsedAtUtc`, `CreatedAtUtc` | Control de uso |

**Finalidad:** recuperación de contraseña y confirmación de correo.  
**Dónde:** no se lista en UI; se consume en flujos de cuenta / correo.

### 1.5 `EmailOutboxMessage`

| Campo | Contenido |
|-------|-----------|
| `ToEmail`, `ToName` | Destinatario |
| `Subject`, `Body` | Contenido del mensaje |
| `AttachmentNames`, `CreatedAtUtc` | Metadatos |

**Finalidad:** retención de correos no enviados / outbox.  
**Dónde:** infraestructura de correo; puede contener datos personales del destinatario y del cuerpo.

### 1.6 Firmas en actas (`ActaMovimiento`)

| Campo | Contenido |
|-------|-----------|
| `EntregaNombre`, `EntregaCargo`, `EntregaConformidadUtc`, `EntregaFirmaPng` | Quien entrega |
| `RecibeNombre`, `RecibeCargo`, `RecibeConformidadUtc`, `RecibeFirmaPng` | Quien recibe |
| `CreadoPorUserId`, `AnuladaPorUserId` | Usuarios del sistema |

**Finalidad:** conformidad documental de movimientos de material/producto.  
**Dónde:** pantallas de actas, PDF del acta, verificación pública por QR (según datos expuestos en la vista pública).

## 2. Pantallas y flujos relevantes

| Ubicación | Datos |
|-----------|-------|
| `/Privacidad` | Política; datos institucionales configurados |
| `/Privacidad/Aceptar` | Aceptación obligatoria si falta la versión vigente |
| Mi perfil / Mis datos | Cuenta, historial de aceptaciones, solicitudes de rectificación/supresión/revocatoria (auditoría estado `recibida`) |
| `/Account/Users` | Admin: nombre, email, rol, estado |
| `/Account/Photo/{id}` | Foto de perfil |
| Actas / PDF | Nombres, cargos, firmas PNG |

## 3. Configuración relacionada

Variables `Privacidad__*` — ver [`PARAMETRIZACION.md`](PARAMETRIZACION.md) y [`PRIVACIDAD-DATOS-INSTITUCION.md`](../PRIVACIDAD-DATOS-INSTITUCION.md).

## 4. Conservación y terceros

Plazo de conservación, proveedores de correo/hosting y transmisión internacional: **configurables** y a menudo **PENDIENTES** de confirmación institucional (no inventar en código ni en pantallas).
