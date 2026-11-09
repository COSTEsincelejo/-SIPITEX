# Pendiente: reingreso y consumo de la orden

El reingreso desde etapas (`ProductionFlowService.ApplyMaterialStageReentryAsync`, pantalla `PlantasInventarioOrdenes/Reingreso`) sigue como estaba.

Hoy hace esto:

- Comprueba la cantidad disponible en la etapa (`QuantityAvailable`), no la línea de materiales de la orden.
- Acepta cualquier material del catálogo, no solo los de la orden.
- Suma esa cantidad al stock y deja un movimiento de entrada `Orden:{id}/Etapa:{nombre}`, sin costo unitario.
- No toca `QuantityConsumed` ni `CostoEntregadoAcumulado`.

Con el arreglo de este cambio, lo disponible para consumir es `QuantityDelivered - QuantityConsumed`. El reingreso no entra en esa cuenta.

Para cerrarlo falta:

1. Una cantidad reingresada en la línea de la orden (o descontar `QuantityDelivered` sin borrar el historial de la entrega).
2. Lo disponible para consumir = entregado − consumido − reingresado.
3. No reingresar más de lo entregado y todavía no consumido.
4. Bajar `CostoEntregadoAcumulado` en la misma proporción de la cantidad que vuelve, y poner ese costo en el movimiento de entrada.
5. Ofrecer en la pantalla solo materiales de la orden, no todo el inventario.
6. Pruebas de etapa que hoy esperan el alta de stock sin mirar la línea (`StageReentryTests`).

Hasta entonces, un reingreso devuelve unidades al inventario y la línea de la orden sigue mostrando como consumible lo que ya volvió a la planta. El administrador debe revisarlo a mano si usa las dos pantallas sobre el mismo material.
