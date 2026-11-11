# Guía: cargar inventario inicial por planta

Pasos cortos para cargar stock real sobre materiales CMTC (u otros) desde Excel.

## 1. Preparar el archivo

1. En **Importar inventario** (solo Administrador), descargue la plantilla Excel.
2. Complete la hoja `Inventario_Materiales`. Columnas obligatorias: **nombre**, **cantidad**, **unidad**, **ubicación**.
3. En **Ubicación** use el nombre exacto de la planta (ej. `Planta de Inventario 1`).
4. Use los **nombres exactos** de los materiales CMTC si ya existen en el catálogo (seed con Stock=0). Así el import **actualiza** el stock en lugar de crear duplicados.
5. Opcional: costo unitario, stock mínimo, categoría, fecha de ingreso.

## 2. Simular

1. Suba el `.xlsx` y pulse **Simular**.
2. Revise filas válidas (alta o «Actualiza stock»), errores por fila y omitidas.
3. Filas con error no detienen el resto: corríjalas y vuelva a simular si hace falta.
4. Si el stock del archivo coincide con el de la base, la fila se **omite** (reimport idempotente).

## 3. Confirmar

1. Pulse **Confirmar**. Se guarda en una sola transacción: altas, actualizaciones de stock (movimiento Entrada/Ajuste) y fichas técnicas si vienen en el libro.
2. Verifique el stock en **Plantas de inventario** → la planta indicada.

## Idempotencia

- Misma clave `código | nombre | planta`: no se duplica el material.
- Si ya existe y la cantidad del archivo **difiere**, se ajusta el stock al valor del archivo y se registra el movimiento.
- Si la cantidad **coincide**, se omite. Un segundo import del mismo archivo no vuelve a sumar stock.
