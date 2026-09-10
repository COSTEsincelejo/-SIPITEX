using Sipitex.Application.Services;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class ProductStateMachineTests
{
    private readonly ProductStateMachine _sut = new();

    [Theory]
    [InlineData(EstadoProducto.MateriaPrima, EstadoProducto.Corte, false, true)]
    [InlineData(EstadoProducto.Corte, EstadoProducto.Confeccion, false, true)]
    [InlineData(EstadoProducto.Confeccion, EstadoProducto.Calidad, false, true)]
    [InlineData(EstadoProducto.Calidad, EstadoProducto.ProductoTerminado, false, true)]
    [InlineData(EstadoProducto.ProductoTerminado, EstadoProducto.VentaEntrega, false, true)]
    public void ForwardOneStep_Allowed(EstadoProducto from, EstadoProducto to, bool just, bool expected)
    {
        Assert.Equal(expected, _sut.CanTransition(from, to, just, out _));
    }

    [Fact]
    public void SkipAhead_Fails()
    {
        Assert.False(_sut.CanTransition(EstadoProducto.MateriaPrima, EstadoProducto.Calidad, false, out var error));
        Assert.Contains("saltar", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BackwardWithoutJustification_Fails()
    {
        Assert.False(_sut.CanTransition(EstadoProducto.Calidad, EstadoProducto.Confeccion, false, out var error));
        Assert.Contains("justificación", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BackwardCalidadToConfeccion_WithJustification_Allowed()
    {
        Assert.True(_sut.CanTransition(EstadoProducto.Calidad, EstadoProducto.Confeccion, true, out var error));
        Assert.Null(error);
    }

    [Fact]
    public void SameState_Fails()
    {
        Assert.False(_sut.CanTransition(EstadoProducto.Corte, EstadoProducto.Corte, false, out _));
    }
}
