# Fase 4 — Pruebas (Cascada)

## 4.1 Plan de pruebas funcionales

| # | Caso | Pasos | Resultado esperado |
|---|------|-------|-------------------|
| T01 | Agregar material | Inventario → nombre + stock → Agregar | Aparece en tabla |
| T02 | Alerta stock bajo | Ajustar stock por debajo del mínimo | Alerta amarilla |
| T03 | Crear solicitud | Orden + material + cantidad → Solicitar | Estado Pendiente |
| T04 | Aprobar solicitud | Aprobar con stock suficiente | Stock descontado |
| T05 | Crear orden | Camisa, 50 uds, fecha | Nueva fila OP-xxx |
| T06 | Producción +10 | Botón +10u en orden | Avance y consumo BOM |
| T07 | Simular MRP | Camisa 50 uds | Líneas OK o déficit |
| T08 | Registrar ficha | Ficha + unidades | Avance de orden |
| T09 | Inspección calidad | Orden + unidades + resultado | Registro en tabla |
| T10 | KPIs | Estadísticas | Valores coherentes con datos |

## 4.2 Pruebas de integración (manual)

```powershell
dotnet build
cd src/Sipitex.Web
dotnet run
```

Verificar cada ruta del menú lateral sin errores 500.

## 4.3 Criterios de aceptación

- Compilación sin errores  
- Persistencia SQLite entre reinicios  
- UI equivalente al prototipo HTML original  
- Separación clara de capas (sin EF en controllers)

## 4.4 Entregable de fase

Acta de pruebas → despliegue (Fase 5).
