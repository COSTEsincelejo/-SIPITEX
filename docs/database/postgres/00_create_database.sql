-- SIPITEX — crear rol y base de datos en PostgreSQL 16+
-- Ejecutar como superusuario (postgres):
--   psql -U postgres -f 00_create_database.sql

DO $$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'sipitex') THEN
    CREATE ROLE sipitex LOGIN PASSWORD 'sipitex';
  END IF;
END$$;

SELECT 'CREATE DATABASE sipitex OWNER sipitex'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'sipitex')\gexec

GRANT ALL PRIVILEGES ON DATABASE sipitex TO sipitex;

\c sipitex
GRANT ALL ON SCHEMA public TO sipitex;
ALTER SCHEMA public OWNER TO sipitex;
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
COMMENT ON DATABASE sipitex IS 'SIPITEX — Sistema Integrado de Producción e Inventario Textil';
