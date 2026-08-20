# Manual de uso — Instructor

**SIPITEX** · Sistema Integrado de Aprendizaje Producción e Inventario Textil  
CMTC · SENA · ADSO

---

## 1. ¿Para quién es este manual?

Para el **Instructor**: registra producción por ficha, solicita materiales **a una bodega concreta**, hace control de calidad y consulta indicadores. Su cuenta la crea el **Administrador**.

No tiene inventario general: no ve el catálogo completo de las dos bodegas. Pide materiales desde **Fichas** o **Solicitar insumos**.

### Credencial de demostración

| Campo | Valor |
|-------|--------|
| Correo | `instructor@sipitex.test` |
| Contraseña | `Instructor123!` |

---

## 2. Cómo iniciar sesión

1. Abra SIPITEX en el navegador.  
2. En **Iniciar sesión**, escriba su correo y contraseña.  
3. Pulse **Entrar**.  
4. Llegará a **Órdenes de producción** (no a Inventario).

**Salir:** botón **Salir** (arriba a la derecha).

**Si olvidó la contraseña:** en el login use **¿Olvidaste tu contraseña?**.

---

## 3. Qué puede y qué no puede hacer

### Puede

| Módulo | Acciones |
|--------|----------|
| Órdenes | Ver órdenes y registrar avance (según permisos de etapa) |
| MRP | Consultar la lista de materiales (BOM) |
| Fichas | Ver y registrar solo en **sus** fichas; **solicitar materiales** eligiendo bodega |
| Mis solicitudes | Ver **sus** solicitudes (columna Bodega) |
| Solicitar insumos | Pedir insumos por descripción, eligiendo bodega |
| Calidad | Registrar inspecciones y reprocesos |
| Estadísticas / Reportes / Alertas | Consultar y configurar preferencias de alerta |
| Mi perfil | Foto, descripción de funciones y contraseña |

### No puede (por defecto)

- Entrar a **Inventario** ni **Movimientos de stock**.  
- Agregar o ajustar stock (eso lo hace Bodega / Admin).  
- Aprobar o rechazar solicitudes (Bodega).  
- Ver solicitudes de **otra** ficha / otro instructor.  
- Simular MRP, crear órdenes o gestionar usuarios (salvo permiso extendido que dé el Admin).

En el menú **no** le aparecen Inventario ni «Solicitudes de materiales» de bodega: es normal.

---

## 4. Mi perfil (importante al empezar)

1. Menú → **Mi perfil**, o clic en su **nombre / avatar**.  
2. Complete foto, descripción de funciones, nombre/correo y contraseña si el Admin le dio una temporal.  
3. **Guardar perfil**.

El rol **Instructor** lo asigna el Administrador. Un instructor **no** lleva bodega asignada en su cuenta: elige la bodega **en cada solicitud**.

---

## 5. Solicitar materiales por ficha (flujo principal)

**Menú → Fichas & producción** → en su ficha, **Solicitar materiales**.

Hay **dos bodegas**. Debe indicar a cuál va el pedido **antes** de elegir materiales.

1. Abra el formulario de la ficha.  
2. En **Bodega**, elija **Bodega 1** o **Bodega 2**.  
   - Hasta que elija bodega, los materiales del catálogo no son seleccionables.  
   - Al elegir **Bodega 1**, desaparecen (quedan deshabilitados) los materiales de Bodega 2, y al revés.  
3. Agregue una o más líneas: material + cantidad.  
4. Observaciones (opcional) → **Enviar solicitud**.  
5. El estado queda **Pendiente** hasta que el bodeguero de **esa** bodega la resuelva.

Si envía un material que no es de la bodega elegida, el sistema muestra: *«El material '…' no pertenece a la bodega seleccionada.»* Vuelva a elegir bodega y material.

---

## 6. Solicitar insumos (sin ficha)

**Menú → Solicitar insumos**

Para pedidos por **descripción** (un insumo que no está ligado a la ficha de formación).

1. Elija **Bodega**.  
2. Escriba las descripciones y cantidades.  
3. Envíe.  
4. El bodeguero de esa bodega mapeará cada línea a un material **de su catálogo** (o creará uno en esa misma bodega).

---

## 7. Mis solicitudes

**Menú → Mis solicitudes**

Solo las que **usted** creó. La tabla incluye **Bodega**.

Pulse **Ver detalle**: cabecera con Código, Estado, Fecha y **Bodega**, más las líneas (cantidad solicitada / aprobada).

El Administrador ve el listado global; usted no ve las de otros instructores.

---

## 8. Órdenes de producción

**Menú → Órdenes de producción**

- Consulte número de orden, producto, meta, producido y estado.  
- El avance por etapas depende de los permisos que le asigne el Administrador.

No puede crear órdenes nuevas salvo que tenga el permiso extendido **Crear órdenes de producción**.

---

## 9. MRP / Materiales (consulta)

**Menú → MRP / Materiales**

- Vea qué materiales exige cada producto.  
- Sirve para saber **qué** pedir; la **bodega** la elige al armar la solicitud.  
- La **simulación** MRP suele estar restringida a Admin/Bodega.

---

## 10. Fichas y registro de producción

**Menú → Fichas & producción**

Solo ve las fichas donde usted es instructor asignado.

- Registrar ficha (si aplica): código, proceso, turno, orden.  
- **Registrar hoy** o **sesión diaria**: unidades y observaciones.  
- Filtros por código de ficha, instructor o turno.

---

## 11. Control de calidad

**Menú → Control de calidad**

1. Elija la **orden**.  
2. Unidades inspeccionadas y resultado (**Aprobada**, **Reproceso** con motivo y responsable, o **Rechazada**).  
3. Guarde.

---

## 12. Estadísticas, reportes y alertas

- **Estadísticas:** producción, calidad, órdenes activas.  
- **Reportes:** PDF o Excel.  
- **Alertas:** active avisos (órdenes por vencer, reprocesos) y **Guardar**. Sin SMTP, bandeja `email-outbox/`.

---

## 13. Rutina diaria sugerida (Instructor)

1. Entrar (primera vez: **Mi perfil**).  
2. Ver **Órdenes** de sus procesos.  
3. En **Fichas**, **solicitar materiales** a la bodega correcta (o **Solicitar insumos**).  
4. Seguir el estado en **Mis solicitudes** (columna Bodega).  
5. Registrar producción del día.  
6. **Calidad**.  
7. **Estadísticas** / **Reporte** si debe reportar avance.  
8. **Salir**.

---

## 14. Problemas frecuentes

| Situación | Qué hacer |
|-----------|-----------|
| No aparece Inventario en el menú | Correcto: el Instructor no consulta el catálogo general |
| No puede elegir materiales | Primero seleccione **Bodega** en el formulario |
| Desaparecieron materiales al cambiar de bodega | Es el filtro: cada material pertenece a una sola bodega |
| La solicitud sigue Pendiente | La atiende el bodeguero de **esa** bodega, no el de la otra |
| No ve una ficha | Pida al Admin que la asigne a su usuario |
| No puede crear una orden | Es función del Administrador (o permiso extendido) |
| Olvidó la contraseña | Use **¿Olvidaste tu contraseña?** |
| Acceso denegado | Está intentando una acción de otro rol |

---

## 15. Flujo rápido de trabajo

```
Login Instructor
    → Fichas: elegir Bodega 1 o Bodega 2
    → Solicitar materiales de ESA bodega
    → Mis solicitudes (ver Bodega y estado)
    → Esperar resolución del bodeguero de esa bodega
    → Registrar producción (Fichas)
    → Inspeccionar (Calidad)
```

---

*Fin del manual del Instructor.*
