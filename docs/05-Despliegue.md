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

## 5.7 Roadmap post-MVP

- Migraciones EF Core formales  
- API REST para integraciones  
- Autenticación JWT para clientes externos  

## 5.8 Entregable de fase

Sistema operativo en intranet + manual de operación.
