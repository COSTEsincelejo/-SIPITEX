# Manual de uso — Administrador

**SIPITEX** · Sistema Integrado de Aprendizaje Producción e Inventario Textil  
CMTC · SENA · ADSO

---

## 1. ¿Para quién es este manual?

Para el **Administrador**: coordina el sistema, crea cuentas, ve **ambas bodegas** y tiene acceso a todos los módulos.

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
| Inventario | Ver **Bodega 1 y Bodega 2**, filtrar, agregar material eligiendo bodega, ajustar stock y estado |
| Movimientos de stock | Historial de entradas y ajustes |
| Órdenes | Crear órdenes, ver avance |
| MRP | Consultar BOM y simular requerimientos |
| Fichas | Crear fichas, registrar producción, **solicitar materiales** (elige bodega) |
| Mis solicitudes / Solicitar insumos | Ver todas las solicitudes (con columna Bodega) e insumos libres |
| Calidad | Registrar inspecciones y reprocesos |
| Estadísticas | Ver KPIs y gráficos |
| Reportes | Exportar PDF / Excel |
| Alertas | Preferencias y evaluar envíos |
| Usuarios | Crear / editar / activar cuentas y **asignar bodega al Bodeguero** |
| Mi perfil | Foto, descripción de funciones, datos y contraseña |

Un Administrador **no** queda con bodega asignada: ve el inventario consolidado. Si el formulario de usuario manda una bodega, el sistema la ignora para este rol.

---

## 4. Mi perfil

1. Menú → **Mi perfil**, o clic en su **nombre / avatar**.  
2. Puede:
   - Subir o quitar **foto** (JPG, PNG o WEBP, máx. 2 MB).  
   - Escribir la **descripción de sus funciones**.  
   - Actualizar nombre y correo.  
   - Cambiar contraseña (dejar en blanco si no desea cambiarla).  
3. Pulse **Guardar perfil**.

El **rol** no se cambia desde aquí.

---

## 5. Gestión de usuarios

Menú → **Usuarios** → **Crear usuario**.

Puede crear cuentas de **Administrador**, **Instructor** y **Bodeguero**.

### 5.1 Crear un usuario

1. Complete nombre, correo, contraseña temporal y rol.  
2. **Ficha asignada** (opcional): útil para instructores.  
3. **Bodega** (obligatoria si el rol es **Bodeguero**):
   - Elija **Bodega 1** o **Bodega 2**.  
   - Sin bodega, el sistema rechaza el alta: *«Debe asignar una bodega al bodeguero.»*  
   - Para Instructor o Administrador deje **Sin asignar**; aunque elija una, se guarda vacía.  
4. Permisos extendidos (opcional).  
5. **Guardar**.

### 5.2 Editar o desactivar

1. En **Usuarios**, pulse **Editar**.  
2. Actualice datos, **bodega** (si es Bodeguero), permisos o contraseña.  
3. Use **Desactivar** / **Activar** para controlar el acceso.

> Usuario demo `bodega@sipitex.test`: edítelo y asígnele una bodega antes de que opere inventario o solicitudes.

### 5.3 Permisos extendidos (opcionales)

Puede otorgar a un instructor, entre otras:

- Registrar materiales en inventario  
- Aprobar / rechazar solicitudes  
- Simular MRP  
- Crear / editar fichas técnicas (BOM)  
- Crear órdenes de producción  
- Configurar / evaluar alertas  

---

## 6. Inventario (vista consolidada)

**Menú → Inventario**

Ve el catálogo de **las dos bodegas**. Cada fila muestra la columna **Bodega**.

### Filtrar por bodega

Arriba de la tabla: **Todas las bodegas**, **Bodega 1** o **Bodega 2**. Al cambiar, la página recarga con `?bodegaId=`.

### Agregar material

1. Nombre, stock inicial, unidad y origen de entrada.  
2. **Bodega**: elija Bodega 1 o Bodega 2 (select habilitado).  
3. **Agregar material**.  
4. El material aparece con ese **BodegaNombre**.

### Ajustar stock o estado

