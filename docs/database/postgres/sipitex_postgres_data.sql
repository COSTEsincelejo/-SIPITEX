--
-- PostgreSQL database dump
--

\restrict 7ZYq7LTNKjdx8W735Z5HQgNgwBiqAFbpvV8SYJGWMGSxkv448MgMj68VuF2lf9y

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

--
-- Data for Name: ProductionOrders; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."ProductionOrders" ("Id", "OrderNumber", "ProductName", "TotalQuantity", "ProducedQuantity", "Status", "Deadline") VALUES (1, 'OP-001', 'Camisa', 120, 45, 1, '2025-04-15');
INSERT INTO public."ProductionOrders" ("Id", "OrderNumber", "ProductName", "TotalQuantity", "ProducedQuantity", "Status", "Deadline") VALUES (2, 'OP-002', 'Pantalón', 80, 20, 1, '2025-04-20');


--
-- Data for Name: Fichas; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."Fichas" ("Id", "FichaCode", "ProcessName", "InstructorName", "Turno", "InstructorUserId", "ProductionOrderId") VALUES (2, 'FICHA-C2', 'Corte', 'Carlos Méndez', 'Mañana', NULL, 1);
INSERT INTO public."Fichas" ("Id", "FichaCode", "ProcessName", "InstructorName", "Turno", "InstructorUserId", "ProductionOrderId") VALUES (3, 'FICHA-E3', 'Confección', 'Ana Rojas', 'Tarde', NULL, 2);
INSERT INTO public."Fichas" ("Id", "FichaCode", "ProcessName", "InstructorName", "Turno", "InstructorUserId", "ProductionOrderId") VALUES (1, 'FICHA-T1', 'Trazo', 'Laura Gómez', 'Mañana', 2, 1);


--
-- Data for Name: Users; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."Users" ("Id", "Nombre", "Email", "PasswordHash", "Rol", "FichaAsignadaId", "PermisosExtendidos", "PhotoPath", "FuncionDescripcion", "IsActive") VALUES (1, 'Administrador SIPITEX', 'admin@sipitex.test', 'pbkdf2$100000$eo6yUc3Eh3DbJBIIzKPjSw==$69NXAKNnuqUVmcg4hzN6h1PyUBjkOcAhGNYgRdpbhhE=', 'Administrador', NULL, '', NULL, NULL, true);
INSERT INTO public."Users" ("Id", "Nombre", "Email", "PasswordHash", "Rol", "FichaAsignadaId", "PermisosExtendidos", "PhotoPath", "FuncionDescripcion", "IsActive") VALUES (3, 'Pedro Bodega', 'bodega@sipitex.test', 'pbkdf2$100000$3MlzCrtekIn0V1vz7gfA0g==$rNE5vsvv0tlvR8HQsEp9N2MrIfyt6jqFa1+MF4SFpR8=', 'Bodeguero', NULL, '', NULL, NULL, true);
INSERT INTO public."Users" ("Id", "Nombre", "Email", "PasswordHash", "Rol", "FichaAsignadaId", "PermisosExtendidos", "PhotoPath", "FuncionDescripcion", "IsActive") VALUES (2, 'Laura Gómez', 'instructor@sipitex.test', 'pbkdf2$100000$K2TX+AHVZfAr9CCVXPoTnw==$0ay22JuNKvJYvwyzxGUptv2AVRbkGeLQvzDHFIxBl2g=', 'Instructor', 1, '', NULL, NULL, true);


--
-- Data for Name: AlertDeliveries; Type: TABLE DATA; Schema: public; Owner: -
--



--
-- Data for Name: AlertPreferences; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."AlertPreferences" ("Id", "UserId", "AlertType", "Enabled") VALUES (1, 1, 1, true);
INSERT INTO public."AlertPreferences" ("Id", "UserId", "AlertType", "Enabled") VALUES (2, 1, 2, true);
INSERT INTO public."AlertPreferences" ("Id", "UserId", "AlertType", "Enabled") VALUES (3, 1, 3, true);
INSERT INTO public."AlertPreferences" ("Id", "UserId", "AlertType", "Enabled") VALUES (4, 1, 4, true);
INSERT INTO public."AlertPreferences" ("Id", "UserId", "AlertType", "Enabled") VALUES (5, 1, 5, true);
INSERT INTO public."AlertPreferences" ("Id", "UserId", "AlertType", "Enabled") VALUES (6, 3, 1, true);
INSERT INTO public."AlertPreferences" ("Id", "UserId", "AlertType", "Enabled") VALUES (7, 3, 2, true);
INSERT INTO public."AlertPreferences" ("Id", "UserId", "AlertType", "Enabled") VALUES (8, 3, 3, false);
INSERT INTO public."AlertPreferences" ("Id", "UserId", "AlertType", "Enabled") VALUES (9, 3, 4, false);
INSERT INTO public."AlertPreferences" ("Id", "UserId", "AlertType", "Enabled") VALUES (10, 3, 5, false);
INSERT INTO public."AlertPreferences" ("Id", "UserId", "AlertType", "Enabled") VALUES (11, 2, 1, false);
INSERT INTO public."AlertPreferences" ("Id", "UserId", "AlertType", "Enabled") VALUES (12, 2, 2, false);
INSERT INTO public."AlertPreferences" ("Id", "UserId", "AlertType", "Enabled") VALUES (13, 2, 3, true);
INSERT INTO public."AlertPreferences" ("Id", "UserId", "AlertType", "Enabled") VALUES (14, 2, 4, true);
INSERT INTO public."AlertPreferences" ("Id", "UserId", "AlertType", "Enabled") VALUES (15, 2, 5, true);


