# SIPITEX — Guía de uso para exposición

**Sistema Integrado de Producción e Inventario Textil**  
CMTC · SENA · ADSO  

Documento orientado a **demostrar el aplicativo en una exposición**: cómo arrancarlo, con qué roles ingresar y cómo recorrer cada módulo de punta a punta.

---

## 1. ¿Qué es SIPITEX?

SIPITEX es una aplicación web de intranet que apoya la gestión de producción textil en el centro de formación. Integra:

| Área | Qué resuelve |
|------|----------------|
| Inventario | Materias primas, stock, estados y mínimos |
| Producción | Órdenes, avance y fichas de proceso |
| Materiales (MRP) | Lista de materiales (BOM) y simulación de requerimientos |
| Calidad | Inspecciones, reprocesos y responsables |
| Análisis | KPIs, reportes PDF/Excel y alertas por correo |
| Administración | Usuarios, perfiles y recuperación de contraseña |

**Fuera de alcance en esta versión:** facturación, nómina, ERP externo y aplicación móvil nativa.

---

## 2. Cómo iniciar el sistema (antes de la exposición)

### Opción A — Local / Codespaces

```bash
cd src/Sipitex.Web
dotnet run
```

Abrir en el navegador la URL que muestre la consola, por ejemplo:

- `http://localhost:5240`
- o `https://localhost:7084`

La ruta inicial es **Inventario**; si no hay sesión, redirige a **Iniciar sesión**.

### Opción B — Docker

```bash
docker compose up --build
```

Abrir `http://localhost:8080`.

### Datos de demostración

Al primer arranque se crean materiales, BOM (Camisa / Pantalón), órdenes, fichas y tres usuarios demo.

---

## 3. Usuarios demo (usar en la exposición)

| Rol | Correo | Contraseña | Para demostrar |
|-----|--------|------------|----------------|
| **Administrador** | `admin@sipitex.test` | `Admin123!` | Todo el sistema, crear órdenes y usuarios |
| **Instructor** | `instructor@sipitex.test` | `Instructor123!` | Fichas, calidad, solicitar materiales |
| **Bodeguero** | `bodega@sipitex.test` | `Bodega123!` | Inventario, aprobar solicitudes, MRP |

> Tip de exposición: inicie sesión primero como **Administrador** para un recorrido completo; luego muestre Instructor y Bodeguero para contrastar permisos.

---

## 4. Roles y menú (qué ve cada actor)

### 4.1 Resumen de permisos

| Capacidad | Administrador | Instructor | Bodeguero |
|-----------|:-------------:|:----------:|:---------:|
| Inventario (consultar) | Sí | Sí | Sí |
| Agregar / ajustar materiales | Sí | No* | Sí |
| Solicitar material | Sí | Sí | No |
| Aprobar / rechazar solicitudes | Sí | No* | Sí |
| Crear órdenes de producción | Sí | No | No |
| Registrar avance (+10u) | Sí | Sí | No |
| Simular MRP | Sí | No* | Sí |
| Fichas y Control de calidad | Sí | Sí | No |
| Estadísticas / Reportes / Alertas | Sí | Sí | Sí |
| Gestión de usuarios | Sí | No | No |
| Mi perfil (foto, funciones, clave) | Sí | Sí | Sí |

\*El administrador puede otorgar **permisos extendidos** a un instructor (por ejemplo aprobar solicitudes o simular MRP).

### 4.2 Menú lateral

**Operación**

- Inventario  
- Órdenes de producción  
- MRP / Materiales  
- Fichas & producción *(solo Admin e Instructor)*  
- Control de calidad *(solo Admin e Instructor)*  

**Análisis**

- Estadísticas  
- Reportes  
- Alertas  
- Usuarios *(solo Admin)*  

**Cuenta / perfil**

- **Mi perfil** — foto, descripción de funciones, datos y contraseña  
- También se abre haciendo clic en el **nombre / avatar** (arriba a la derecha) o en el botón **Mi perfil**

---

## 5. Recorrido recomendado para la exposición (15–20 min)

Siga este orden para contar una historia completa: *pedido → materiales → producción → calidad → análisis*.

### Paso 1 — Iniciar sesión (Administrador)

1. Ir a **Iniciar sesión**.  
2. Usar `admin@sipitex.test` / `Admin123!`.  
3. Comentar: autenticación por cookie, roles y menú según el perfil.

### Paso 2 — Inventario

