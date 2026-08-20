# Manual de uso — Bodeguero

**SIPITEX** · Sistema Integrado de Aprendizaje Producción e Inventario Textil  
CMTC · SENA · ADSO

---

## 1. ¿Para quién es este manual?

Para el **Bodeguero**: controla **una** de las dos bodegas (Bodega 1 o Bodega 2), atiende las solicitudes de **esa** bodega, ajusta stock y consulta MRP. Su cuenta la crea el **Administrador**, quien **debe asignarle una bodega**.

Usted no ve el inventario ni las solicitudes de la otra bodega.

### Credencial de demostración

| Campo | Valor |
|-------|--------|
| Correo | `bodega@sipitex.test` |
| Contraseña | `Bodega123!` |

> Si al entrar ve *«Su cuenta no tiene una bodega asignada. Contacte al administrador.»*, pida al Admin que edite su usuario y elija **Bodega 1** o **Bodega 2**. Sin eso no puede listar inventario ni resolver solicitudes.

---

## 2. Cómo iniciar sesión

1. Abra SIPITEX en el navegador.  
2. En **Iniciar sesión**, escriba su correo y contraseña.  
3. Pulse **Entrar**.  
4. Llegará a **Inventario** (solo materiales de **su** bodega).

**Salir:** botón **Salir** (arriba a la derecha).

**Si olvidó la contraseña:** en el login use **¿Olvidaste tu contraseña?**.

---

## 3. Qué puede y qué no puede hacer

### Puede

| Módulo | Acciones |
|--------|----------|
| Inventario | Ver y registrar materiales **de su bodega**, ajustar stock y estado |
| Movimientos de stock | Historial de movimientos |
| Solicitudes de materiales | Resolver pedidos **de su bodega** (PorFicha e InsumosLibres) |
| Materiales de órdenes | Revisar materiales ligados a órdenes |
| Reingreso desde etapas | Devolver material desde el flujo de producción |
| Órdenes | Consultar (no crea órdenes) |
| MRP | Consultar BOM y **simular** requerimientos |
| Estadísticas / Reportes / Alertas | Consultar; configurar preferencias de alerta |
| Mi perfil | Foto, descripción de funciones y contraseña |

### No puede

- Ver o despachar solicitudes de la **otra** bodega.  
- Registrar un material en una bodega que no es la suya.  
- Crear órdenes de producción.  
- Entrar a **Fichas & producción**, **Mis solicitudes** de instructor ni **Control de calidad**.  
- Gestionar usuarios ni cambiarse de bodega (eso lo hace el Admin).

En el menú **no** le aparecen Fichas ni Calidad: es normal.

---

## 4. Mi perfil (importante al empezar)

1. Menú → **Mi perfil**, o clic en su **nombre / avatar**.  
2. Complete foto, descripción de funciones (ej.: despacho Bodega 2), nombre, correo y contraseña.  
3. **Guardar perfil**.

El rol y la **bodega** los asigna el Administrador; usted no los cambia en el perfil.

---

## 5. Inventario (solo su bodega)

**Menú → Inventario**

La tabla muestra únicamente materiales de **su** bodega, con la columna **Bodega**.

### 5.1 Agregar un material

1. Nombre, stock inicial, unidad y origen de entrada.  
2. El campo **Bodega** aparece **bloqueado** en su bodega (Bodega 1 o Bodega 2). No puede elegir la otra.  
3. **Agregar material**.  
4. La fila nueva muestra el nombre de **su** bodega (por ejemplo «Bodega 2»).

Si intenta enviar otra bodega, el sistema responde: *«Solo puede registrar materiales en su propia bodega.»*

### 5.2 Ajustar stock

En la fila: nuevo stock. Si **sube**, indique origen (compra, devolución u otra fuente autorizada).

### 5.3 Cambiar estado

**Bueno / Regular / Deteriorado** según la revisión física.

### 5.4 Stock bajo mínimo

Si un material de **su** bodega está bajo el mínimo, verá la alerta. Priorice reposición.

### 5.5 Movimientos

**Menú → Movimientos de stock** para el historial.

---

## 6. Solicitudes de materiales (cola de su bodega)

