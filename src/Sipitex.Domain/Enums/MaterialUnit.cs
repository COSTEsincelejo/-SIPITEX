namespace Sipitex.Domain.Enums;

// Unidades de medida del material. Los valores 0–3 se conservan para no romper
// filas ya persistidas (SQLite/PostgreSQL almacenan el enum como entero).
// UNI y UND se modelan aparte: no hay certeza de que sean intercambiables.
public enum MaterialUnit
{
    Metros = 0,     // metro (histórico)
    Unidades = 1,  // unidad genérica (histórico)
    Kg = 2,         // kilogramo (histórico)
    Gramos = 3,    // gramo (histórico)
    Galon = 4,
    Caja = 5,
    Uni = 6,        // unidad UNI
    Und = 7,        // unidad UND
    Docena = 8,
    Decena = 9,
    Litro = 10,
    Libra = 11,
    Yarda = 12
}