1. Menú → **Inventario**.  
2. Mostrar materiales demo (Tela Jersey, Hilo, Cremallera, Forro).  
3. **Agregar material** (nombre, stock, unidad).  
4. **Ajustar stock** y cambiar **estado** (Bueno / Regular / Deteriorado).  
5. Señalar la alerta si el stock está bajo el mínimo.

### Paso 3 — Crear una orden de producción

1. Menú → **Órdenes de producción**.  
2. Solo Admin: producto (**Camisa** o **Pantalón**), cantidad y fecha límite.  
3. Generar orden: el sistema valida el BOM y muestra el avance / hint de MRP.  
4. Opcional: botón **+10u** para incrementar unidades producidas.

### Paso 4 — MRP / Materiales

1. Menú → **MRP / Materiales**.  
2. Revisar la tabla BOM (qué material exige cada producto).  
3. **Simular**: producto + cantidad → Required / Available / Déficit.

### Paso 5 — Solicitud y despacho de materiales

1. Con **Instructor** (`instructor@sipitex.test`): Inventario → **Solicitar** (orden + material + cantidad).  
2. Estado queda **Pendiente**.  
3. Cerrar sesión e ingresar como **Bodeguero** (`bodega@sipitex.test`).  
4. En Inventario → **Aprobar** (descuenta stock) o **Rechazar**.  
5. Mensaje clave: *Instructor pide; bodega controla y despacha*.

### Paso 6 — Fichas y registro de producción

1. Como **Admin** o **Instructor** → **Fichas & producción**.  
2. **Registrar ficha** (código, proceso, instructor, turno, orden).  
3. **Registrar hoy** o **Registrar sesión diaria** (unidades + observaciones).  
4. Explicar: el Instructor solo ve / registra en **sus** fichas; el Admin ve todas.  
5. Usar filtros por código, instructor o turno.

### Paso 7 — Control de calidad

1. Menú → **Control de calidad**.  
2. Registrar inspección: orden, unidades, resultado (**Aprobada / Reproceso / Rechazada**).  
3. Si es **Reproceso**: motivo y responsable (obligatorios).  
4. Relacionar con KPIs y alertas de reproceso.

### Paso 8 — Estadísticas y reportes

1. **Estadísticas**: prendas producidas, tasa de calidad, órdenes activas, materiales bajo mínimo y gráfico.  
2. **Reportes**: exportar **PDF** o **Excel** de Inventario, Órdenes, Calidad o Dashboard KPI.

### Paso 9 — Alertas

1. Menú → **Alertas**.  
2. Activar preferencias (stock bajo, solicitudes pendientes, órdenes por vencer, reprocesos, atrasadas).  
3. Guardar.  
4. Como Admin: **Evaluar alertas**.  
5. Sin SMTP configurado, los correos quedan en la carpeta `email-outbox/` (útil para demo).

### Paso 10 — Usuarios y Mi perfil

1. Como Admin → **Usuarios** → crear **Instructor** o **Bodeguero** (contraseña temporal, ficha opcional, permisos extendidos).  
2. Entrar con esa cuenta → **Mi perfil**:  
   - foto (JPG/PNG/WEBP, máx. 2 MB)  
   - descripción de funciones  
   - cambio de contraseña  
3. En login: enlace **¿Olvidaste tu contraseña?** para recuperación.

---

## 6. Guía por módulo (referencia detallada)

### 6.1 Inicio de sesión y seguridad

| Acción | Dónde | Notas |
|--------|-------|--------|
| Entrar | `/Account/Login` | Correo + contraseña |
| Salir | Botón **Salir** | Cierra la sesión |
| Recuperar clave | ¿Olvidaste tu contraseña? | Enlace por correo o `email-outbox/` |
| Acceso denegado | `/Account/AccessDenied` | Intento sin permiso |

### 6.2 Inventario (`/Inventario`)

- Listado de materias primas con stock, mínimo, unidad y estado.  
- Solicitudes de material con estados: **Pendiente**, **Aprobada**, **Rechazada**.  
- Aprobar una solicitud **descuenta** del stock disponible.

### 6.3 Órdenes (`/Ordenes`)

- Listado: número, producto, meta, producido, % avance, estado, plazo.  
- Estados: Pendiente, En proceso, Finalizada, Cancelada.  
- Solo el Administrador crea nuevas órdenes con productos del BOM demo.

### 6.4 MRP (`/Mrp`)

