# Manual del administrador

SIPITEX para el rol Administrador. El menú de inventario es **Plantas de inventario**. No hay una sección aparte llamada Inventario: `/Inventario` redirige a esa pantalla. Importar inventario queda en Operación y solo lo ve este rol.

## Plantas de inventario

1. Abra **Plantas de inventario**.
2. Elija una planta o deje **Todas las plantas** y pulse **Ver**.
3. El resumen muestra Insumos, OK, Bajo, Crítico y Sin mínimo.
4. Busque por nombre, código de almacén o referencia.
5. Filtre por categoría, por tipo de elemento (Consumible o Devolutivo) y por nivel. **Sin costo** deja los materiales cuyo promedio sigue en cero.
6. Cada fila muestra código de almacén, referencia, categoría, subcategoría y tipo. **Ficha** abre la ficha técnica del material.
7. **Administrar plantas** crea, renombra, desactiva o reactiva plantas. Planta de Inventario 1 no se elimina.

Los niveles son: OK (hay stock y llega al mínimo), Bajo (hay stock pero no llega al mínimo), Crítico (hay mínimo y no hay existencias) y Sin mínimo definido (no es una falta). El correo de alerta nombra el material con código de almacén, referencia, categoría y tipo.

## Cómo importar inventario desde Excel

Respalde la base antes de confirmar. El procedimiento está en [BACKUP.md](BACKUP.md), sección «Antes de importar inventario». La confirmación no se deshace desde la pantalla.

1. Abra **Importar inventario**.
2. Elija un `.xlsx` de hasta 5 MB. La hoja de materiales se llama `Inventario_Materiales`.
3. Columnas obligatorias: nombre, cantidad, unidad y ubicación. Si vienen, también se leen código de almacén, referencia, categoría, subcategoría, descripción, proveedor, tipo de elemento, stock mínimo y costo unitario (también «precio» o «valor unitario»).
4. Simule. Revise válidas, omitidas y advertencias. Material en proceso y producto terminado se omiten: se registran por órdenes y trazabilidad. Un duplicado en el archivo (mismo código, nombre y planta) se omite e indica las dos filas.
5. Tipo consumible o devolutivo; vacío queda Consumible. Caja y libra son unidades. `65,4` se guarda como 65.4. El mínimo vacío queda en 0.
6. Confirme. Con costo unitario mayor que cero, la entrada es una compra y el promedio no se diluye con stock anterior en cero. Costo vacío o cero deja el material en **Sin costo**.
7. La hoja opcional `Fichas_Tecnicas` se asocia solo si el código de almacén coincide con un único material. Si no, la advertencia es: `ficha sin material único: completar a mano`.

## Ficha técnica de material

No es la receta de la prenda (BOM). Esa receta se arma en MRP.

1. En **Plantas de inventario**, localice el material y pulse **Ficha**.
2. La cabecera muestra código interno, código de almacén, referencia, categoría, subcategoría, tipo de elemento, proveedor, unidad y planta.
3. Complete descripción técnica, tipo de material, composición, gramaje, ancho, colores, acabados, uso, proceso, normas y el enlace al documento original.
4. Guarde. Si el Excel no pudo asociar la ficha, escríbala aquí en el material correcto.

## Solicitudes, consumo y costeo

- **Solicitar insumos** pide una descripción libre y la planta de inventario destino. No exige el código de almacén.
- En **Solicitudes de materiales**, al resolver un pedido el material se elige por nombre, código de almacén, referencia, categoría y tipo.
- **Materiales de órdenes** entrega lo pedido por la orden y muestra esos mismos datos bajo el nombre.
- **Consumo de materiales** registra la cantidad usada. El costo unitario queda congelado con el promedio de ese momento. La lista muestra código de almacén, referencia, categoría y tipo.
- **Costeo de prendas** suma solo materiales. Si falta un costo, muestra «costo incompleto» en lugar del total. La tarifa de mano de obra es referencia y no entra en ese total.

## Usuarios y correo

Cree usuarios en **Usuarios**. El encargado de bodega necesita al menos una planta de inventario. En producción `Seed__DemoUsers` queda en `false`, `ADMIN_SEED_PASSWORD` se define en el panel (no en git) y el correo sale por Brevo. El detalle está en el README, sección Producción.
