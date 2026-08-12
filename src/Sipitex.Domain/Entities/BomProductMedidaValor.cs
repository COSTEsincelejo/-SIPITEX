namespace Sipitex.Domain.Entities;

// Valor numérico de una medida para una talla concreta — Fase B
// Unique (MedidaId, TallaId). Cascade al borrar talla o medida.
public class BomProductMedidaValor
{
    public int Id { get; set; }

    public int BomProductMedidaId { get; set; }
    public BomProductMedida Medida { get; set; } = null!;

    public int BomProductTallaId { get; set; }
    public BomProductTalla Talla { get; set; } = null!;

    // Nullable: celdas vacías en fichas reales
    public decimal? Valor { get; set; }
}
