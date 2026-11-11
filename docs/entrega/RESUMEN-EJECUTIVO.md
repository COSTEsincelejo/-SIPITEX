# Resumen ejecutivo — SIPITEX v1.0

**SIPITEX** es el sistema web del CMTC (SENA · ADSO) para aprendizaje, producción e inventario textil: órdenes/MES, MRP/BOM, plantas de inventario, solicitudes, calidad, actas, trazabilidad, reportes y alertas. Stack: ASP.NET Core MVC por capas, PostgreSQL 16, Docker/Render. Roles: Administrador, Instructor, Encargado de bodega.

**Entrega documental v1.0** (sin git tags). Línea temporal: primer commit **2026-06-28** → `main` **2026-11-11** `52e4170` (#162). Hitos: renombre Bodega→PlantaInventario (#85), SQLite→PostgreSQL (#97), stock único (#152), auditoría (#161), verificación (#162). Admin de producción: `admin@sipitex.local` (el correo del seed #95 es histórico y ya no aplica al arranque actual).

**Calidad (2026-11-11):** 768 tests OK (~19 s); cobertura líneas 87,06 % (branch 48,05 %). Requisitos: usar IEEE 830 como referencia alineada al código; `01-Requisitos`/seed difieren en IDs (p. ej. JWT vs cookies). **Criterios de calidad por tipo de prenda (RF16 seed):** Ausente.

**Fuera / deuda (HANDOVER):** MRP no automático al crear OP; producto terminado sin pantalla de consulta; cancelar OP/anular acta no revierte stock; nomenclatura mixta; privacidad institucional incompleta; no reactivar seed demo en producción. Transición: variables de entorno, Brevo, inventario inicial con respaldo, rotación de secretos — ver `PLAN-TRANSICION.md` y `HANDOVER.md`.
