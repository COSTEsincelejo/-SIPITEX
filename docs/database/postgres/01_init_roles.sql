-- SIPITEX — inicialización de roles/esquema base en PostgreSQL
-- Se ejecuta al crear el volumen por primera vez (docker-entrypoint-initdb.d).
-- El esquema de tablas lo aplica EF Core (MigrateAsync) al arrancar la app.
-- Este script solo deja comentarios / extensiones útiles.

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

COMMENT ON DATABASE sipitex IS 'SIPITEX — Sistema Integrado de Producción e Inventario Textil';
