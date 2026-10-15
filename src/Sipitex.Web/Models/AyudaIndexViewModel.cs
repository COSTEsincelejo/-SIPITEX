using Sipitex.Application.Ayuda;

namespace Sipitex.Web.Models;

public sealed class AyudaIndexViewModel
{
    public required IReadOnlyList<TemaAyuda> Temas { get; init; }
    public required IReadOnlyList<TerminoGlosario> Glosario { get; init; }
    public required string Responsable { get; init; }
    public required string Correo { get; init; }
    public required IReadOnlyList<ManualAyudaItem> Manuales { get; init; }
}

public sealed record ManualAyudaItem(string Titulo, string Url);
