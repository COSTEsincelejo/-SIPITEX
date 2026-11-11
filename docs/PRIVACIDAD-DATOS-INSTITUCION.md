# Datos institucionales de privacidad

Los datos del responsable y de los proveedores **no se inventan en código**. Se leen de la sección `Privacidad` de `appsettings` o de variables `Privacidad__*`. Si el valor está vacío o es el literal `PENDIENTE`, la política, el PDF del acta y el log de arranque **no lo publican**.

Pendientes de confirmación jurídica: [`PENDIENTES-PRIVACIDAD.md`](../PENDIENTES-PRIVACIDAD.md).

| Dato | Variable de entorno / clave appsettings | Dónde se muestra |
|------|-----------------------------------------|------------------|
| Versión de la política | `Privacidad__Version` / `Privacidad:Version` | `/Privacidad`, descarga, aceptación |
| Fecha de actualización | `Privacidad__FechaActualizacion` / `Privacidad:FechaActualizacion` | `/Privacidad`, descarga |
| Nombre del responsable | `Privacidad__ResponsableNombre` / `Privacidad:ResponsableNombre` | `/Privacidad`, pie del PDF de acta |
| NIT | `Privacidad__NIT` / `Privacidad:NIT` | `/Privacidad`, pie del PDF de acta |
| Ciudad | `Privacidad__Ciudad` / `Privacidad:Ciudad` | `/Privacidad`, pie del PDF de acta |
| Correo de contacto | `Privacidad__CorreoContacto` / `Privacidad:CorreoContacto` | `/Privacidad`; avisos de «Mis datos» |
| Dirección de notificación | `Privacidad__Direccion` / `Privacidad:Direccion` | `/Privacidad` |
| Teléfono | `Privacidad__Telefono` / `Privacidad:Telefono` | `/Privacidad` |
| Horario de atención | `Privacidad__HorarioAtencion` / `Privacidad:HorarioAtencion` | `/Privacidad` |
| Área o cargo de solicitudes | `Privacidad__AreaSolicitudes` / `Privacidad:AreaSolicitudes` | `/Privacidad` |
| Plazo de conservación | `Privacidad__PlazoConservacion` / `Privacidad:PlazoConservacion` | `/Privacidad` |
| Proveedor de correo | `Privacidad__ProveedorCorreo` / `Privacidad:ProveedorCorreo` | `/Privacidad` |
| País del proveedor de correo | `Privacidad__PaisProveedorCorreo` / `Privacidad:PaisProveedorCorreo` | `/Privacidad` (y frase de transmisión internacional si no es Colombia) |
| Proveedor de hosting / BD | `Privacidad__ProveedorHosting` / `Privacidad:ProveedorHosting` | `/Privacidad` |
| País del hosting | `Privacidad__PaisProveedorHosting` / `Privacidad:PaisProveedorHosting` | `/Privacidad` (y frase de transmisión internacional si no es Colombia) |
| URL política institucional SENA | `Privacidad__UrlPoliticaInstitucional` / `Privacidad:UrlPoliticaInstitucional` | `/Privacidad` (solo si es http/https) |

El pie de página del sitio (`_Layout` / `_LoginLayout`) muestra la marca SIPITEX · CMTC · SENA · ADSO y el enlace a la política; **no** imprime NIT ni datos de contacto.
