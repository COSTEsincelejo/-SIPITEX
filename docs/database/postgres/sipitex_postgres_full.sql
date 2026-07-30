--
-- PostgreSQL database dump
--

\restrict ZPmxamLFqU5c0JX8kKGm7mHRuAXHRuhksmYaBi1sGyujO5IFb97tN42O0W6nOf8

-- Dumped from database version 16.14 (Ubuntu 16.14-0ubuntu0.24.04.1)
-- Dumped by pg_dump version 16.14 (Ubuntu 16.14-0ubuntu0.24.04.1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

ALTER TABLE IF EXISTS ONLY public."Users" DROP CONSTRAINT IF EXISTS "FK_Users_Fichas_FichaAsignadaId";
ALTER TABLE IF EXISTS ONLY public."QualityRecords" DROP CONSTRAINT IF EXISTS "FK_QualityRecords_ProductionOrders_ProductionOrderId";
ALTER TABLE IF EXISTS ONLY public."ProductionSessions" DROP CONSTRAINT IF EXISTS "FK_ProductionSessions_Users_RegisteredByUserId";
ALTER TABLE IF EXISTS ONLY public."ProductionSessions" DROP CONSTRAINT IF EXISTS "FK_ProductionSessions_ProductionOrders_ProductionOrderId";
ALTER TABLE IF EXISTS ONLY public."ProductionSessions" DROP CONSTRAINT IF EXISTS "FK_ProductionSessions_Fichas_FichaId";
ALTER TABLE IF EXISTS ONLY public."PasswordResetTokens" DROP CONSTRAINT IF EXISTS "FK_PasswordResetTokens_Users_UserId";
ALTER TABLE IF EXISTS ONLY public."MaterialRequests" DROP CONSTRAINT IF EXISTS "FK_MaterialRequests_ProductionOrders_ProductionOrderId";
ALTER TABLE IF EXISTS ONLY public."MaterialRequests" DROP CONSTRAINT IF EXISTS "FK_MaterialRequests_Materials_MaterialId";
ALTER TABLE IF EXISTS ONLY public."Fichas" DROP CONSTRAINT IF EXISTS "FK_Fichas_Users_InstructorUserId";
ALTER TABLE IF EXISTS ONLY public."Fichas" DROP CONSTRAINT IF EXISTS "FK_Fichas_ProductionOrders_ProductionOrderId";
ALTER TABLE IF EXISTS ONLY public."BomItems" DROP CONSTRAINT IF EXISTS "FK_BomItems_Materials_MaterialId";
ALTER TABLE IF EXISTS ONLY public."AlertPreferences" DROP CONSTRAINT IF EXISTS "FK_AlertPreferences_Users_UserId";
ALTER TABLE IF EXISTS ONLY public."AlertDeliveries" DROP CONSTRAINT IF EXISTS "FK_AlertDeliveries_Users_UserId";
DROP INDEX IF EXISTS public."IX_Users_FichaAsignadaId";
DROP INDEX IF EXISTS public."IX_Users_Email";
DROP INDEX IF EXISTS public."IX_QualityRecords_ProductionOrderId";
DROP INDEX IF EXISTS public."IX_ProductionSessions_RegisteredByUserId";
DROP INDEX IF EXISTS public."IX_ProductionSessions_ProductionOrderId";
DROP INDEX IF EXISTS public."IX_ProductionSessions_FichaId";
DROP INDEX IF EXISTS public."IX_ProductionOrders_OrderNumber";
DROP INDEX IF EXISTS public."IX_PasswordResetTokens_UserId_CreatedAtUtc";
DROP INDEX IF EXISTS public."IX_PasswordResetTokens_TokenHash";
DROP INDEX IF EXISTS public."IX_NonFunctionalRequirements_Code";
DROP INDEX IF EXISTS public."IX_MaterialRequests_ProductionOrderId";
DROP INDEX IF EXISTS public."IX_MaterialRequests_MaterialId";
DROP INDEX IF EXISTS public."IX_FunctionalRequirements_Code";
DROP INDEX IF EXISTS public."IX_Fichas_ProductionOrderId";
DROP INDEX IF EXISTS public."IX_Fichas_InstructorUserId";
DROP INDEX IF EXISTS public."IX_BomItems_MaterialId";
DROP INDEX IF EXISTS public."IX_AlertPreferences_UserId_AlertType";
DROP INDEX IF EXISTS public."IX_AlertDeliveries_UserId";
ALTER TABLE IF EXISTS ONLY public."__EFMigrationsHistory" DROP CONSTRAINT IF EXISTS "PK___EFMigrationsHistory";
ALTER TABLE IF EXISTS ONLY public."Users" DROP CONSTRAINT IF EXISTS "PK_Users";
ALTER TABLE IF EXISTS ONLY public."QualityRecords" DROP CONSTRAINT IF EXISTS "PK_QualityRecords";
ALTER TABLE IF EXISTS ONLY public."ProductionSessions" DROP CONSTRAINT IF EXISTS "PK_ProductionSessions";
ALTER TABLE IF EXISTS ONLY public."ProductionOrders" DROP CONSTRAINT IF EXISTS "PK_ProductionOrders";
ALTER TABLE IF EXISTS ONLY public."PasswordResetTokens" DROP CONSTRAINT IF EXISTS "PK_PasswordResetTokens";
ALTER TABLE IF EXISTS ONLY public."NonFunctionalRequirements" DROP CONSTRAINT IF EXISTS "PK_NonFunctionalRequirements";
ALTER TABLE IF EXISTS ONLY public."Materials" DROP CONSTRAINT IF EXISTS "PK_Materials";
ALTER TABLE IF EXISTS ONLY public."MaterialRequests" DROP CONSTRAINT IF EXISTS "PK_MaterialRequests";
ALTER TABLE IF EXISTS ONLY public."FunctionalRequirements" DROP CONSTRAINT IF EXISTS "PK_FunctionalRequirements";
ALTER TABLE IF EXISTS ONLY public."Fichas" DROP CONSTRAINT IF EXISTS "PK_Fichas";
ALTER TABLE IF EXISTS ONLY public."BomItems" DROP CONSTRAINT IF EXISTS "PK_BomItems";
ALTER TABLE IF EXISTS ONLY public."AlertPreferences" DROP CONSTRAINT IF EXISTS "PK_AlertPreferences";
ALTER TABLE IF EXISTS ONLY public."AlertDeliveries" DROP CONSTRAINT IF EXISTS "PK_AlertDeliveries";
DROP TABLE IF EXISTS public."__EFMigrationsHistory";
DROP TABLE IF EXISTS public."Users";
DROP TABLE IF EXISTS public."QualityRecords";
DROP TABLE IF EXISTS public."ProductionSessions";
DROP TABLE IF EXISTS public."ProductionOrders";
DROP TABLE IF EXISTS public."PasswordResetTokens";
DROP TABLE IF EXISTS public."NonFunctionalRequirements";
DROP TABLE IF EXISTS public."Materials";
DROP TABLE IF EXISTS public."MaterialRequests";
DROP TABLE IF EXISTS public."FunctionalRequirements";
DROP TABLE IF EXISTS public."Fichas";
DROP TABLE IF EXISTS public."BomItems";
DROP TABLE IF EXISTS public."AlertPreferences";
DROP TABLE IF EXISTS public."AlertDeliveries";
-- *not* dropping schema, since initdb creates it
--
-- Name: public; Type: SCHEMA; Schema: -; Owner: -
--

-- *not* creating schema, since initdb creates it


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: AlertDeliveries; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AlertDeliveries" (
    "Id" integer NOT NULL,
    "UserId" integer NOT NULL,
    "AlertType" integer NOT NULL,
    "Subject" character varying(200) NOT NULL,
    "Body" character varying(4000) NOT NULL,
    "SentAt" timestamp with time zone NOT NULL,
    "Channel" character varying(40) NOT NULL
);


