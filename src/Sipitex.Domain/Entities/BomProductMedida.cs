using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Fila de tabla de medidas (patrón o prenda terminada) — Fase B
public class BomProductMedida
{
    public int Id { get; set; }
    public int BomProductId { get; set; }
    public BomProduct BomProduct { get; set; } = null!;

    public BomMedidaTipo Tipo { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? Tolerancia { get; set; }
    public string? ComoMedir { get; set; }
    public int Orden { get; set; }

    public ICollection<BomProductMedidaValor> Valores { get; set; } = [];
}
