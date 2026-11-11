# Manual del instructor

SIPITEX para el rol Instructor. Trabaja con las órdenes, fichas y calidad de su alcance (órdenes asignadas por etapa MES o ficha). No administra el catálogo de plantas ni importa Excel.

## Órdenes de producción

1. Abra **Órdenes de producción**.
2. Vea solo las órdenes de las que es responsable (etapa asignada o ficha ligada).
3. Avance etapas MES según el flujo del taller. El estado de la prenda sigue la etapa.
4. Crear órdenes requiere el permiso extendido `Ordenes.Crear` (lo asigna el administrador). Sin ese permiso la pantalla de alta no está disponible.

## MRP / ficha técnica de la prenda

1. Consulte el BOM y, si tiene permiso `Mrp.Simular`, ejecute la simulación de requerimientos.
2. Crear o editar una ficha técnica (BOM) exige el permiso `Mrp.GestionarFichas`.
3. **Eliminar** un BOM solo lo puede hacer el administrador. Aunque tenga permiso de gestionar fichas, el borrado queda bloqueado.

## Fichas y producción

1. Abra **Fichas & producción**.
2. Registre la producción por ficha/grupo según lo asignado.
3. Use **Grupos de confección** para horas y prendas del grupo.
4. **Mis solicitudes** y **Solicitar insumos** piden material a la planta destino con descripción libre.

## Consumo de materiales

1. Abra **Consumo de materiales**.
2. Registre lo usado en una orden, hasta lo entregado menos lo ya consumido y lo reingresado.
3. El costo unitario queda congelado al promedio de ese momento.
4. No puede consumir en una orden finalizada o cancelada.

## Control de calidad

1. Abra **Control de calidad**.
2. Registre la inspección solo sobre órdenes a las que tiene acceso.
3. Clasificación **Bueno** con la orden en estado Calidad: la prenda avanza a **Producto terminado** (vía el ciclo de vida; no inventaría de nuevo el stock de PT).
4. Clasificación **Malo**: vuelve a **Confección** (con justificación).
5. Reproceso exige motivo y responsable.

## Actas y trazabilidad

Puede ver y crear **Actas de ingreso/egreso** y consultar **Trazabilidad** de las prendas del taller.

## Lo que no hace este rol

- No abre **Plantas de inventario** (consulta de stock consolidado). Los materiales se ven por MRP, solicitudes y materiales de órdenes.
- No aprueba solicitudes de material salvo que el administrador le asigne el permiso `Solicitudes.Aprobar`.
- No ve **Reingreso desde etapas** ni **Movimientos de stock**.
- No importa Excel ni administra usuarios ni plantas.
- No elimina BOM.
