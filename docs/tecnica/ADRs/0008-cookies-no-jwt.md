# ADR 0008 — Autenticación por cookies (no JWT)

- **Estado:** aceptada
- **Fecha:** 2026-07-10 (`5f6512d` — autenticación de usuarios con roles; endurecimiento `d76c3fb` 2026-10-14; keys en PG `50eb1fe` 2026-09-30)

## Contexto

`docs/01-Requisitos.md` RF02 / RNF02 hablan de **JWT**. El seed (`DbInitializer`) y `docs/08-IEEE830-Especificacion.md` describen login con **cookies**. El código implementa cookies.

## Decisión

- `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(...)` en `Program.cs`.
- Cookie `sipitex.auth` (HttpOnly, SameSite=Lax; Secure=Always fuera de Development/tests).
- `ExpireTimeSpan` = `SeguridadOptions.MinutosEfectivos()` (default **20** minutos, sliding); no hay “8 horas” en el código de sesión (EXTRACCION).
- Comentario explícito: *«Login con cookies, no JWT ni nada raro.»*
- Claves Data Protection persistidas en PostgreSQL (`PersistKeysToDbContext<SipitexDbContext>`).

## Consecuencias

- Discrepancia documental: RF02 en `01-Requisitos` queda desalineado respecto a implementación / IEEE830 / seed.
- No hay emisión ni validación de Bearer JWT en el pipeline.
- Sesiones sobreviven redeploys en Render mientras la BD conserve `DataProtectionKeys`.
