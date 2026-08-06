namespace Sipitex.Domain.Entities;

// Valores permitidos para Ficha.Turno (lista cerrada)
public static class FichaTurnos
{
    public const string Manana = "Mañana";
    public const string Tarde = "Tarde";
    public const string Noche = "Noche";

    public static readonly string[] All =
    [
        Manana,
        Tarde,
        Noche
    ];

    public static bool IsValid(string? turno) =>
        !string.IsNullOrWhiteSpace(turno)
        && All.Any(t => string.Equals(t, turno.Trim(), StringComparison.Ordinal));
}
