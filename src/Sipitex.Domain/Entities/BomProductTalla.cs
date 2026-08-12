namespace Sipitex.Domain.Entities;

// Talla asociada a una ficha técnica BOM (Fase A)
public class BomProductTalla
{
    public int Id { get; set; }
    public int BomProductId { get; set; }
    public BomProduct BomProduct { get; set; } = null!;

    // Nombre de talla (ej. "UNICA", "S/36", "8")
    public string Nombre { get; set; } = string.Empty;

    // Orden de aparición (S,M,L o 6,8,10…)
    public int Orden { get; set; }

    // Valores de medida asociados (Fase B); cascade al eliminar talla
    public ICollection<BomProductMedidaValor> MedidaValores { get; set; } = [];
}
