namespace Sipitex.Domain.Entities;

// Ficha técnica del material (tela, hilo, insumo). No es la ficha de la prenda (BomProduct).
public class FichaTecnicaMaterial
{
    public const int DescripcionTecnicaMaxLength = 2000;
    public const int TipoMaterialMaxLength = 160;
    public const int ComposicionMaxLength = 500;
    public const int ColoresMaxLength = 400;
    public const int AcabadosMaxLength = 500;
    public const int EspecificacionesUsoMaxLength = 2000;
    public const int ProcesoFabricacionMaxLength = 2000;
    public const int NormasCertificacionesMaxLength = 500;
    public const int EnlaceFichaOriginalMaxLength = 500;
    public const int ObservacionesMaxLength = 1000;

    public int Id { get; set; }

    public int MaterialId { get; set; }

    public Material Material { get; set; } = null!;

    public string? DescripcionTecnica { get; set; }

    public string? TipoMaterial { get; set; }

    public string? Composicion { get; set; }

    public decimal? GramajeGm2 { get; set; }

    public decimal? AnchoCm { get; set; }

    public string? Colores { get; set; }

    public string? Acabados { get; set; }

    public string? EspecificacionesUso { get; set; }

    public string? ProcesoFabricacion { get; set; }

    public string? NormasCertificaciones { get; set; }

    public string? EnlaceFichaOriginal { get; set; }

    public string? Observaciones { get; set; }
}
