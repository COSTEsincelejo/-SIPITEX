# Manuales de uso por rol — SIPITEX

Documentos prácticos para operar el **Sistema Integrado de Aprendizaje Producción e Inventario Textil**.

Hay **dos bodegas** (Bodega 1 y Bodega 2) en la misma base de datos. El Administrador asigna una bodega a cada Bodeguero. El Instructor elige la bodega al solicitar materiales. El Bodeguero solo ve y despacha lo de **su** bodega.

| Manual | Destinatario | Archivo |
|--------|--------------|---------|
| Manual del Administrador | Gestión del sistema, usuarios y vista consolidada | [Manual-Administrador.md](Manual-Administrador.md) |
| Manual del Instructor | Fichas, solicitudes y calidad | [Manual-Instructor.md](Manual-Instructor.md) |
| Manual del Bodeguero | Inventario, despachos y MRP de su bodega | [Manual-Bodeguero.md](Manual-Bodeguero.md) |

## Acceso de demostración

| Rol | Correo | Contraseña |
|-----|--------|------------|
| Administrador | `admin@sipitex.test` | `Admin123!` |
| Instructor | `instructor@sipitex.test` | `Instructor123!` |
| Bodeguero | `bodega@sipitex.test` | `Bodega123!` |

> El usuario demo **Bodeguero** nace sin bodega asignada. Antes de operar inventario o solicitudes, el Administrador debe editarlo y asignarle **Bodega 1** o **Bodega 2**.

## Cómo entrar al sistema

1. Abrir la URL de SIPITEX (por ejemplo `http://localhost:5240`).
2. Iniciar sesión con el correo y la contraseña de su rol.
3. Usar el **menú lateral**. **Mi perfil**: menú o clic en el nombre (arriba a la derecha).

Tras el login:

- **Administrador** y **Bodeguero** → Inventario.
- **Instructor** → Órdenes de producción (no tiene el inventario general).

Si olvida la contraseña: en el login use **¿Olvidaste tu contraseña?**.
