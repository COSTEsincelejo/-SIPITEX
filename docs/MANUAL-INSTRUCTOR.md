# Manual del instructor

SIPITEX para el rol Instructor. Este manual cubre el flujo de materiales de una orden: la bodega entrega y usted registra el consumo.

## Lo que hace cada rol

- El **encargado de bodega** entrega los materiales en **Materiales de órdenes**. En esa entrega baja el stock, una sola vez, y queda congelado el costo promedio del material en ese momento.
- El **instructor** registra en **Consumo de materiales** cuánto de lo ya entregado se usó de verdad. Ese registro no vuelve a bajar el stock.
- El **administrador** puede hacer las dos cosas.

## Consumo de materiales

1. Abra **Consumo de materiales**.
2. Elija la orden. Al cambiarla, la lista de materiales se actualiza y muestra solo los de esa orden.
3. Si no hay entregas, la pantalla lo indica: primero el encargado de bodega debe entregar desde **Materiales de órdenes**.
4. Cada material muestra entregado, consumido y disponible para consumir.
5. Elija un material con disponible mayor que cero, escriba la cantidad y pulse **Registrar**.
6. No se puede consumir más de lo entregado menos lo ya consumido, ni un material que no está en la orden, ni en una orden finalizada o cancelada.

El costo unitario del consumo es el de la entrega. Si después cambia el costo del material en la planta, el consumo ya registrado no cambia. Si la entrega salió de stock sin costo, ese consumo queda en cero y **Costeo de prendas** lo marca como costo incompleto.

## Costeo de prendas

El costo final es solo materiales, sobre lo consumido:

(Σ cantidad consumida × costo unitario congelado en la entrega) / volumen × volumen de la orden.

La tarifa de hora del grupo de confección es referencia. No entra en el total.

Si hay materiales entregados y el consumo no cubre esa entrega, la pantalla avisa **Hay materiales entregados sin consumo registrado** y no presenta un total en cero como si fuera el costo real. Cuando ya hay algo consumido, muestra el costo de esa cantidad y conserva el aviso de lo que falta.

## Lo que no hace este rol

- No entrega materiales de la orden ni registra el reingreso desde etapas.
- No importa el inventario ni ajusta el stock de la planta.
