using System.ComponentModel.DataAnnotations;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Web.Models;

public class MaterialFichaViewModel
{
    public int MaterialId { get; set; }
    public int PlantaId { get; set; }
    public string Planta { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string? CodigoAlmacen { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Referencia { get; set; }
    public string? Categoria { get; set; }
    public string? Subcategoria { get; set; }
    public string? Proveedor { get; set; }
    public TipoElemento TipoElemento { get; set; }
    public string Unidad { get; set; } = string.Empty;
    public bool TieneFicha { get; set; }

    [MaxLength(FichaTecnicaMaterial.DescripcionTecnicaMaxLength)]
    public string? DescripcionTecnica { get; set; }

    [MaxLength(FichaTecnicaMaterial.TipoMaterialMaxLength)]
    public string? TipoMaterial { get; set; }

    [MaxLength(FichaTecnicaMaterial.ComposicionMaxLength)]
    public string? Composicion { get; set; }

    public string? GramajeGm2 { get; set; }

    public string? AnchoCm { get; set; }

    [MaxLength(FichaTecnicaMaterial.ColoresMaxLength)]
    public string? Colores { get; set; }

    [MaxLength(FichaTecnicaMaterial.AcabadosMaxLength)]
    public string? Acabados { get; set; }

    [MaxLength(FichaTecnicaMaterial.EspecificacionesUsoMaxLength)]
    public string? EspecificacionesUso { get; set; }

    [MaxLength(FichaTecnicaMaterial.ProcesoFabricacionMaxLength)]
    public string? ProcesoFabricacion { get; set; }

    [MaxLength(FichaTecnicaMaterial.NormasCertificacionesMaxLength)]
    public string? NormasCertificaciones { get; set; }

    [MaxLength(FichaTecnicaMaterial.EnlaceFichaOriginalMaxLength)]
    public string? EnlaceFichaOriginal { get; set; }

    [MaxLength(FichaTecnicaMaterial.ObservacionesMaxLength)]
    public string? Observaciones { get; set; }

    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}