--
-- Name: AlertDeliveries_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."AlertDeliveries" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."AlertDeliveries_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: AlertPreferences; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AlertPreferences" (
    "Id" integer NOT NULL,
    "UserId" integer NOT NULL,
    "AlertType" integer NOT NULL,
    "Enabled" boolean NOT NULL
);


--
-- Name: AlertPreferences_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."AlertPreferences" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."AlertPreferences_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: BomItems; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."BomItems" (
    "Id" integer NOT NULL,
    "ProductName" character varying(80) NOT NULL,
    "MaterialId" integer NOT NULL,
    "QuantityPerUnit" numeric(18,2) NOT NULL,
    "Unit" integer NOT NULL
);


--
-- Name: BomItems_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."BomItems" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."BomItems_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: Fichas; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Fichas" (
    "Id" integer NOT NULL,
    "FichaCode" character varying(30) NOT NULL,
    "ProcessName" text NOT NULL,
    "InstructorName" text NOT NULL,
    "Turno" character varying(20) NOT NULL,
    "InstructorUserId" integer,
    "ProductionOrderId" integer
);


--
-- Name: Fichas_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."Fichas" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Fichas_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: FunctionalRequirements; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."FunctionalRequirements" (
    "Id" integer NOT NULL,
    "Code" character varying(10) NOT NULL,
    "Description" text NOT NULL,
    "Module" text NOT NULL,
    "Status" integer NOT NULL,
    "Observation" text NOT NULL
);


