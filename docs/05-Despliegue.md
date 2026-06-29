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

## 5.5 Roadmap post-MVP

- Autenticación JWT y roles (RF01–RF03, RNF02–RNF03)  
- Migraciones EF Core formales  
- Docker Compose (RNF07)  
- API REST para integraciones  

## 5.6 Entregable de fase

Sistema operativo en intranet + manual de operación.
