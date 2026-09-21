# Manual de uso — Bodeguero

**SIPITEX** · Sistema Integrado de Producción e Inventario Textil  
CMTC · SENA · ADSO

---

## 1. ¿Para quién es este manual?

Para el **Bodeguero**: controla materias primas, atiende solicitudes de material, ajusta stock y consulta MRP. Su cuenta la crea el **Administrador**.

### Credencial de demostración

| Campo | Valor |
|-------|--------|
| Correo | `bodega@sipitex.test` |
| Contraseña | `Bodega123!` |

---

## 2. Cómo iniciar sesión

1. Abra SIPITEX en el navegador.  
2. En **Iniciar sesión**, escriba su correo y contraseña.  
3. Pulse **Entrar**.  
4. Llegará a **Inventario** (su pantalla principal).

**Salir:** botón **Salir** (arriba a la derecha).

**Si olvidó la contraseña:** en el login use **¿Olvidaste tu contraseña?**.

---

## 3. Qué puede y qué no puede hacer

### Puede

| Módulo | Acciones |
|--------|----------|
| Inventario | Agregar materiales, ajustar stock, cambiar estado, **aprobar / rechazar** solicitudes |
| Órdenes | Consultar órdenes y avance (no crea órdenes) |
| MRP | Consultar BOM y **simular** requerimientos |
| Estadísticas / Reportes / Alertas | Consultar; configurar preferencias de alerta |
| Mi perfil | Foto, descripción de funciones y contraseña |

### No puede

- Crear órdenes de producción (Administrador).  
- Solicitar material como Instructor (esa acción es de Admin/Instructor).  
- Entrar a **Fichas & producción** ni **Control de calidad**.  
- Gestionar usuarios.  

En el menú **no** le aparecen Fichas ni Calidad: es normal.

---

## 4. Mi perfil (importante al empezar)

1. Menú → **Mi perfil**, o clic en su **nombre / avatar**.  
2. Complete:
   - **Foto** (JPG, PNG o WEBP, máx. 2 MB).  
   - **Descripción de mis funciones** (ej.: despacho, conteos, control de stock).  
   - Nombre, correo y nueva contraseña si aplica.  
3. **Guardar perfil**.

El rol **Bodeguero** lo asigna el Administrador.

---

## 5. Inventario (módulo principal)

**Menú → Inventario**

### 5.1 Agregar un material

1. Escriba el nombre del material.  
2. Indique stock inicial y unidad (metros, unidades, etc.).  
3. Pulse **Agregar material**.

### 5.2 Ajustar stock

1. En la fila del material, escriba el nuevo stock.  
2. Confirme el ajuste (conteo físico, entrada o corrección).

### 5.3 Cambiar estado del material

Seleccione el estado físico:

- **Bueno**  
- **Regular**  
- **Deteriorado**  

Úselo cuando revise la calidad del material en bodega.

### 5.4 Atender solicitudes (despacho)

Las solicitudes las crean Instructores o el Administrador.

1. En la tabla de solicitudes busque las **Pendientes**.  
2. Verifique que haya stock suficiente.  
3. Elija:
   - **Aprobar** → el sistema **descuenta** el stock y marca la solicitud como aprobada/despachada.  
   - **Rechazar** → la solicitud queda rechazada (sin descontar).  

Si no hay stock, el sistema mostrará error al aprobar: ajuste inventario o rechace e informe.

### 5.5 Stock bajo mínimo

Si un material está bajo el mínimo, verá una alerta. Priorice reposición o avise al Administrador.

---

## 6. Órdenes de producción (solo consulta)

**Menú → Órdenes de producción**

- Vea órdenes, producto, cantidades y avance.  
- Le ayuda a anticipar qué materiales se van a necesitar.  
- **No** puede crear órdenes ni usar el avance +10u (eso es Admin/Instructor).

---

## 7. MRP / Materiales

**Menú → MRP / Materiales**

### Consultar BOM

Revise cuánto material exige cada producto (por ejemplo Camisa o Pantalón) por unidad.

### Simular requerimientos

1. Elija producto y cantidad.  
2. Ejecute la **simulación**.  
3. Analice:
   - **Requerido**  
   - **Disponible**  
   - **Déficit**  

Si hay déficit, planifique compras o entradas de stock antes de que se acumulen solicitudes.

---

## 8. Estadísticas, reportes y alertas

### Estadísticas

Consulte indicadores globales (útil para reuniones de bodega / producción).

### Reportes

Exporte **PDF** o **Excel** de Inventario (y otros reportes disponibles) para inventarios o auditorías.

### Alertas

1. En **Alertas**, active por ejemplo:
   - Stock bajo mínimo  
   - Solicitudes pendientes  
2. **Guardar**.  
3. Así recibe avisos cuando deba reponer o despachar.

> Evaluar / enviar alertas de forma masiva suele estar reservado al Administrador (o a quien tenga permiso extendido).

---

## 9. Rutina diaria sugerida (Bodeguero)

1. Iniciar sesión y revisar **Inventario**.  
2. Atender solicitudes **Pendientes** (aprobar o rechazar).  
3. Ajustar stock y estados según el conteo del día.  
4. Revisar materiales **bajo mínimo**.  
5. Usar **MRP → Simular** si hay órdenes grandes próximas.  
6. Exportar un **Reporte de Inventario** si lo piden.  
7. Mantener actualizado **Mi perfil**.  
8. **Salir**.

---

## 10. Problemas frecuentes

| Situación | Qué hacer |
|-----------|-----------|
| No ve Fichas ni Calidad | Correcto: esos módulos no son del rol Bodeguero |
| No puede aprobar una solicitud | Verifique stock; si falta, ajuste inventario o rechace |
| No puede crear una orden | Solicite al Administrador |
| No puede solicitar material | Esa acción es de Instructor/Admin; usted despacha |
| Olvidó la contraseña | Use **¿Olvidaste tu contraseña?** |
| Acceso denegado | Está intentando una función de otro rol |

---

## 11. Flujo rápido de trabajo

```
Login Bodeguero
    → Revisar Inventario y alertas de stock
    → Aprobar / rechazar solicitudes pendientes
    → Ajustar stock y estado de materiales
    → Simular MRP si hay órdenes grandes
    → Reportes / Salir
```

### Relación con otros roles

| Quién | Qué hace | Usted (Bodega) |
|-------|----------|----------------|
| Instructor / Admin | Solicita material | Aprueba o rechaza |
| Administrador | Crea órdenes y usuarios | Consulta y abastece |
| Instructor | Produce en fichas | No interviene en fichas |

---

*Fin del manual del Bodeguero.*
