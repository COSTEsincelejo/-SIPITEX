# Respaldo de la base SIPITEX (Neon / PostgreSQL)

La aplicación guarda usuarios, fichas, materiales, órdenes, fotos de perfil, correos sin SMTP y las claves de las cookies en PostgreSQL. El disco del servicio en Render no conserva datos entre deploys. Este procedimiento exporta e importa esa base. No pegue contraseñas, URLs con clave ni volcados dentro del repositorio.

## Variables

Use la cadena que entrega el panel de Neon (o `DATABASE_URL` en Render). En los ejemplos, `$DATABASE_URL` es esa URL y no se escribe aquí.

Neon exige TLS. `pg_dump` y `psql` recientes aceptan la URL `postgresql://…?sslmode=require`.

## Exportar

Desde una máquina con el cliente de PostgreSQL 16:

```bash
pg_dump --no-owner --no-acl --format=custom --file=sipitex.dump "$DATABASE_URL"
```

Para un script SQL legible:

```bash
pg_dump --no-owner --no-acl --format=plain --file=sipitex.sql "$DATABASE_URL"
```

Guarde el archivo fuera del repo (otro disco, almacenamiento cifrado). Un volcado incluye hashes de contraseña y datos personales.

## Importar

En una base vacía (otra rama de Neon, o una base local):

```bash
pg_restore --no-owner --no-acl --dbname "$DATABASE_URL_DESTINO" sipitex.dump
```

Si el formato es SQL plano:

```bash
psql "$DATABASE_URL_DESTINO" -f sipitex.sql
```

`pg_restore` puede avisar de objetos que ya existen si la base de destino ya tiene el esquema de EF Core. Para una restauración limpia, use una base nueva o una rama nueva de Neon, no la base que está sirviendo tráfico.

Después de importar, las secuencias de identidad deben quedar en el máximo `Id`. Si restauró con `pg_dump`/`pg_restore`, las secuencias viajan en el volcado. Si copió filas con `tools/Sipitex.DataMigration`, esa herramienta las reinicia ella misma.

## Ramas de Neon

Una rama es una copia de la base en un punto dado, con su propia cadena de conexión. Sirve para ensayar una importación o un cambio de esquema sin tocar la base de producción.

1. En el panel de Neon, abra el proyecto y elija **Branches**.
2. Cree una rama a partir de `main` (o de la rama que esté en uso).
3. Copie la cadena de conexión de esa rama a una variable local, por ejemplo `DATABASE_URL_DESTINO`. No la suba a git.
4. Apunte el comando de importación, o una instancia local de SIPITEX, a esa cadena.
5. Cuando termine la prueba, borre la rama en el panel si ya no la necesita.

La rama de producción no se modifica al crear otra rama.

## Restauración a un punto en el tiempo

Neon guarda el historial de la rama (point-in-time restore) dentro de la ventana que muestra el panel, según el plan.

1. En **Branches**, elija **Restore** (restaurar) sobre la rama que quiere recuperar.
2. Indique la fecha y hora (UTC) anteriores al incidente.
3. Neon crea una rama nueva con los datos de ese momento. La rama actual sigue igual hasta que usted decida usarla.
4. Compruebe la rama nueva con `psql` (conteos de `"Users"`, `"Fichas"`, `"Materials"`) y, si está bien, cambie `DATABASE_URL` del servicio en Render para que apunte a la cadena de esa rama.
5. Redeploye el servicio para que tome la variable nueva.

No hace falta tocar el disco del contenedor: las fotos, los correos de outbox y las claves de cookie están en las tablas `UserProfilePhotos`, `EmailOutboxMessages` y `DataProtectionKeys`.

## Qué no entra en este respaldo

- El código y las migraciones EF Core viven en git.
- `build-timestamp.txt` es un artefacto de publicación, no un dato de negocio.
- Los intentos fallidos de login se guardan solo en memoria del proceso y se reinician con cada deploy a propósito.
