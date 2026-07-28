# Fase 5 — Despliegue y mantenimiento (Cascada)

## 5.1 Publicación en intranet (Windows)

```powershell
cd src/Sipitex.Web
dotnet publish -c Release -o ./publish
```

Copiar carpeta `publish` al servidor IIS o ejecutar:

```powershell
./publish/Sipitex.Web.exe
```

## 5.2 Configuración

`appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=sipitex.db"
}
```

Para producción, usar ruta absoluta a la BD en el servidor.

## 5.3 IIS (opcional)

1. Instalar [ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/10.0)  
2. Crear sitio apuntando a `publish`  
3. Pool: **Sin código administrado**

## 5.4 Mantenimiento

| Tarea | Frecuencia |
|-------|------------|
| Respaldo `sipitex.db` | Diario |
| Revisión logs | Semanal |
| Actualización paquetes NuGet | Mensual |

## 5.5 Docker Compose (RNF07)

```bash
docker compose up --build
```

La aplicación queda en `http://localhost:8080` con SQLite persistente en el volumen `sipitex-data`.

## 5.6 Reportes y alertas

- **Reportes** (`/Reportes`): PDF (QuestPDF) y Excel (ClosedXML) de Inventario, Órdenes, Calidad y Dashboard.
- **Alertas** (`/Alertas`): cada actor activa/desactiva notificaciones (stock bajo, solicitudes pendientes, órdenes por vencer/atrasadas, reprocesos).
- Sin SMTP (`Email:Enabled=false`) los correos se guardan en `email-outbox/`.
- Con SMTP, configure `Email` en `appsettings.json` (`Host`, `User`, `Password`, `From`).

## 5.7 Base de datos y migraciones EF Core

El esquema se aplica con **migraciones EF Core** (`MigrateAsync` al arrancar), no con `EnsureCreated`.

```bash
# Crear una nueva migración (desarrollo)
dotnet ef migrations add NombreCambio \
  --project src/Sipitex.Infrastructure \
  --startup-project src/Sipitex.Web
```

- **Instalación limpia** (sin `sipitex.db`): al iniciar se crea el esquema completo y el seed de demo.
- **BD creada con el esquema anterior** (`EnsureCreated` / SQL manual, sin `__EFMigrationsHistory`): `MigrateAsync` **falla** porque `InitialCreate` intenta `CREATE TABLE` sobre tablas que ya existen.  
  Opciones pendientes de decisión: (A) borrar `sipitex.db` y regenerar, (B) *baseline* (marcar `InitialCreate` como aplicada sin ejecutarla si el esquema ya está completo), (C) migraciones incrementales/SQL de parche para columnas faltantes.

## 5.8 Roadmap post-MVP

- API REST para integraciones  
- Autenticación JWT para clientes externos  

## 5.9 Entregable de fase

Sistema operativo en intranet + manual de operación.