--
-- Name: FunctionalRequirements_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."FunctionalRequirements" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."FunctionalRequirements_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: MaterialRequests; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."MaterialRequests" (
    "Id" integer NOT NULL,
    "MaterialId" integer NOT NULL,
    "Quantity" numeric(18,2) NOT NULL,
    "ProductionOrderId" integer NOT NULL,
    "Status" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL
);


--
-- Name: MaterialRequests_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."MaterialRequests" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."MaterialRequests_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: Materials; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Materials" (
    "Id" integer NOT NULL,
    "Code" character varying(40) NOT NULL,
    "Name" character varying(120) NOT NULL,
    "Unit" integer NOT NULL,
    "Stock" numeric(18,2) NOT NULL,
    "MinStock" numeric(18,2) NOT NULL,
    "Status" integer NOT NULL,
    "LastEntryDate" date NOT NULL
);


--
-- Name: Materials_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."Materials" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Materials_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: NonFunctionalRequirements; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NonFunctionalRequirements" (
    "Id" integer NOT NULL,
    "Code" character varying(10) NOT NULL,
    "Description" text NOT NULL,
    "Status" integer NOT NULL,
    "Observation" text NOT NULL
);


--
-- Name: NonFunctionalRequirements_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."NonFunctionalRequirements" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."NonFunctionalRequirements_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: PasswordResetTokens; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."PasswordResetTokens" (
    "Id" integer NOT NULL,
    "UserId" integer NOT NULL,
    "TokenHash" character varying(128) NOT NULL,
    "ExpiresAtUtc" timestamp with time zone NOT NULL,
    "UsedAtUtc" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL
);


--
-- Name: PasswordResetTokens_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."PasswordResetTokens" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."PasswordResetTokens_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: ProductionOrders; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."ProductionOrders" (
    "Id" integer NOT NULL,
    "OrderNumber" character varying(20) NOT NULL,
    "ProductName" character varying(80) NOT NULL,
    "TotalQuantity" integer NOT NULL,
    "ProducedQuantity" integer NOT NULL,
    "Status" integer NOT NULL,
    "Deadline" date NOT NULL
);


