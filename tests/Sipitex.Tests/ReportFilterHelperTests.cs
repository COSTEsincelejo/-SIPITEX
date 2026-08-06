using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class ReportFilterHelperTests
{
    [Fact]
    public void MatchesDate_DiaEspecifico_SoloEseDia()
    {
        var filter = new ReportFilterDto(Fecha: new DateOnly(2026, 3, 15));
        Assert.True(ReportFilterHelper.MatchesDate(new DateOnly(2026, 3, 15), filter));
        Assert.False(ReportFilterHelper.MatchesDate(new DateOnly(2026, 3, 16), filter));
    }

    [Fact]
    public void MatchesDate_MesAnio_FiltraPeriodo()
    {
        var filter = new ReportFilterDto(Mes: 3, Anio: 2026);
        Assert.True(ReportFilterHelper.MatchesDate(new DateOnly(2026, 3, 1), filter));
        Assert.False(ReportFilterHelper.MatchesDate(new DateOnly(2026, 4, 1), filter));
    }

    [Fact]
    public void MatchesFicha_PorInstructorYJornada()
    {
        var ficha = new Ficha
        {
            Id = 1,
            Turno = "Mañana",
            InstructorUserId = 10,
            Instructors = [new FichaInstructor { FichaId = 1, UserId = 10 }]
        };

        Assert.True(ReportFilterHelper.MatchesFicha(ficha, new ReportFilterDto(InstructorId: 10, Jornada: "Mañana")));
        Assert.False(ReportFilterHelper.MatchesFicha(ficha, new ReportFilterDto(InstructorId: 99)));
        Assert.False(ReportFilterHelper.MatchesFicha(ficha, new ReportFilterDto(Jornada: "Noche")));
    }

    [Fact]
    public void MatchingOrderIds_SinFiltroDeFicha_DevuelveVacio()
    {
        var ids = ReportFilterHelper.MatchingOrderIds(
            [new Ficha { Id = 1, ProductionOrderId = 5, Turno = "Tarde" }],
            new ReportFilterDto(Fecha: new DateOnly(2026, 1, 1)));

        Assert.Empty(ids);
        Assert.False(ReportFilterHelper.NeedsFichaScope(new ReportFilterDto(Fecha: new DateOnly(2026, 1, 1))));
    }
}
