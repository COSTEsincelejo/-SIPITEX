using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Ledger inmutable de movimientos de stock de materiales (bodega)
public class StockMovement
{
    public int Id { get; set; }

    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;

    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;

    public int UsuarioId { get; set; }
    public User Usuario { get; set; } = null!;

    public StockMovementType TipoMovimiento { get; set; }

    // Origen de la entrada (compra/devolución/otra). Null en salidas, aprobaciones y ajustes a la baja.
    public StockEntryOrigin? Origen { get; set; }

    // Magnitud del movimiento (siempre >= 0)
    public decimal Cantidad { get; set; }

    // Stock del material inmediatamente después del movimiento
    public decimal StockResultante { get; set; }

    // Referencia opcional: "MaterialRequest:10", "Orden:5", "SolicitudMaterial:3"
    public string? Referencia { get; set; }
}