--
-- Name: ProductionOrders_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."ProductionOrders" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."ProductionOrders_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: ProductionSessions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."ProductionSessions" (
    "Id" integer NOT NULL,
    "FichaId" integer NOT NULL,
    "ProductionOrderId" integer NOT NULL,
    "Units" integer NOT NULL,
    "Observations" character varying(500) NOT NULL,
    "SessionDate" timestamp with time zone NOT NULL,
    "RegisteredByUserId" integer
);


--
-- Name: ProductionSessions_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."ProductionSessions" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."ProductionSessions_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: QualityRecords; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."QualityRecords" (
    "Id" integer NOT NULL,
    "ProductionOrderId" integer NOT NULL,
    "UnitsInspected" integer NOT NULL,
    "Result" integer NOT NULL,
    "MotivoReproceso" character varying(300),
    "Responsable" character varying(120),
    "InspectionDate" date NOT NULL
);


--
-- Name: QualityRecords_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."QualityRecords" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."QualityRecords_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: Users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Users" (
    "Id" integer NOT NULL,
    "Nombre" character varying(120) NOT NULL,
    "Email" character varying(160) NOT NULL,
    "PasswordHash" character varying(256) NOT NULL,
    "Rol" character varying(40) NOT NULL,
    "FichaAsignadaId" integer,
    "PermisosExtendidos" character varying(500) NOT NULL,
    "PhotoPath" character varying(260),
    "FuncionDescripcion" character varying(800),
    "IsActive" boolean NOT NULL
);


--
-- Name: Users_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."Users" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Users_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Data for Name: AlertDeliveries; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."AlertDeliveries" ("Id", "UserId", "AlertType", "Subject", "Body", "SentAt", "Channel") FROM stdin;
\.


--
-- Data for Name: AlertPreferences; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."AlertPreferences" ("Id", "UserId", "AlertType", "Enabled") FROM stdin;
1	1	1	t
2	1	2	t
3	1	3	t
4	1	4	t
5	1	5	t
6	3	1	t
7	3	2	t
8	3	3	f
9	3	4	f
10	3	5	f
11	2	1	f
12	2	2	f
13	2	3	t
14	2	4	t
15	2	5	t
\.


--
-- Data for Name: BomItems; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."BomItems" ("Id", "ProductName", "MaterialId", "QuantityPerUnit", "Unit") FROM stdin;
1	Camisa	1	1.60	0
2	Camisa	2	18.00	0
3	Camisa	3	1.00	1
4	Pantalón	1	2.20	0
5	Pantalón	2	22.00	0
6	Pantalón	4	0.80	0
\.


--
-- Data for Name: Fichas; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Fichas" ("Id", "FichaCode", "ProcessName", "InstructorName", "Turno", "InstructorUserId", "ProductionOrderId") FROM stdin;
2	FICHA-C2	Corte	Carlos Méndez	Mañana	\N	1
3	FICHA-E3	Confección	Ana Rojas	Tarde	\N	2
1	FICHA-T1	Trazo	Laura Gómez	Mañana	2	1
\.


