# Glosario — SIPITEX v1.0

Fuentes: `AyudaCatalogo.Glosario` (`src/Sipitex.Application/Ayuda/AyudaCatalogo.cs`), comentarios de dominio (extracción §5.2), IEEE 830 §1.3, HANDOVER (ambigüedades).

---

## Términos del centro de ayuda (`AyudaCatalogo`)

| Término | Definición |
|---------|------------|
| Planta de inventario | Lugar donde se guarda el stock. Planta de Inventario 1 es la planta por defecto y no se puede eliminar. |
| Bodega | En SIPITEX es la misma planta de inventario. El menú Plantas de inventario consulta el stock de esas plantas. |
| Orden de producción | Pedido para fabricar una cantidad de un producto que ya tiene ficha técnica, con fecha límite. |
| MRP | Cálculo de materiales necesarios a partir de la ficha técnica y de la cantidad a fabricar. |
| BOM | Receta de la prenda: materiales y cantidades por unidad. Se arma en MRP. No es la ficha técnica del material. |
| Ficha técnica de material | Datos del insumo (composición, gramaje, normas). Se abre con Ficha, en la fila del material. No cambia la receta de la prenda. |
| Stock OK | Hay existencias y alcanzan el mínimo definido para ese material. |
| Stock Bajo | Hay existencias, pero la cantidad es menor que el mínimo. |
| Stock Crítico | El material tiene mínimo definido y la existencia está en cero. |
| Sin mínimo definido | El material no tiene mínimo. No cuenta como falta ni dispara la misma alerta que Bajo o Crítico. |
| Reingreso | Devolución a la planta del material entregado a la orden que no se consumió. También se puede hacer con la orden finalizada o cancelada. El producto terminado vuelve en unidades de la etapa. |
| Acta de ingreso o egreso | Documento de entrada o salida con nombre, cargo, hora UTC y firma dibujada. Se crea con Nueva acta. |
| Solicitud de materiales | Pedido de insumos por descripción, o la entrega de materiales ya ligados a una orden. |

---

## Dominio / IEEE (complemento)

| Término | Definición / ubicación |
|---------|------------------------|
| Ficha (formación) | Ficha de formación del SENA (grupo/proceso del taller) — entidad `Ficha` |
| Ficha técnica (prenda / BOM) | Cabecera de ficha técnica (producto) — metadatos de la receta BOM — `BomProduct` |
| Ficha técnica de material | No es la ficha de la prenda (`BomProduct`) — `FichaTecnicaMaterial` |
| ConsumoMaterial | Ficha de consumo: lo usado en una orden. No descuenta stock (el descuento operativo ya ocurrió en la entrega) — `ConsumoMaterial` |
| MaterialRequest | Solicitud 1-ítem legacy a plantaInventario — sin uso de producto (HANDOVER #7) |
| FinishedGoodStock | Inventario de producto terminado (aparte de materiales/insumos) — `FinishedGoodStock` |
| BOM / Bill of Materials | Materiales necesarios por unidad de producto — IEEE 830 §1.3 |
| MRP / Material Requirements Planning | Cálculo de lo que falta comprar o pedir — IEEE 830 §1.3 |
| RF / RNF | Requisito funcional / no funcional |
| SRS | Especificación de requisitos de software — IEEE 830 |
| Encargado de bodega | Rol persistido `Encargado de bodega` (`UserRoles.EncargadoDeBodega`); UI de stock = plantas |
| Permiso extendido | Claim type `permiso` (`ExtendedPermissions`) otorgado por Administrador |
| Stock único | Regla post-#152: el consumo no vuelve a descontar el stock ya entregado a la orden |
| MES | Flujo de etapas de taller en la orden de producción (avance por etapas) |

---

## Ambigüedades conocidas (HANDOVER #4)

| Confusión | Clarificación operativa |
|-----------|-------------------------|
| «Ficha» | Puede ser grupo SENA, BOM de prenda o ficha técnica de material |
| «Bodega» | Rol / lenguaje histórico vs menú «Planta de inventario» |
| «Material» vs «insumo» | Usados de forma intercambiable en UI; mismo stock de planta |

Unificación de nomenclatura: **PENDIENTE** (deuda de producto).
