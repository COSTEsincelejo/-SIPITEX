-- Auditoría de solo lectura. No modifica datos.
-- Lista órdenes en las que el mismo material tiene entrega y consumo,
-- y además una salida de stock por el consumo (referencia ConsumoMaterial:).
-- Eso es el doble descuento anterior a este arreglo: la entrega ya había
-- bajado el stock y el consumo lo volvió a bajar.
-- Revise el resultado a mano. No hay un UPDATE en este archivo.

SELECT
    o."Id" AS orden_id,
    o."OrderNumber" AS orden,
    m."Id" AS material_id,
    m."Name" AS material,
    req."QuantityDelivered" AS entregado_en_linea,
    COALESCE(c.cantidad_consumida, 0) AS cantidad_consumida,
    COALESCE(ent.cantidad_salida_entrega, 0) AS cantidad_salida_entrega,
    COALESCE(con.cantidad_salida_consumo, 0) AS cantidad_salida_consumo
FROM "ProductionOrders" o
JOIN "ProductionOrderMaterialRequirements" req
    ON req."ProductionOrderId" = o."Id"
JOIN "Materials" m
    ON m."Id" = req."MaterialId"
LEFT JOIN (
    SELECT "ProductionOrderId", "MaterialId", SUM("Cantidad") AS cantidad_consumida
    FROM "ConsumosMaterial"
    GROUP BY "ProductionOrderId", "MaterialId"
) c
    ON c."ProductionOrderId" = o."Id"
   AND c."MaterialId" = m."Id"
LEFT JOIN (
    SELECT
        split_part("Referencia", ':', 2)::integer AS orden_id,
        "MaterialId",
        SUM("Cantidad") AS cantidad_salida_entrega
    FROM "StockMovements"
    WHERE "TipoMovimiento" = 'Salida'
      AND "Referencia" ~ '^Orden:[0-9]+$'
    GROUP BY 1, 2
) ent
    ON ent.orden_id = o."Id"
   AND ent."MaterialId" = m."Id"
LEFT JOIN (
    SELECT
        split_part("Referencia", ':', 2) AS order_number,
        "MaterialId",
        SUM("Cantidad") AS cantidad_salida_consumo
    FROM "StockMovements"
    WHERE "TipoMovimiento" = 'Salida'
      AND "Referencia" LIKE 'ConsumoMaterial:%'
    GROUP BY 1, 2
) con
    ON con.order_number = o."OrderNumber"
   AND con."MaterialId" = m."Id"
WHERE req."QuantityDelivered" > 0
  AND COALESCE(c.cantidad_consumida, 0) > 0
  AND COALESCE(con.cantidad_salida_consumo, 0) > 0
ORDER BY o."Id", m."Name";
