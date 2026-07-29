# Manual de uso — Administrador

**SIPITEX** · Sistema Integrado de Producción e Inventario Textil  
CMTC · SENA · ADSO

---

## 1. ¿Para quién es este manual?

Para el **Administrador**: la persona que coordina el sistema, crea cuentas, genera órdenes de producción y tiene acceso a todos los módulos.

### Credencial de demostración

| Campo | Valor |
|-------|--------|
| Correo | `admin@sipitex.test` |
| Contraseña | `Admin123!` |

---

## 2. Cómo iniciar sesión

1. Abra SIPITEX en el navegador.  
2. En **Iniciar sesión**, escriba su correo y contraseña.  
3. Pulse **Entrar**.  
4. Llegará a **Inventario**.

**Salir:** botón **Salir** (arriba a la derecha).

**Si olvidó la contraseña:** en el login use **¿Olvidaste tu contraseña?** e indique su correo.

---

## 3. Qué puede hacer el Administrador

| Módulo | Acciones principales |
|--------|----------------------|
| Inventario | Agregar materiales, ajustar stock, cambiar estado, solicitar y aprobar/rechazar |
| Órdenes | Crear órdenes, ver avance, sumar +10 unidades |
| MRP | Consultar BOM y simular requerimientos |
| Fichas | Crear fichas, registrar producción, ver todas las sesiones |
| Calidad | Registrar inspecciones y reprocesos |
| Estadísticas | Ver KPIs y gráficos |
| Reportes | Exportar PDF / Excel |
| Alertas | Preferencias y evaluar envíos |
| Usuarios | Crear / editar / activar Instructor y Bodeguero |
| Mi perfil | Foto, descripción de funciones, datos y contraseña |

---

## 4. Mi perfil

1. Menú → **Mi perfil**, o clic en su **nombre / avatar**.  
2. Puede:
   - Subir o quitar **foto** (JPG, PNG o WEBP, máx. 2 MB).  
   - Escribir la **descripción de sus funciones**.  
   - Actualizar nombre y correo.  
   - Cambiar contraseña (dejar en blanco si no desea cambiarla).  
3. Pulse **Guardar perfil**.

El **rol** no se cambia desde aquí; es fijo como Administrador.

---

## 5. Gestión de usuarios

Solo el Administrador crea cuentas de **Instructor** y **Bodeguero**.

### 5.1 Crear un usuario

1. Menú → **Usuarios** → **Crear instructor / bodeguero**.  
2. Complete:
   - Nombre  
   - Correo  
   - Contraseña temporal  
   - Rol: **Instructor** o **Bodeguero**  
   - Ficha asignada (opcional, útil para instructores)  
   - Permisos extendidos (opcional)  
3. **Guardar**.

El usuario podrá luego completar su foto, funciones y cambiar la clave en **Mi perfil**.

### 5.2 Editar o desactivar

1. En **Usuarios**, pulse **Editar**.  
2. Actualice datos, permisos o contraseña.  
3. Use **Desactivar** / **Activar** para controlar el acceso.

> No se crean nuevos Administradores desde la pantalla. El rol del administrador existente no se cambia a Instructor/Bodeguero.

### 5.3 Permisos extendidos (opcionales)

Puede otorgar a un instructor capacidades extra, por ejemplo:

- Registrar materiales en inventario  
- Aprobar / rechazar solicitudes  
- Simular MRP  
- Evaluar / configurar alertas  

---

## 6. Inventario

**Menú → Inventario**

### Agregar material

1. Indique nombre, stock inicial y unidad.  
2. Pulse **Agregar material**.

### Ajustar stock o estado

- En cada fila: actualizar stock o estado (**Bueno / Regular / Deteriorado**).

### Solicitudes de material

- Puede **solicitar** material para una orden.  
- Puede **aprobar** (descuenta stock) o **rechazar** solicitudes pendientes.

Revise la alerta de materiales bajo el stock mínimo.

---

## 7. Órdenes de producción

**Menú → Órdenes de producción**

### Crear una orden (solo Admin)

1. Elija producto (**Camisa** o **Pantalón**).  
2. Indique cantidad total y fecha límite.  
3. Genere la orden: el sistema valida el BOM (lista de materiales) y muestra el avance.

### Avance rápido

- Use **+10u** para sumar 10 unidades producidas (si la orden no está finalizada).

Estados típicos: Pendiente, En proceso, Finalizada, Cancelada.

---

## 8. MRP / Materiales

**Menú → MRP / Materiales**

1. Revise la tabla BOM (materiales por producto).  
2. Use **Simular** con producto y cantidad para ver:
   - Requerido  
   - Disponible  
   - Déficit  

Útil antes de lanzar o ampliar una orden.

---

## 9. Fichas y producción

**Menú → Fichas & producción**

Como Administrador ve **todas** las fichas y sesiones.

### Registrar ficha

1. Código de ficha, proceso (Trazo, Corte, Confección, etc.).  
2. Instructor, turno y orden (opcional).  
3. Guardar.

### Registrar producción

- **Registrar hoy** en una ficha, o  
- **Registrar sesión diaria** (orden, ficha, unidades, observaciones).

Puede filtrar por código de ficha, instructor o turno.

---

## 10. Control de calidad

**Menú → Control de calidad**

1. Seleccione la orden.  
2. Indique unidades inspeccionadas.  
3. Resultado: **Aprobada**, **Reproceso** o **Rechazada**.  
4. Si es **Reproceso**: complete **motivo** y **responsable** (obligatorios).  
5. Guarde.

---

## 11. Estadísticas, reportes y alertas

### Estadísticas

KPIs: prendas producidas, tasa de calidad, órdenes activas, materiales bajo mínimo y gráfico de avance.

### Reportes

**Menú → Reportes** → descargue **PDF** o **Excel** de:

- Inventario  
- Órdenes  
- Calidad  
- Dashboard KPI  

### Alertas

1. Active los tipos que le interesan (stock bajo, solicitudes pendientes, órdenes por vencer, reprocesos, atrasadas).  
2. **Guardar** preferencias.  
3. Use **Evaluar alertas** para disparar el envío.  
4. Sin SMTP, los mensajes quedan en la carpeta `email-outbox/`.

---

## 12. Rutina diaria sugerida (Administrador)

1. Revisar **Inventario** (stock bajo y solicitudes pendientes).  
2. Revisar / crear **Órdenes**.  
3. Verificar **Fichas** y producción del día.  
4. Revisar **Calidad**.  
5. Mirar **Estadísticas** o exportar un **Reporte**.  
6. Crear usuarios nuevos si hace falta.  
7. Actualizar **Mi perfil** cuando cambien sus funciones.

---

## 13. Problemas frecuentes

| Situación | Qué hacer |
|-----------|-----------|
| No puede entrar | Verifique correo/clave o use recuperar contraseña |
| Un instructor no ve una ficha | Asígnela o cree la ficha vinculada a ese instructor |
| No se puede aprobar solicitud | Revise si hay stock suficiente |
| No llega el correo de alerta | Configure SMTP o revise `email-outbox/` |
| Acceso denegado | Está intentando una acción restringida; confirme el rol |

---

*Fin del manual del Administrador.*
