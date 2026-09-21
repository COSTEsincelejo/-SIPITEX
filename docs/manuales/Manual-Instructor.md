# Manual de uso — Instructor

**SIPITEX** · Sistema Integrado de Producción e Inventario Textil  
CMTC · SENA · ADSO

---

## 1. ¿Para quién es este manual?

Para el **Instructor**: registra producción por ficha, solicita materiales, hace control de calidad y consulta indicadores. Su cuenta la crea el **Administrador**.

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
4. Llegará a **Inventario**.

**Salir:** botón **Salir** (arriba a la derecha).

**Si olvidó la contraseña:** en el login use **¿Olvidaste tu contraseña?**.

---

## 3. Qué puede y qué no puede hacer

### Puede

| Módulo | Acciones |
|--------|----------|
| Inventario | Consultar materiales y **solicitar** material para una orden |
| Órdenes | Ver órdenes y sumar avance **+10u** |
| MRP | Consultar la lista de materiales (BOM) |
| Fichas | Ver y registrar solo en **sus** fichas |
| Calidad | Registrar inspecciones y reprocesos |
| Estadísticas / Reportes / Alertas | Consultar y configurar preferencias de alerta |
| Mi perfil | Foto, descripción de funciones y contraseña |

### No puede (por defecto)

- Crear órdenes de producción (solo el Administrador).  
- Agregar o ajustar stock de materiales (eso lo hace Bodega / Admin).  
- Aprobar o rechazar solicitudes de material (Bodega / Admin).  
- Simular MRP (salvo que el Admin le dé permiso extendido).  
- Gestionar usuarios.  
- Ver o registrar en fichas de **otros** instructores.

---

## 4. Mi perfil (importante al empezar)

1. Menú → **Mi perfil**, o clic en su **nombre / avatar**.  
2. Complete:
   - **Foto** de perfil (JPG, PNG o WEBP, máx. 2 MB).  
   - **Descripción de mis funciones** (qué hace en el taller / proceso).  
   - Nombre y correo si deben actualizarse.  
   - Nueva contraseña (si el Admin le dio una temporal).  
3. **Guardar perfil**.

El **rol Instructor** lo asigna el Administrador; usted no lo cambia.

---

## 5. Inventario — solicitar materiales

**Menú → Inventario**

1. Revise el stock disponible.  
2. En **Solicitudes**, elija:
   - Orden de producción  
   - Material  
   - Cantidad  
3. Pulse **Solicitar**.  
4. El estado quedará **Pendiente** hasta que Bodega o Admin **apruebe** o **rechace**.

> Usted pide; la bodega despacha. No apruebe solicitudes desde este rol (salvo permiso especial).

---

## 6. Órdenes de producción

**Menú → Órdenes de producción**

- Consulte número de orden, producto, meta, producido y estado.  
- Si la orden no está finalizada, puede usar **+10u** para registrar avance rápido de 10 unidades.

No puede crear órdenes nuevas: solicítelas al Administrador.

---

## 7. MRP / Materiales (consulta)

**Menú → MRP / Materiales**

- Vea qué materiales exige cada producto (Camisa, Pantalón, etc.).  
- Sirve para planear solicitudes a bodega.  
- La **simulación** MRP suele estar restringida a Admin/Bodega (a menos que tenga permiso extendido).

---

## 8. Fichas y registro de producción (su módulo principal)

**Menú → Fichas & producción**

Solo ve las fichas donde usted es el instructor asignado.

### Registrar una ficha (si aplica)

1. Código, proceso (Trazo, Corte, Confección…), turno, orden.  
2. Al guardar como Instructor, la ficha queda vinculada a usted.

### Registrar producción del día

Opciones:

1. **Registrar hoy** en la ficha (unidades + observación).  
2. **Registrar sesión diaria**: orden + ficha + unidades + observaciones.

El sistema actualiza el avance de la orden y considera el consumo de materiales según el BOM.

### Filtros

Puede filtrar por código de ficha, instructor o turno para ubicar rápido su trabajo.

---

## 9. Control de calidad

**Menú → Control de calidad**

1. Elija la **orden**.  
2. Indique unidades inspeccionadas.  
3. Resultado:
   - **Aprobada**  
   - **Reproceso** → debe indicar **motivo** y **responsable**  
   - **Rechazada**  
4. Guarde.

Esto alimenta las estadísticas y las alertas de reproceso.

---

## 10. Estadísticas, reportes y alertas

### Estadísticas

Revise indicadores globales: producción, calidad, órdenes activas y materiales bajos.

### Reportes

Descargue PDF o Excel de Inventario, Órdenes, Calidad o Dashboard para reuniones o evidencias.

### Alertas

1. En **Alertas**, active los avisos que le interesan (por ejemplo órdenes por vencer o reprocesos).  
2. **Guardar**.  
3. Recibirá notificaciones según la configuración del sistema (correo o bandeja demo `email-outbox/`).

---

## 11. Rutina diaria sugerida (Instructor)

1. Entrar y revisar **Mi perfil** (primera vez: foto y funciones).  
2. Ver **Órdenes** activas de sus procesos.  
3. En **Inventario**, solicitar materiales si faltan.  
4. En **Fichas**, registrar la producción del día.  
5. En **Calidad**, registrar inspecciones.  
6. Revisar **Estadísticas** o un **Reporte** si debe reportar avance.  
7. **Salir** al terminar.

---

## 12. Problemas frecuentes

| Situación | Qué hacer |
|-----------|-----------|
| No aparece el menú Fichas / Calidad | Confirme que inició sesión como Instructor (no Bodeguero) |
| No ve una ficha | Pida al Admin que la asigne a su usuario |
| La solicitud sigue Pendiente | Espere a que Bodega apruebe o rechace |
| No puede crear una orden | Es función del Administrador |
| Olvidó la contraseña | Use **¿Olvidaste tu contraseña?** en el login |
| Acceso denegado | Está intentando una acción de otro rol |

---

## 13. Flujo rápido de trabajo

```
Login Instructor
    → Solicitar materiales (Inventario)
    → Esperar aprobación de Bodega
    → Registrar producción (Fichas)
    → Inspeccionar (Calidad)
    → Consultar Estadísticas / Reportes
```

---

*Fin del manual del Instructor.*