--
-- Data for Name: FunctionalRequirements; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") FROM stdin;
1	RF01	Crear, editar y desactivar usuarios por rol.	Usuarios	0	CRUD de usuarios con roles.
2	RF02	Autenticación con credenciales propias por rol.	Usuarios	0	Login con cookies de autenticación.
3	RF03	Registrar entradas con fecha, cantidad y unidad.	Inventario	0	Fecha de última entrada en material.
4	RF04	Consultar stock disponible en tiempo real.	Inventario	0	Actualización reactiva.
5	RF05	Bodeguero registra estado del material (Bueno/Regular/Deteriorado).	Inventario	0	Selector de estado en inventario.
6	RF06	Instructor solicita materiales (orden, producto, cantidad).	Salida	0	Formulario de solicitud.
7	RF07	Bodeguero aprueba / rechaza y registra entrega.	Salida	0	Aprobación y rechazo implementados.
8	RF08	Salida trazada por orden y producto.	Salida	1	Se guarda orderId pero no historial visible.
9	RF09	Admin crea órdenes con producto, cantidad y fecha.	Órdenes	0	Formulario completo.
10	RF10	Estados: Pendiente, En Proceso, Finalizada, Cancelada.	Órdenes	1	Solo 'En Proceso' y 'Finalizada'.
11	RF11	Cálculo MRP automático por prenda y talla.	Órdenes	0	BOM hardcoded, falta diferenciación por talla.
12	RF12	Fichas asociadas a un proceso de la línea.	Fichas	0	Cada ficha tiene proceso.
13	RF13	Un instructor puede tener múltiples fichas.	Fichas	1	Estructuralmente posible, sin UI asignación.
14	RF14	Instructor registra sesión diaria: ficha, unidades, observaciones.	Producción	0	Sesiones con observaciones persistidas.
15	RF15	Mostrar avance acumulado vs meta de la orden.	Producción	0	Tabla de órdenes muestra avance.
16	RF16	Criterios diferenciales por tipo de prenda.	Calidad	2	Solo aprobado/reproceso genérico.
17	RF17	Prendas en reproceso con trazabilidad de motivo y responsable.	Calidad	0	Motivo y responsable obligatorios en reproceso.
18	RF18	Reportes de producción por período, instructor y orden.	Estadísticas	1	Por orden, falta filtro por período/instructor.
19	RF19	Estadísticas de consumo de materiales por producto.	Estadísticas	0	Cálculo vía BOM+MRP.
20	RF20	Dashboard con KPIs: unidades, calidad, eficiencia.	Estadísticas	0	KPIs y gráfico.
\.


--
-- Data for Name: MaterialRequests; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."MaterialRequests" ("Id", "MaterialId", "Quantity", "ProductionOrderId", "Status", "CreatedAt") FROM stdin;
\.


--
-- Data for Name: Materials; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Materials" ("Id", "Code", "Name", "Unit", "Stock", "MinStock", "Status", "LastEntryDate") FROM stdin;
1	mat1	Tela Jersey	0	280.00	80.00	0	2026-07-30
2	mat2	Hilo Poliéster	0	3200.00	500.00	0	2026-07-30
3	mat3	Cremallera invisible	1	95.00	40.00	0	2026-07-30
4	mat4	Forro Satín	0	120.00	50.00	1	2026-07-30
\.


--
-- Data for Name: NonFunctionalRequirements; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NonFunctionalRequirements" ("Id", "Code", "Description", "Status", "Observation") FROM stdin;
1	RNF01	Carga rápida en intranet (<2s)	0	HTML estático liviano.
2	RNF02	Autenticación con sesión y expiración	0	Cookies con expiración de 8 horas.
3	RNF03	Control de acceso por rol	0	Authorize por rol en controladores.
4	RNF04	Disponible desde cualquier PC de la intranet	1	Requiere servidor HTTP.
5	RNF05	Interfaz responsiva e intuitiva	0	Layout adaptable.
6	RNF06	Código modular y documentado	1	Arquitectura por capas.
7	RNF07	Despliegue con Docker Compose	0	Dockerfile + docker-compose.yml.
8	RNF08	Integridad transaccional	1	EF Core con SQLite o PostgreSQL.
\.


--
-- Data for Name: PasswordResetTokens; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."PasswordResetTokens" ("Id", "UserId", "TokenHash", "ExpiresAtUtc", "UsedAtUtc", "CreatedAtUtc") FROM stdin;
\.


--
-- Data for Name: ProductionOrders; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."ProductionOrders" ("Id", "OrderNumber", "ProductName", "TotalQuantity", "ProducedQuantity", "Status", "Deadline") FROM stdin;
1	OP-001	Camisa	120	45	1	2025-04-15
2	OP-002	Pantalón	80	20	1	2025-04-20
\.


--
-- Data for Name: ProductionSessions; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."ProductionSessions" ("Id", "FichaId", "ProductionOrderId", "Units", "Observations", "SessionDate", "RegisteredByUserId") FROM stdin;
\.


