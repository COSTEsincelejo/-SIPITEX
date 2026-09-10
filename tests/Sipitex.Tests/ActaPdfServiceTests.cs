using Sipitex.Application.DTOs;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Reporting;

namespace Sipitex.Tests;

public class ActaPdfServiceTests
{
    [Fact]
    public void Render_GeneraPdfConCabeceraYFirmas()
    {
        var acta = new ActaMovimientoDto(
            1,
            "ACT-0001",
            ActaTipo.Egreso,
            ActaOrigen.Consumo,
            new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc),
            "Salida a confección",
            4,
            "OP-4",
            null,
            null,
            "Laura Gómez",
            "Instructor",
            new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc),
            "Pedro Encargado",
            "Encargado de bodega",
            new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc),
            7,
            "Ana",
            [
                new ActaDetalleDto(1, ActaItemTipo.Material, "Hilo", 5, "metro", 3, 11, null, 4)
            ]);

        var file = new ActaPdfService().Render(acta);

        Assert.Equal("application/pdf", file.ContentType);
        Assert.StartsWith("SIPITEX_ACT-0001_", file.FileName);
        Assert.True(file.Content.Length > 100);
        Assert.Equal((byte)'%', file.Content[0]);
        Assert.Equal((byte)'P', file.Content[1]);
        Assert.Equal((byte)'D', file.Content[2]);
        Assert.Equal((byte)'F', file.Content[3]);
    }
}
