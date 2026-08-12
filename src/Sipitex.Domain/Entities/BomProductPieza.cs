namespace Sipitex.Domain.Entities;

// Pieza del patrón (descripción piezas del modelo) — Fase B
public class BomProductPieza
{
    public int Id { get; set; }
    public int BomProductId { get; set; }
    public BomProduct BomProduct { get; set; } = null!;

    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public string Tela { get; set; } = string.Empty;
    public int Orden { get; set; }
}