--
-- Data for Name: QualityRecords; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."QualityRecords" ("Id", "ProductionOrderId", "UnitsInspected", "Result", "MotivoReproceso", "Responsable", "InspectionDate") FROM stdin;
\.


--
-- Data for Name: Users; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Users" ("Id", "Nombre", "Email", "PasswordHash", "Rol", "FichaAsignadaId", "PermisosExtendidos", "PhotoPath", "FuncionDescripcion", "IsActive") FROM stdin;
1	Administrador SIPITEX	admin@sipitex.test	pbkdf2$100000$eo6yUc3Eh3DbJBIIzKPjSw==$69NXAKNnuqUVmcg4hzN6h1PyUBjkOcAhGNYgRdpbhhE=	Administrador	\N		\N	\N	t
3	Pedro Bodega	bodega@sipitex.test	pbkdf2$100000$3MlzCrtekIn0V1vz7gfA0g==$rNE5vsvv0tlvR8HQsEp9N2MrIfyt6jqFa1+MF4SFpR8=	Bodeguero	\N		\N	\N	t
2	Laura Gómez	instructor@sipitex.test	pbkdf2$100000$K2TX+AHVZfAr9CCVXPoTnw==$0ay22JuNKvJYvwyzxGUptv2AVRbkGeLQvzDHFIxBl2g=	Instructor	1		\N	\N	t
\.


--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") FROM stdin;
20260730145651_InitialPostgreSQL	10.0.9
\.


--
-- Name: AlertDeliveries_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."AlertDeliveries_Id_seq"', 1, false);


--
-- Name: AlertPreferences_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."AlertPreferences_Id_seq"', 15, true);


--
-- Name: BomItems_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."BomItems_Id_seq"', 6, true);


--
-- Name: Fichas_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."Fichas_Id_seq"', 3, true);


--
-- Name: FunctionalRequirements_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."FunctionalRequirements_Id_seq"', 20, true);


--
-- Name: MaterialRequests_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."MaterialRequests_Id_seq"', 1, false);


--
-- Name: Materials_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."Materials_Id_seq"', 4, true);


--
-- Name: NonFunctionalRequirements_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."NonFunctionalRequirements_Id_seq"', 8, true);


--
-- Name: PasswordResetTokens_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."PasswordResetTokens_Id_seq"', 1, false);


--
-- Name: ProductionOrders_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."ProductionOrders_Id_seq"', 2, true);


--
-- Name: ProductionSessions_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."ProductionSessions_Id_seq"', 1, false);


--
-- Name: QualityRecords_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."QualityRecords_Id_seq"', 1, false);


--
-- Name: Users_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."Users_Id_seq"', 3, true);


--
-- Name: AlertDeliveries PK_AlertDeliveries; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AlertDeliveries"
    ADD CONSTRAINT "PK_AlertDeliveries" PRIMARY KEY ("Id");


--
-- Name: AlertPreferences PK_AlertPreferences; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AlertPreferences"
    ADD CONSTRAINT "PK_AlertPreferences" PRIMARY KEY ("Id");


--
-- Name: BomItems PK_BomItems; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."BomItems"
    ADD CONSTRAINT "PK_BomItems" PRIMARY KEY ("Id");


--
-- Name: Fichas PK_Fichas; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Fichas"
    ADD CONSTRAINT "PK_Fichas" PRIMARY KEY ("Id");


--
-- Name: FunctionalRequirements PK_FunctionalRequirements; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."FunctionalRequirements"
    ADD CONSTRAINT "PK_FunctionalRequirements" PRIMARY KEY ("Id");


--
-- Name: MaterialRequests PK_MaterialRequests; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."MaterialRequests"
    ADD CONSTRAINT "PK_MaterialRequests" PRIMARY KEY ("Id");


--
-- Name: Materials PK_Materials; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Materials"
    ADD CONSTRAINT "PK_Materials" PRIMARY KEY ("Id");


