# Especificación de Requisitos de Software (SRS)
## Norma IEEE Std 830-1998 — SIPITEX

| Campo | Valor |
|-------|--------|
| **Proyecto** | SIPITEX — Sistema Integrado de Producción e Inventario Textil |
| **Institución** | SENA CMTC · Programa ADSO |
| **Versión del documento** | 2.1 |
| **Fecha** | 2026-07-28 |
| **Estado** | Alineado con la implementación actual |
| **Tecnologías** | ASP.NET Core MVC · EF Core · SQLite · Docker |
| **Autores** | Equipo SIPITEX |

> Este documento sigue la estructura de la norma **IEEE Std 830-1998** (*Recommended Practice for Software Requirements Specifications*).  
> Los diagramas se muestran como **imágenes** para que se vean en cualquier visor de Markdown, PDF o presentación.

---

## Tabla de contenido

1. [Introducción](#1-introducción)
2. [Descripción general del sistema](#2-descripción-general-del-sistema)
3. [Requisitos específicos](#3-requisitos-específicos)
4. [Diagramas del sistema](#4-diagramas-del-sistema)
5. [Apéndices](#5-apéndices)

---

## 1. Introducción

### 1.1 Propósito

Este documento describe **qué debe hacer** el sistema SIPITEX.

Sirve para:

- Acordar el alcance entre instructores, bodega, administración y el equipo de desarrollo.
- Guiar la implementación y las pruebas.
- Presentar el diseño con diagramas UML y el modelo de datos.

### 1.2 Alcance

SIPITEX es una aplicación web para la **intranet del centro de formación**. Permite gestionar:

| Módulo | Qué hace |
|--------|----------|
| **Usuarios** | Login, roles y permisos |
| **Inventario** | Materiales, stock y estado físico |
| **Solicitudes** | Pedidos de material y aprobación en bodega |
| **Órdenes** | Órdenes de producción de cualquier prenda |
| **MRP / BOM** | Lista de materiales y cálculo de requerimientos |
| **Fichas** | Aprendices / procesos y registro de producción |
| **Calidad** | Inspecciones (aprobado o reproceso) |
| **Reportes** | Exportación PDF / Excel con filtros |
| **Alertas** | Avisos por correo o bandeja de demostración |
| **Estadísticas** | KPIs y gráficos de avance |

**Fuera de alcance (versión actual):** facturación, nómina, ERP externo y aplicación móvil nativa.

### 1.3 Definiciones

| Término | Significado |
|---------|-------------|
| **BOM** | *Bill of Materials*: materiales necesarios por unidad de producto |
| **MRP** | *Material Requirements Planning*: cálculo de lo que falta comprar o pedir |
| **RF** | Requisito funcional (qué hace el sistema) |
| **RNF** | Requisito no funcional (cómo debe comportarse: velocidad, seguridad, etc.) |
| **Ficha** | Grupo o proceso de formación asociado a un instructor |
| **SRS** | Especificación de requisitos de software |

### 1.4 Referencias

- IEEE Std 830-1998  
- Documentos de fases: `docs/01` a `docs/05`  
- Código fuente del repositorio SIPITEX  
- Imágenes de diagramas: carpeta [`docs/diagramas/`](diagramas/)

### 1.5 Organización del documento

| Sección | Contenido |
|---------|-----------|
| **2** | Visión general: actores, restricciones y arquitectura |
| **3** | Lista detallada de requisitos (RF y RNF) |
| **4** | Diagramas visuales (casos de uso, clases, secuencias, ER) |
| **5** | Credenciales demo e historial de versiones |

---

## 2. Descripción general del sistema

### 2.1 Perspectiva del producto

SIPITEX es una aplicación web **monolítica por capas** (arquitectura limpia simplificada).

![Arquitectura por capas de SIPITEX](diagramas/00-arquitectura.png)

**Lectura rápida del diagrama:**

1. **Presentación** — pantallas y controladores (`Sipitex.Web`).  
2. **Aplicación** — reglas de negocio y DTOs.  
3. **Dominio** — entidades (Material, Orden, Ficha, etc.).  
4. **Infraestructura** — base de datos SQLite con EF Core.

### 2.2 Funciones principales (resumen)

1. Autenticar usuarios y controlar el acceso por rol o permiso.  
2. Registrar y consultar materiales (niveles: Agotado / Por agotarse / Normal).  
3. Crear órdenes de producción para cualquier producto.  
4. Mantener el BOM y simular el MRP.  
5. Solicitar, aprobar o rechazar salidas de bodega.  
6. Registrar producción por ficha y avance de la orden.  
7. Registrar inspecciones de calidad.  
8. Generar reportes filtrables y alertas.

### 2.3 Usuarios del sistema

| Actor | Rol en el centro | Qué hace en SIPITEX |
|-------|------------------|---------------------|
| **Administrador** | Gestión del sistema | Usuarios, permisos, órdenes, reportes y alertas |
| **Bodeguero** | Almacén | Stock, estado del material y aprobación de solicitudes |
| **Instructor** | Formación / línea de producción | Solicitudes, sesiones de producción y calidad |

### 2.4 Restricciones

- Base de datos **SQLite** (`sipitex.db`) para desarrollo e intranet.  
- Autenticación por **cookies** (sesión web), no JWT en la versión actual.  
- Navegadores modernos; interfaz responsiva.  
- Despliegue opcional con **Docker Compose**.

### 2.5 Supuestos

- Existe red local (intranet) del centro.  
- Hay usuarios de demostración para pruebas (ver apéndice).  
- Si no hay SMTP configurado, las alertas se guardan en `email-outbox/`.

---

## 3. Requisitos específicos

### 3.1 Requisitos funcionales (RF)

| ID | Módulo | Descripción | Prioridad |
|----|--------|-------------|-----------|
| RF01 | Usuarios | Crear, editar y desactivar usuarios por rol | Alta |
| RF02 | Usuarios | Iniciar y cerrar sesión con cookies | Alta |
| RF03 | Usuarios | Otorgar permisos extendidos desde el administrador | Alta |
| RF04 | Inventario | Registrar cualquier material (nombre, stock, mínimo, unidad) | Alta |
| RF05 | Inventario | Consultar stock y filtrar por nivel (Agotado / Por agotarse / Normal) | Alta |
| RF06 | Inventario | Actualizar estado físico (Bueno / Regular / Deteriorado) | Media |
| RF07 | Inventario | Ajustar stock y fecha de última entrada | Media |
| RF08 | Salida | Solicitar material para una orden (crear material si no existe) | Alta |
| RF09 | Salida | Aprobar o rechazar solicitud; al aprobar, descontar stock | Alta |
| RF10 | Órdenes | Crear orden con cualquier nombre de producto | Alta |
| RF11 | Órdenes | Registrar avance y finalizar al alcanzar la meta | Alta |
| RF12 | MRP | Mantener BOM por producto | Alta |
| RF13 | MRP | Simular requerimiento neto frente al stock | Alta |
| RF14 | Fichas | Asociar ficha a proceso, instructor y orden | Alta |
| RF15 | Producción | Registrar sesión diaria (ficha, unidades, observaciones) | Alta |
| RF16 | Calidad | Registrar inspección; en reproceso exigir motivo y responsable | Media |
| RF17 | Reportes | Exportar inventario, órdenes, calidad y dashboard (PDF/Excel) | Alta |
| RF18 | Reportes | Filtrar por día, semana, mes, año, instructor o ficha | Alta |
| RF19 | Alertas | Preferencias por usuario y evaluación de eventos | Media |
| RF20 | Estadísticas | Dashboard con KPIs y gráfico de avance | Alta |

### 3.2 Requisitos no funcionales (RNF)

| ID | Descripción |
|----|-------------|
| RNF01 | Tiempo de respuesta percibido menor a 2 segundos en intranet |
| RNF02 | Sesión autenticada con expiración (8 horas) |
| RNF03 | Control de acceso por rol y permisos extendidos |
| RNF04 | Acceso desde cualquier PC de la intranet del centro |
| RNF05 | Interfaz responsiva (móvil y escritorio) |
| RNF06 | Código modular por capas y documentado |
| RNF07 | Despliegue reproducible con Docker Compose |
| RNF08 | Integridad de datos con EF Core / SQLite |

### 3.3 Interfaces externas

| Tipo | Detalle |
|------|---------|
| **Usuario** | Navegador web (HTML, CSS, JavaScript) |
| **Hardware** | Servidor HTTP o contenedor Docker |
| **Software** | .NET 10, EF Core, QuestPDF, ClosedXML, MailKit |
| **Comunicaciones** | HTTP(S) local; SMTP opcional para alertas |

---

## 4. Diagramas del sistema

> Todas las imágenes están en [`docs/diagramas/`](diagramas/).  
> Los archivos fuente Mermaid (por si se editan después) están en [`docs/diagramas/src/`](diagramas/src/).

### 4.1 Diagrama de casos de uso

Muestra **quién** usa el sistema y **qué puede hacer**.

![Diagrama de casos de uso — SIPITEX](diagramas/01-casos-de-uso.png)

#### Matriz actor ↔ caso de uso

| Caso de uso | Admin | Bodeguero | Instructor |
|-------------|:-----:|:---------:|:----------:|
| 1. Iniciar sesión | ✓ | ✓ | ✓ |
| 2. Gestionar usuarios | ✓ | | |
| 3. Registrar materiales | ✓ | ✓ | con permiso |
| 4. Consultar stock | ✓ | ✓ | ✓ |
| 5. Solicitar material | ✓ | | ✓ |
| 6. Aprobar / rechazar | ✓ | ✓ | con permiso |
| 7. Crear orden | ✓ | | |
| 8. Registrar producción | ✓ | | ✓ |
| 9. BOM / MRP | ✓ | ✓ | con permiso |
| 10. Control de calidad | ✓ | | ✓ |
| 11. Descargar reportes | ✓ | ✓ | ✓ |
| 12. Configurar alertas | ✓ | parcial | parcial |

---

### 4.2 Diagrama de clases (dominio)

Representa las **entidades principales** del negocio y cómo se relacionan.

![Diagrama de clases del dominio](diagramas/02-clases-dominio.png)

#### Vista de capas (controllers → servicios → repositorio)

![Vista lógica de capas de aplicación](diagramas/03-capas-aplicacion.png)

---

### 4.3 Diagramas de secuencia

Explican el **orden de los mensajes** entre usuario, controladores, servicios y base de datos.

#### 4.3.1 Inicio de sesión

![Secuencia: login](diagramas/04-secuencia-login.png)

#### 4.3.2 Solicitar material

![Secuencia: solicitar material](diagramas/05-secuencia-solicitar.png)

#### 4.3.3 Aprobar solicitud y descontar stock

![Secuencia: aprobar solicitud](diagramas/06-secuencia-aprobar.png)

#### 4.3.4 Crear orden de producción

![Secuencia: crear orden](diagramas/07-secuencia-crear-orden.png)

#### 4.3.5 Descargar reporte filtrado

![Secuencia: reportes](diagramas/08-secuencia-reportes.png)

#### 4.3.6 Agregar material al BOM (MRP)

![Secuencia: agregar BOM](diagramas/09-secuencia-bom.png)

---

### 4.4 Diagrama entidad-relación (ER)

Modelo de la **base de datos**: tablas, claves y relaciones.

![Diagrama entidad-relación](diagramas/10-entidad-relacion.png)

---

## 5. Apéndices

### 5.1 Credenciales de demostración

| Rol | Correo | Contraseña |
|-----|--------|------------|
| Administrador | `admin@sipitex.test` | `Admin123!` |
| Instructor | `instructor@sipitex.test` | `Instructor123!` |
| Bodeguero | `bodega@sipitex.test` | `Bodega123!` |

### 5.2 Cómo ejecutar el sistema

```bash
cd src/Sipitex.Web
dotnet run
```

Abrir la URL que muestre la consola (por ejemplo `http://localhost:5240`).

Con Docker:

```bash
docker compose up --build
```

Abrir `http://localhost:8080`.

### 5.3 Cómo regenerar las imágenes de diagramas

```bash
cd docs/diagramas
npm install
npx mmdc -i src/01-casos-de-uso.mmd -o 01-casos-de-uso.png -b white -s 2 -w 1400
# Repetir para cada archivo .mmd de la carpeta src/
```

### 5.4 Historial de versiones

| Versión | Fecha | Cambios |
|---------|-------|---------|
| 1.0 | 2025 | Requisitos iniciales RF01–RF20 |
| 2.0 | 2026-07-23 | Auth cookies, permisos, productos libres, reportes filtrables, diagramas UML/ER |
| **2.1** | **2026-07-28** | Documento más claro para presentación; diagramas exportados como **imágenes PNG** visibles |

---

*Fin del documento IEEE 830 — SIPITEX v2.1*