--
-- Data for Name: Materials; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."Materials" ("Id", "Code", "Name", "Unit", "Stock", "MinStock", "Status", "LastEntryDate") VALUES (1, 'mat1', 'Tela Jersey', 0, 280.00, 80.00, 0, '2026-07-30');
INSERT INTO public."Materials" ("Id", "Code", "Name", "Unit", "Stock", "MinStock", "Status", "LastEntryDate") VALUES (2, 'mat2', 'Hilo Poliéster', 0, 3200.00, 500.00, 0, '2026-07-30');
INSERT INTO public."Materials" ("Id", "Code", "Name", "Unit", "Stock", "MinStock", "Status", "LastEntryDate") VALUES (3, 'mat3', 'Cremallera invisible', 1, 95.00, 40.00, 0, '2026-07-30');
INSERT INTO public."Materials" ("Id", "Code", "Name", "Unit", "Stock", "MinStock", "Status", "LastEntryDate") VALUES (4, 'mat4', 'Forro Satín', 0, 120.00, 50.00, 1, '2026-07-30');


--
-- Data for Name: BomItems; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."BomItems" ("Id", "ProductName", "MaterialId", "QuantityPerUnit", "Unit") VALUES (1, 'Camisa', 1, 1.60, 0);
INSERT INTO public."BomItems" ("Id", "ProductName", "MaterialId", "QuantityPerUnit", "Unit") VALUES (2, 'Camisa', 2, 18.00, 0);
INSERT INTO public."BomItems" ("Id", "ProductName", "MaterialId", "QuantityPerUnit", "Unit") VALUES (3, 'Camisa', 3, 1.00, 1);
INSERT INTO public."BomItems" ("Id", "ProductName", "MaterialId", "QuantityPerUnit", "Unit") VALUES (4, 'Pantalón', 1, 2.20, 0);
INSERT INTO public."BomItems" ("Id", "ProductName", "MaterialId", "QuantityPerUnit", "Unit") VALUES (5, 'Pantalón', 2, 22.00, 0);
INSERT INTO public."BomItems" ("Id", "ProductName", "MaterialId", "QuantityPerUnit", "Unit") VALUES (6, 'Pantalón', 4, 0.80, 0);


