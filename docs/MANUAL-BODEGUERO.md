# Manual del encargado de bodega

SIPITEX para el rol Encargado de bodega. El menú de stock es **Plantas de inventario**. Solo aparecen las plantas asignadas a su usuario. Si no tiene ninguna, la lista queda vacía: un administrador debe asignársela.

Este rol no importa Excel, no administra el catálogo de plantas, no ve Costeo de prendas y no administra usuarios.

## Plantas de inventario

1. Abra **Plantas de inventario**.
2. Elija su planta y pulse **Ver**, o use el nombre de la planta en el resumen.
3. Lea Insumos, OK, Bajo, Crítico y Sin mínimo.
4. Busque por nombre, código de almacén o referencia.
5. Filtre por categoría, por tipo de elemento (Consumible o Devolutivo) y por nivel. **Sin costo** muestra materiales cuyo promedio sigue en cero.
6. La fila muestra código de almacén, referencia, categoría, subcategoría y tipo.

Los niveles son: OK (hay stock y llega al mínimo), Bajo (hay stock pero no llega al mínimo), Crítico (hay mínimo y no hay existencias) y Sin mínimo definido (no es una falta). El aviso por correo usa el mismo criterio y nombra código de almacén, referencia, categoría y tipo.

La búsqueda del encabezado también encuentra un material por código de almacén, referencia o categoría.

## Ficha técnica de material

Es la ficha del insumo, no la receta de la prenda.

1. En la fila del material pulse **Ficha**.
2. La cabecera muestra código de almacén, referencia, categoría, subcategoría, tipo de elemento y proveedor.
3. Complete composición, gramaje, ancho, colores, acabados, uso, proceso, normas y el enlace al documento original.
4. Guarde. Si al importar quedó el aviso «ficha sin material único: completar a mano», el administrador o usted escriben aquí los datos en el material correcto.

## Solicitudes y consumo

1. **Solicitudes de materiales** es la cola de su planta. Abra el detalle.
2. En un pedido libre, elija el material del catálogo. La lista muestra nombre, código de almacén, referencia, categoría, tipo y stock. No apruebe más de lo que hay.
3. **Materiales de órdenes** entrega a producción lo que la orden ya pidió. La fila repite código de almacén, referencia, categoría y tipo, y muestra entregado, consumido y lo que todavía se puede consumir. El stock baja en esta entrega, una sola vez. El costo promedio del material en ese momento queda congelado en la línea. Las unidades que salen de stock sin costo entran a la orden con costo 0. No se entrega a una orden cerrada ni a una que siga pendiente de aprobación.
4. **Reingreso desde etapas** devuelve material o producto terminado desde Trazo, Corte, Confección, Control de Calidad o Terminado. No puede reingresar más de lo disponible en la etapa. Ese reingreso suma stock en la planta y todavía no descuenta lo disponible para consumir en la línea de la orden.
5. **Movimientos de stock** consulta entradas y salidas. La entrega a una orden deja una salida con referencia `Orden:` y el costo unitario de esa entrega. No edita el historial. Un ajuste se hace en el inventario de la planta y deja otra línea.
6. **Consumo de materiales** no está en este menú. Lo registra el instructor (o el administrador) sobre lo que usted ya entregó. Ese registro no vuelve a bajar el stock.

## Lo que no hace este rol

- No abre **Importar inventario**. El administrador carga el Excel y, antes de confirmar, respalda la base como indica [BACKUP.md](BACKUP.md).
- No calcula el costo de la prenda. Si un material quedó sin costo, el administrador lo ve en Costeo como «costo incompleto».
