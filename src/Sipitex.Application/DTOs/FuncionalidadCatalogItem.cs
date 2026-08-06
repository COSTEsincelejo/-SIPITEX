namespace Sipitex.Application.DTOs;

// Ítem del catálogo de funcionalidades del sistema (fuente en código, no BD)
public record FuncionalidadCatalogItem(
    string Modulo,
    string Funcionalidad,
    string Descripcion,
    string Rol);
