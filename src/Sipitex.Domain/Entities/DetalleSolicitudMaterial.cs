using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Ítem de una SolicitudMaterial (cantidad pedida / aprobada por material)
public class DetalleSolicitudMaterial
{
    public int Id { get; set; }

    public int SolicitudMaterialId { get; set; }
    public SolicitudMaterial SolicitudMaterial { get; set; } = null!;

    // Null hasta que Bodega mapea (InsumosLibres); obligatorio al crear Tipo=PorFicha
    public int? MaterialId { get; set; }
    public Material? Material { get; set; }

    // Descripción del ítem pedida por Instructor (InsumosLibres)
    public string? DescripcionItem { get; set; }

    public decimal CantidadSolicitada { get; set; }

    // Null hasta que bodega resuelva el ítem
    public decimal? CantidadAprobada { get; set; }

    public DetalleSolicitudEstado EstadoItem { get; set; } = DetalleSolicitudEstado.Pendiente;
}
