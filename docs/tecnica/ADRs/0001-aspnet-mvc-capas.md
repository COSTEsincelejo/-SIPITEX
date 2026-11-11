# ADR 0001 — ASP.NET Core MVC con arquitectura por capas

- **Estado:** aceptada
- **Fecha:** 2026-06-28 (primer commit del proyecto: `c35d9e7`)

## Contexto

SIPITEX necesita una aplicación web con roles (Administrador, Instructor, Encargado de bodega), pantallas MVC y lógica de negocio separable del acceso a datos. El README fija metodología cascada y estructura Domain / Application / Infrastructure / Web.

## Decisión

Usar **ASP.NET Core MVC** (controllers + vistas Razor) sobre cuatro proyectos `Sipitex.Domain`, `Sipitex.Application`, `Sipitex.Infrastructure`, `Sipitex.Web`, con dependencias:

```
Web → Application → Domain
Web → Infrastructure → Application → Domain
```

El host registra `AddControllersWithViews`, `AddApplicationServices()` y `AddInfrastructure()` en `Program.cs`.

## Consecuencias

- La UI es server-rendered; no hay API JWT como capa principal (ver ADR 0008).
- Domain queda libre de NuGet; Infrastructure concentra EF, correo y PDF.
- Nuevos módulos se añaden como controllers + servicios Application + repositorios Infrastructure.