--
-- Data for Name: FunctionalRequirements; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (1, 'RF01', 'Crear, editar y desactivar usuarios por rol.', 'Usuarios', 0, 'CRUD de usuarios con roles.');
INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (2, 'RF02', 'Autenticación con credenciales propias por rol.', 'Usuarios', 0, 'Login con cookies de autenticación.');
INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (3, 'RF03', 'Registrar entradas con fecha, cantidad y unidad.', 'Inventario', 0, 'Fecha de última entrada en material.');
INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (4, 'RF04', 'Consultar stock disponible en tiempo real.', 'Inventario', 0, 'Actualización reactiva.');
INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (5, 'RF05', 'Bodeguero registra estado del material (Bueno/Regular/Deteriorado).', 'Inventario', 0, 'Selector de estado en inventario.');
INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (6, 'RF06', 'Instructor solicita materiales (orden, producto, cantidad).', 'Salida', 0, 'Formulario de solicitud.');
INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (7, 'RF07', 'Bodeguero aprueba / rechaza y registra entrega.', 'Salida', 0, 'Aprobación y rechazo implementados.');
INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (8, 'RF08', 'Salida trazada por orden y producto.', 'Salida', 1, 'Se guarda orderId pero no historial visible.');
INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (9, 'RF09', 'Admin crea órdenes con producto, cantidad y fecha.', 'Órdenes', 0, 'Formulario completo.');
INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (10, 'RF10', 'Estados: Pendiente, En Proceso, Finalizada, Cancelada.', 'Órdenes', 1, 'Solo ''En Proceso'' y ''Finalizada''.');
INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (11, 'RF11', 'Cálculo MRP automático por prenda y talla.', 'Órdenes', 0, 'BOM hardcoded, falta diferenciación por talla.');
INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (12, 'RF12', 'Fichas asociadas a un proceso de la línea.', 'Fichas', 0, 'Cada ficha tiene proceso.');
INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (13, 'RF13', 'Un instructor puede tener múltiples fichas.', 'Fichas', 1, 'Estructuralmente posible, sin UI asignación.');
INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (14, 'RF14', 'Instructor registra sesión diaria: ficha, unidades, observaciones.', 'Producción', 0, 'Sesiones con observaciones persistidas.');
INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (15, 'RF15', 'Mostrar avance acumulado vs meta de la orden.', 'Producción', 0, 'Tabla de órdenes muestra avance.');
INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (16, 'RF16', 'Criterios diferenciales por tipo de prenda.', 'Calidad', 2, 'Solo aprobado/reproceso genérico.');
INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (17, 'RF17', 'Prendas en reproceso con trazabilidad de motivo y responsable.', 'Calidad', 0, 'Motivo y responsable obligatorios en reproceso.');
INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (18, 'RF18', 'Reportes de producción por período, instructor y orden.', 'Estadísticas', 1, 'Por orden, falta filtro por período/instructor.');
INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (19, 'RF19', 'Estadísticas de consumo de materiales por producto.', 'Estadísticas', 0, 'Cálculo vía BOM+MRP.');
INSERT INTO public."FunctionalRequirements" ("Id", "Code", "Description", "Module", "Status", "Observation") VALUES (20, 'RF20', 'Dashboard con KPIs: unidades, calidad, eficiencia.', 'Estadísticas', 0, 'KPIs y gráfico.');


--
-- Data for Name: MaterialRequests; Type: TABLE DATA; Schema: public; Owner: -
--



--
-- Data for Name: NonFunctionalRequirements; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."NonFunctionalRequirements" ("Id", "Code", "Description", "Status", "Observation") VALUES (1, 'RNF01', 'Carga rápida en intranet (<2s)', 0, 'HTML estático liviano.');
INSERT INTO public."NonFunctionalRequirements" ("Id", "Code", "Description", "Status", "Observation") VALUES (2, 'RNF02', 'Autenticación con sesión y expiración', 0, 'Cookies con expiración de 8 horas.');
INSERT INTO public."NonFunctionalRequirements" ("Id", "Code", "Description", "Status", "Observation") VALUES (3, 'RNF03', 'Control de acceso por rol', 0, 'Authorize por rol en controladores.');
INSERT INTO public."NonFunctionalRequirements" ("Id", "Code", "Description", "Status", "Observation") VALUES (4, 'RNF04', 'Disponible desde cualquier PC de la intranet', 1, 'Requiere servidor HTTP.');
INSERT INTO public."NonFunctionalRequirements" ("Id", "Code", "Description", "Status", "Observation") VALUES (5, 'RNF05', 'Interfaz responsiva e intuitiva', 0, 'Layout adaptable.');
INSERT INTO public."NonFunctionalRequirements" ("Id", "Code", "Description", "Status", "Observation") VALUES (6, 'RNF06', 'Código modular y documentado', 1, 'Arquitectura por capas.');
INSERT INTO public."NonFunctionalRequirements" ("Id", "Code", "Description", "Status", "Observation") VALUES (7, 'RNF07', 'Despliegue con Docker Compose', 0, 'Dockerfile + docker-compose.yml.');
INSERT INTO public."NonFunctionalRequirements" ("Id", "Code", "Description", "Status", "Observation") VALUES (8, 'RNF08', 'Integridad transaccional', 1, 'EF Core con SQLite o PostgreSQL.');


--
-- Data for Name: PasswordResetTokens; Type: TABLE DATA; Schema: public; Owner: -
--



--
-- Data for Name: ProductionSessions; Type: TABLE DATA; Schema: public; Owner: -
--



--
-- Data for Name: QualityRecords; Type: TABLE DATA; Schema: public; Owner: -
--



--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20260730145651_InitialPostgreSQL', '10.0.9');


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
-- PostgreSQL database dump complete
--

\unrestrict 7ZYq7LTNKjdx8W735Z5HQgNgwBiqAFbpvV8SYJGWMGSxkv448MgMj68VuF2lf9y