En cada fila: nuevo stock (con origen si el stock sube) o estado **Bueno / Regular / Deteriorado**.

### Movimientos

**Menú → Movimientos de stock** para el historial de entradas y ajustes.

### Solicitudes legacy en esta pantalla

La tabla inferior de «Solicitudes de material» (por orden) es el flujo antiguo. El flujo actual de taller es **Fichas → Solicitar materiales** y **Solicitar insumos** (ver §9).

---

## 7. Órdenes de producción

**Menú → Órdenes de producción**

1. Cree la orden con un producto del catálogo BOM habilitado, cantidad y fecha.  
2. El sistema valida la ficha técnica.  
3. Siga el avance y el historial de la orden.

Estados típicos: Pendiente, En proceso, Finalizada, Cancelada.

---

## 8. MRP / Materiales

**Menú → MRP / Materiales**

1. Revise la tabla BOM (materiales por producto).  
2. **Simular** con producto y cantidad: requerido, disponible y déficit.

---

## 9. Fichas, solicitudes e insumos

### Fichas & producción

**Menú → Fichas & producción**

Como Administrador ve **todas** las fichas y sesiones.

- Registrar ficha: código, proceso, instructor, turno, orden (opcional).  
- **Solicitar materiales** (por ficha):
  1. Elija **Bodega** (Bodega 1 o Bodega 2).  
  2. El listado de materiales muestra solo los de esa bodega.  
  3. Agregue líneas (material + cantidad) y envíe.  
  4. Si elige un material de otra bodega, el servidor rechaza: *«El material '…' no pertenece a la bodega seleccionada.»*

### Mis solicitudes

**Menú → Mis solicitudes**

Listado global (todas las de instructores). Columna **Bodega**. En el detalle: Código, Estado, Fecha y **Bodega**.

### Solicitar insumos

**Menú → Solicitar insumos**

Pedido por **descripción libre** (sin ficha). Debe elegir **Bodega**. Bodega mapea cada línea a un material de **esa** bodega al resolver.

---

## 10. Control de calidad

**Menú → Control de calidad**

1. Seleccione la orden.  
2. Unidades inspeccionadas y resultado: **Aprobada**, **Reproceso** o **Rechazada**.  
3. Si es **Reproceso**: motivo y responsable (obligatorios).

---

## 11. Estadísticas, reportes y alertas

### Estadísticas

KPIs: producción, calidad, órdenes activas, materiales bajo mínimo y gráfico de avance.

### Reportes

**Menú → Reportes** → PDF o Excel (inventario, órdenes, calidad, dashboard).

### Alertas

1. Active los tipos (stock bajo, solicitudes, órdenes por vencer, reprocesos).  
2. **Guardar**.  
3. **Evaluar alertas** para disparar el envío.  
4. Sin SMTP, los mensajes quedan en `email-outbox/`.

---

## 12. Rutina diaria sugerida (Administrador)

1. Revisar **Usuarios**: cada Bodeguero con bodega asignada.  
2. **Inventario**: filtro por bodega o vista «Todas»; stock bajo.  
3. **Mis solicitudes** / cola que atiende cada bodeguero.  
4. **Órdenes** y **Fichas** del día.  
5. **Calidad**.  
6. **Estadísticas** o un **Reporte**.  
7. Actualizar **Mi perfil** si cambian sus funciones.

---

## 13. Problemas frecuentes

| Situación | Qué hacer |
|-----------|-----------|
| No puede entrar | Verifique correo/clave o recupere contraseña |
| Un bodeguero no ve solicitudes | Edite el usuario y asígnele **Bodega 1** o **Bodega 2** |
| Un instructor no ve una ficha | Asígnela o créela vinculada a ese instructor |
| No se puede crear el bodeguero | Falta la bodega en el formulario |
| Un material «no pertenece a la bodega» | La solicitud y el material deben ser de la misma bodega |
| No llega el correo de alerta | Configure SMTP o revise `email-outbox/` |
| Acceso denegado | Confirme el rol de la sesión |

---

*Fin del manual del Administrador.*
