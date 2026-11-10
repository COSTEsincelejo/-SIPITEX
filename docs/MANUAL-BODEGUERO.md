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
3. **Materiales de órdenes** entrega a producción lo que la orden ya pidió. La fila repite código de almacén, referencia, categoría y tipo. No se entrega a una orden cerrada ni a una que siga pendiente de aprobación.
4. **Consumo de materiales** registra lo usado en una orden, y solo hasta lo entregado que todavía no se consumió ni se reingresó. El costo unitario queda congelado. La fila muestra código de almacén, referencia, categoría y tipo cuando el material los tiene.
5. **Reingreso desde etapas** devuelve a la planta el sobrante: material entregado a la orden que no se consumió. La pantalla lista solo los materiales de esa orden, con entregado, consumido, reingresado y disponible. No se puede devolver más de ese disponible. El reingreso de material también se hace en una orden finalizada o cancelada. El producto terminado sigue limitado a las unidades enteras disponibles en la etapa (Trazo, Corte, Confección, Control de Calidad o Terminado) y no cambia el stock de insumos. El instructor no ve esta pantalla.
6. **Movimientos de stock** consulta entradas y salidas. No edita el historial. Un ajuste se hace en el inventario de la planta y deja otra línea.

## Lo que no hace este rol

- No abre **Importar inventario**. El administrador carga el Excel y, antes de confirmar, respalda la base como indica [BACKUP.md](BACKUP.md).
- No calcula el costo de la prenda. Si un material quedó sin costo, el administrador lo ve en Costeo como «costo incompleto».