- Consulta de lista de materiales por producto.  
- Simulación de necesidad neta antes de producir.

### 6.5 Fichas (`/Fichas`)

- Fichas ligadas a proceso (Trazo, Corte, Confección, etc.), turno e instructor.  
- Sesiones diarias que actualizan el avance de la orden y consumen materiales según BOM.

### 6.6 Calidad (`/Calidad`)

- Historial de inspecciones.  
- Reproceso exige trazabilidad (motivo + responsable).

### 6.7 Estadísticas (`/Estadisticas`)

- Tablero de indicadores para la exposición o visitas de seguimiento.

### 6.8 Reportes (`/Reportes`)

| Reporte | Formatos |
|---------|----------|
| Inventario | PDF / Excel |
| Órdenes | PDF / Excel |
| Calidad | PDF / Excel |
| Dashboard KPI | PDF / Excel |

### 6.9 Alertas (`/Alertas`)

Tipos disponibles:

1. Stock bajo mínimo  
2. Solicitudes pendientes  
3. Órdenes por vencer (≤ 7 días)  
4. Reprocesos de calidad  
5. Órdenes atrasadas  

### 6.10 Usuarios (`/Account/Users`) — solo Administrador

- Crear cuentas de **Instructor** y **Bodeguero** (no se crean nuevos administradores desde la UI).  
- Editar, activar / desactivar y asignar ficha o permisos extendidos.  
- En el listado se ve la foto y la descripción de funciones que cada usuario escribió en su perfil.

### 6.11 Mi perfil (`/Account/Profile`)

Disponible para **Administrador, Instructor y Bodeguero**:

1. Foto de perfil  
2. Nombre y correo  
3. Rol (solo lectura; lo asigna el administrador)  
4. Descripción de mis funciones (texto libre, hasta 800 caracteres)  
5. Cambio de contraseña  

---

## 7. Guion corto sugerido (5 minutos)

Si el tiempo es limitado, use este guion:

1. **Login Admin** → explicar roles.  
2. **Órdenes** → crear una orden de Camisa.  
3. **Inventario** → solicitar material (cambiar a Instructor) y aprobar (Bodeguero).  
4. **Fichas** → registrar producción del día.  
5. **Calidad** → una inspección Aprobada.  
6. **Estadísticas + un reporte PDF**.  
7. **Mi perfil** → mostrar foto y descripción de funciones.  
8. Cierre: SIPITEX integra inventario, producción, calidad y análisis en un solo sistema.

---

## 8. Preguntas frecuentes (para la exposición)

**¿Se puede usar sin Internet?**  
Sí, en red local / Codespaces / Docker. El correo real requiere SMTP; sin SMTP se usa `email-outbox/`.

**¿Quién crea las cuentas?**  
Solo el **Administrador**. Instructor y Bodeguero completan su perfil después.

**¿El Instructor ve todas las fichas?**  
No: solo las suyas. El Administrador ve todas.

**¿El Bodeguero registra calidad?**  
No. Su foco es inventario, solicitudes y MRP.

**¿Qué productos trae el demo?**  
Camisa y Pantalón, con BOM precargado.

**¿Dónde está la base de datos?**  
Archivo SQLite `sipitex.db` (local o volumen Docker `sipitex-data`).

---

## 9. Checklist rápido antes de presentar

- [ ] `dotnet run` o `docker compose up` funcionando  
- [ ] Navegador abierto en la URL correcta  
- [ ] Credenciales demo a la vista (o anotadas)  
- [ ] Probar login Admin → Inventario carga  
- [ ] (Opcional) Tener una orden y una solicitud listas  
- [ ] Saber dónde está **Mi perfil** y **Usuarios**  
- [ ] Si usan alertas: revisar `email-outbox/` o SMTP  

---

## 10. Referencias técnicas del proyecto

| Recurso | Ubicación |
|---------|-----------|
| README general | [`README.md`](../README.md) |
| Requisitos | [`01-Requisitos.md`](01-Requisitos.md) |
| Diseño | [`02-Diseno.md`](02-Diseno.md) |
| Despliegue | [`05-Despliegue.md`](05-Despliegue.md) |
| Especificación IEEE 830 | [`08-IEEE830-Especificacion.md`](08-IEEE830-Especificacion.md) |
| Diagramas | [`diagramas/`](diagramas/) |

---

*Documento preparado para exposición académica / demostración funcional de SIPITEX.*