**Menú → Solicitudes de materiales**

Solo ve pedidos con **Bodega** = la suya. La tabla muestra la columna **Bodega** para confirmarlo.

- Filtro: **Solo pendientes** o **Todas**.  
- **Resolver** (pendiente) o **Ver** (ya resuelta).

### 6.1 Resolver una solicitud PorFicha

1. Abra el detalle: cabecera con Código, Estado, Fecha y **Bodega**.  
2. Para cada línea: cantidad a aprobar (no puede superar lo solicitado ni el stock de **su** catálogo).  
3. **Confirmar resolución**. El sistema descuenta stock y genera entrega.

Si la solicitud es de la otra bodega, la pantalla responde como si no existiera (no se lista ni se abre).

### 6.2 Resolver InsumosLibres

El instructor pidió por **descripción**. Usted:

1. Mapea cada línea a un material **de su bodega**, **o**  
2. Crea un material nuevo (nombre + unidad): nace con **la misma bodega de la solicitud**.  
3. Indica cantidad aprobada y confirma.

No mapee a un material de la otra bodega: el combo solo ofrece los de la suya.

---

## 7. Materiales de órdenes y reingreso

**Menú → Materiales de órdenes**

Revise y atienda materiales asociados a órdenes de producción (pendiente de revisión de bodega).

**Menú → Reingreso desde etapas**

Devuelva material al stock cuando una etapa de producción lo libera.

---

## 8. Órdenes de producción (solo consulta)

**Menú → Órdenes de producción**

Vea órdenes y avance para anticipar demanda. **No** crea órdenes.

---

## 9. MRP / Materiales

**Menú → MRP / Materiales**

1. Consulte el BOM.  
2. **Simular** producto y cantidad: requerido, disponible, déficit.  
3. Planifique entradas **en su bodega** si hay déficit.

---

## 10. Estadísticas, reportes y alertas

- **Estadísticas** y **Reportes** (PDF/Excel) para inventarios o auditorías.  
- **Alertas:** active stock bajo y solicitudes pendientes; **Guardar**.  
- Evaluar envíos masivos suele estar reservado al Administrador.

---

## 11. Rutina diaria sugerida (Bodeguero)

1. Iniciar sesión: confirmar que Inventario muestra **su** bodega.  
2. **Solicitudes de materiales** → pendientes de hoy.  
3. Resolver PorFicha / mapear InsumosLibres.  
4. Ajustar stock y estados según el conteo.  
5. Revisar **bajo mínimo**.  
6. **Materiales de órdenes** / **Reingreso** si hay movimiento de producción.  
7. **MRP → Simular** si hay órdenes grandes.  
8. Exportar reporte si lo piden.  
9. **Salir**.

---

## 12. Problemas frecuentes

| Situación | Qué hacer |
|-----------|-----------|
| «Su cuenta no tiene una bodega asignada» | El Admin debe asignarle Bodega 1 o Bodega 2 |
| No ve una solicitud que el instructor dice haber enviado | Esa solicitud es de la **otra** bodega |
| No ve Fichas ni Calidad | Correcto para el rol Bodeguero |
| No puede elegir otra bodega al agregar material | Correcto: solo registra en la suya |
| No puede aprobar una línea | Revise stock de **su** catálogo |
| No puede crear una orden | Solicite al Administrador |
| Olvidó la contraseña | Use **¿Olvidaste tu contraseña?** |

---

## 13. Flujo rápido de trabajo

```
Login Bodeguero (bodega asignada)
    → Inventario de SU bodega
    → Solicitudes de materiales de SU bodega
    → Resolver / mapear / descontar stock
    → Ajustar stock y estado
    → MRP / Reportes / Salir
```

### Relación con otros roles

| Quién | Qué hace | Usted (su bodega) |
|-------|----------|-------------------|
| Instructor | Elige bodega y solicita | Resuelve solo si el pedido es el suyo |
| Administrador | Asigna su bodega; ve ambas | Usted opera una |
| El otro bodeguero | Opera la otra bodega | No comparte cola ni catálogo |

---

*Fin del manual del Bodeguero.*