--
-- Name: NonFunctionalRequirements PK_NonFunctionalRequirements; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NonFunctionalRequirements"
    ADD CONSTRAINT "PK_NonFunctionalRequirements" PRIMARY KEY ("Id");


--
-- Name: PasswordResetTokens PK_PasswordResetTokens; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PasswordResetTokens"
    ADD CONSTRAINT "PK_PasswordResetTokens" PRIMARY KEY ("Id");


--
-- Name: ProductionOrders PK_ProductionOrders; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ProductionOrders"
    ADD CONSTRAINT "PK_ProductionOrders" PRIMARY KEY ("Id");


--
-- Name: ProductionSessions PK_ProductionSessions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ProductionSessions"
    ADD CONSTRAINT "PK_ProductionSessions" PRIMARY KEY ("Id");


--
-- Name: QualityRecords PK_QualityRecords; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."QualityRecords"
    ADD CONSTRAINT "PK_QualityRecords" PRIMARY KEY ("Id");


--
-- Name: Users PK_Users; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "PK_Users" PRIMARY KEY ("Id");


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: IX_AlertDeliveries_UserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AlertDeliveries_UserId" ON public."AlertDeliveries" USING btree ("UserId");


--
-- Name: IX_AlertPreferences_UserId_AlertType; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_AlertPreferences_UserId_AlertType" ON public."AlertPreferences" USING btree ("UserId", "AlertType");


--
-- Name: IX_BomItems_MaterialId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_BomItems_MaterialId" ON public."BomItems" USING btree ("MaterialId");


--
-- Name: IX_Fichas_InstructorUserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Fichas_InstructorUserId" ON public."Fichas" USING btree ("InstructorUserId");


--
-- Name: IX_Fichas_ProductionOrderId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Fichas_ProductionOrderId" ON public."Fichas" USING btree ("ProductionOrderId");


--
-- Name: IX_FunctionalRequirements_Code; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_FunctionalRequirements_Code" ON public."FunctionalRequirements" USING btree ("Code");


--
-- Name: IX_MaterialRequests_MaterialId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MaterialRequests_MaterialId" ON public."MaterialRequests" USING btree ("MaterialId");


--
-- Name: IX_MaterialRequests_ProductionOrderId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MaterialRequests_ProductionOrderId" ON public."MaterialRequests" USING btree ("ProductionOrderId");


--
-- Name: IX_NonFunctionalRequirements_Code; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_NonFunctionalRequirements_Code" ON public."NonFunctionalRequirements" USING btree ("Code");


--
-- Name: IX_PasswordResetTokens_TokenHash; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_PasswordResetTokens_TokenHash" ON public."PasswordResetTokens" USING btree ("TokenHash");


--
-- Name: IX_PasswordResetTokens_UserId_CreatedAtUtc; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_PasswordResetTokens_UserId_CreatedAtUtc" ON public."PasswordResetTokens" USING btree ("UserId", "CreatedAtUtc");


--
-- Name: IX_ProductionOrders_OrderNumber; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_ProductionOrders_OrderNumber" ON public."ProductionOrders" USING btree ("OrderNumber");


--
-- Name: IX_ProductionSessions_FichaId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ProductionSessions_FichaId" ON public."ProductionSessions" USING btree ("FichaId");


--
-- Name: IX_ProductionSessions_ProductionOrderId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ProductionSessions_ProductionOrderId" ON public."ProductionSessions" USING btree ("ProductionOrderId");


--
-- Name: IX_ProductionSessions_RegisteredByUserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ProductionSessions_RegisteredByUserId" ON public."ProductionSessions" USING btree ("RegisteredByUserId");


--
-- Name: IX_QualityRecords_ProductionOrderId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_QualityRecords_ProductionOrderId" ON public."QualityRecords" USING btree ("ProductionOrderId");


--
-- Name: IX_Users_Email; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Users_Email" ON public."Users" USING btree ("Email");


