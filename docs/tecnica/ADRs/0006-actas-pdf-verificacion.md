# ADR 0006 — Actas PDF con firma y verificación pública

- **Estado:** aceptada
- **Fecha:** 2026-11-09 (`546a6a2` — verificación por QR y correo; PDF/firma: `1e853fd`, `f442e11`)

## Contexto

El centro necesita constancia de ingreso/egreso con conformidad (nombre, cargo, timestamp UTC, firma dibujada) y un medio de comprobar el documento fuera de sesión.

## Decisión

- Entidad `ActaMovimiento` (+ detalles); estados incluyen firma pendiente y anulación (solo Admin).
- PDF: `ActaPdfService` (QuestPDF Community + QRCoder) en Infrastructure.
- Verificación: `VerificarController.Acta` con `[AllowAnonymous]` y rate limit `verificar-acta` (30/min por IP) en `Program.cs`.
- Envío por correo del acta (mismo commit de verificación QR).

## Consecuencias

- El pie institucional del PDF usa `PrivacidadOptions` (vacío/`PENDIENTE` no se publica).
- Anular acta no revierte stock (HANDOVER #3).
- URL de verificación depende de `App__BaseUrl` / URL pública.
