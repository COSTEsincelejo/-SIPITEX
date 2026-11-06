# Pendientes de la política de datos personales

**Esto no es asesoría legal.** El texto publicado en `/Privacidad` (versión 1.1) debe revisarlo el área jurídica o el oficial de datos del Centro de Manufactura en Textil y Cuero antes de tratarlo como definitivo. Lo que sigue son decisiones del centro, no afirmaciones del sistema.

La página no muestra un dato vacío ni la palabra de configuración interna. Cuando el centro confirme un valor, se escribe en la sección `Privacidad` (o en la variable `Privacidad__Clave` del entorno) y vuelve a aparecer en la política. Si queda vacío, el arranque deja una advertencia en el log.

## [COMPLETAR] Responsable y NIT

- [COMPLETAR: confirmar que el responsable que se publica es «SENA, Centro de Manufactura en Textil y Cuero (CMTC)». Hoy ese texto está en `Privacidad:ResponsableNombre`.]
- [COMPLETAR: NIT. El encargo citó 899.999.034-1 como candidato. No se publicó. Llenar `Privacidad:NIT` solo después de que el área jurídica lo confirme.]
- [COMPLETAR: ciudad. El texto pedido dice Bogotá D.C. y por eso `Privacidad:Ciudad` quedó en «Bogotá D.C.». Antes la configuración decía Sincelejo, que es la sede del centro. Confirmar cuál ciudad debe verse.]

## [COMPLETAR] Contacto

- [COMPLETAR: correo del responsable. `Privacidad:CorreoContacto` está vacío. Mientras lo esté, la política no muestra correo y las solicitudes de Mis datos no envían aviso, aunque sí quedan en Auditoría.]
- [COMPLETAR: dirección de notificación. `Privacidad:Direccion` está vacía.]
- [COMPLETAR: teléfono. `Privacidad:Telefono` está vacío.]
- [COMPLETAR: horario de atención. `Privacidad:HorarioAtencion` está vacío.]
- [COMPLETAR: área o cargo que atiende las solicitudes de datos. `Privacidad:AreaSolicitudes` está vacío.]

## [COMPLETAR] Proveedores y países

- [COMPLETAR: nombre y país del proveedor de correo. `Privacidad:ProveedorCorreo` y `Privacidad:PaisProveedorCorreo` están vacíos. El ejemplo de producción del repositorio usa el canal configurado en `Email:Provider` (el valor de ejemplo es Brevo). No se escribió ese nombre en la política.]
- [COMPLETAR: nombre y país de quien aloja la aplicación y la base de datos. `Privacidad:ProveedorHosting` y `Privacidad:PaisProveedorHosting` están vacíos. La guía de despliegue menciona Render. No se escribió ese nombre en la política.]
- Si el país confirmado no es Colombia, la política dirá que hay transmisión o transferencia internacional hacia ese país, bajo las garantías de la ley. Si el país queda vacío, esa frase no aparece.
- [COMPLETAR: confirmar que existe el contrato o acuerdo de transmisión con esos encargados. La política dice que actúan como encargados bajo ese acuerdo.]
- El canal de archivo interno de correo guarda el texto del mensaje, incluido un código de verificación. No debe usarse en producción.

## [COMPLETAR] Plazo de conservación

- [COMPLETAR: plazo, por ejemplo «X años desde la terminación de la formación o vinculación». `Privacidad:PlazoConservacion` está vacío. La política dice que los datos siguen el tiempo que exijan formación, inventario y auditoría, y que luego se suprimen o se anonimizan. No inventa un número de años.]

## [COMPLETAR] Menores de edad

- El usuario no tiene fecha de nacimiento. No se agregó una verificación de mayoría de edad ni un campo de autorización del representante legal.
- [COMPLETAR: decidir si el registro o el primer ingreso pide la fecha de nacimiento o la autorización del representante. La política ya explica que, si el titular es menor de 18 años, prevalece su interés superior, solo se tratan los datos necesarios para la formación y hace falta esa autorización, gestionada por fuera del sistema mientras el campo no exista.]

## [COMPLETAR] Registro Nacional de Bases de Datos

- [COMPLETAR: verificar con el centro y el área jurídica si la base de SIPITEX debe inscribirse en el RNBD.]
- La política no afirma que esté inscrita ni publica un número de registro.

## [COMPLETAR] Excepciones a la autorización

- [COMPLETAR con el área jurídica: qué casos de la Ley 1581 permiten tratar datos sin autorización y si de verdad aplican al centro, por ejemplo funciones legales de una entidad pública.]
- La política no enumera esos casos. Dice que el centro solo aplicará uno cuando el área jurídica confirme que corresponde a este sistema.

## [COMPLETAR] Política institucional del SENA

- [COMPLETAR: enlace a la política institucional de datos del SENA, si existe. `Privacidad:UrlPoliticaInstitucional` está vacío y la página no muestra un enlace. Tiene que ser una dirección http o https.]