--
-- Name: IX_Users_FichaAsignadaId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Users_FichaAsignadaId" ON public."Users" USING btree ("FichaAsignadaId");


--
-- Name: AlertDeliveries FK_AlertDeliveries_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AlertDeliveries"
    ADD CONSTRAINT "FK_AlertDeliveries_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: AlertPreferences FK_AlertPreferences_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AlertPreferences"
    ADD CONSTRAINT "FK_AlertPreferences_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: BomItems FK_BomItems_Materials_MaterialId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."BomItems"
    ADD CONSTRAINT "FK_BomItems_Materials_MaterialId" FOREIGN KEY ("MaterialId") REFERENCES public."Materials"("Id") ON DELETE CASCADE;


--
-- Name: Fichas FK_Fichas_ProductionOrders_ProductionOrderId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Fichas"
    ADD CONSTRAINT "FK_Fichas_ProductionOrders_ProductionOrderId" FOREIGN KEY ("ProductionOrderId") REFERENCES public."ProductionOrders"("Id");


--
-- Name: Fichas FK_Fichas_Users_InstructorUserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Fichas"
    ADD CONSTRAINT "FK_Fichas_Users_InstructorUserId" FOREIGN KEY ("InstructorUserId") REFERENCES public."Users"("Id") ON DELETE SET NULL;


--
-- Name: MaterialRequests FK_MaterialRequests_Materials_MaterialId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."MaterialRequests"
    ADD CONSTRAINT "FK_MaterialRequests_Materials_MaterialId" FOREIGN KEY ("MaterialId") REFERENCES public."Materials"("Id") ON DELETE CASCADE;


--
-- Name: MaterialRequests FK_MaterialRequests_ProductionOrders_ProductionOrderId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."MaterialRequests"
    ADD CONSTRAINT "FK_MaterialRequests_ProductionOrders_ProductionOrderId" FOREIGN KEY ("ProductionOrderId") REFERENCES public."ProductionOrders"("Id") ON DELETE CASCADE;


--
-- Name: PasswordResetTokens FK_PasswordResetTokens_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PasswordResetTokens"
    ADD CONSTRAINT "FK_PasswordResetTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: ProductionSessions FK_ProductionSessions_Fichas_FichaId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ProductionSessions"
    ADD CONSTRAINT "FK_ProductionSessions_Fichas_FichaId" FOREIGN KEY ("FichaId") REFERENCES public."Fichas"("Id") ON DELETE CASCADE;


--
-- Name: ProductionSessions FK_ProductionSessions_ProductionOrders_ProductionOrderId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ProductionSessions"
    ADD CONSTRAINT "FK_ProductionSessions_ProductionOrders_ProductionOrderId" FOREIGN KEY ("ProductionOrderId") REFERENCES public."ProductionOrders"("Id") ON DELETE CASCADE;


--
-- Name: ProductionSessions FK_ProductionSessions_Users_RegisteredByUserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ProductionSessions"
    ADD CONSTRAINT "FK_ProductionSessions_Users_RegisteredByUserId" FOREIGN KEY ("RegisteredByUserId") REFERENCES public."Users"("Id") ON DELETE SET NULL;


--
-- Name: QualityRecords FK_QualityRecords_ProductionOrders_ProductionOrderId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."QualityRecords"
    ADD CONSTRAINT "FK_QualityRecords_ProductionOrders_ProductionOrderId" FOREIGN KEY ("ProductionOrderId") REFERENCES public."ProductionOrders"("Id") ON DELETE CASCADE;


--
-- Name: Users FK_Users_Fichas_FichaAsignadaId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "FK_Users_Fichas_FichaAsignadaId" FOREIGN KEY ("FichaAsignadaId") REFERENCES public."Fichas"("Id") ON DELETE SET NULL;


--
-- PostgreSQL database dump complete
--

\unrestrict ZPmxamLFqU5c0JX8kKGm7mHRuAXHRuhksmYaBi1sGyujO5IFb97tN42O0W6nOf8

