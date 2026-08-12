using Sipitex.Domain;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

/// <summary>
/// StockLevel: Critico = Stock&lt;=0; Bajo = Stock&gt;0 &amp;&amp; Stock&lt;MinStock;
/// IsLowStock = Critico ∨ Bajo. Borde Stock=0/MinStock=0 → Critico.
/// </summary>
public class StockLevelRulesTests
{
    [Theory]
    [InlineData(0, 10, StockLevel.Critico)]
    [InlineData(-1, 10, StockLevel.Critico)]
    [InlineData(0, 0, StockLevel.Critico)] // borde: sin existencias siempre crítico
    [InlineData(5, 10, StockLevel.Bajo)]
    [InlineData(0.01, 10, StockLevel.Bajo)]
    [InlineData(10, 10, StockLevel.Ok)]
    [InlineData(15, 10, StockLevel.Ok)]
    [InlineData(5, 0, StockLevel.Ok)] // MinStock 0 y stock positivo → OK
    public void Resolve_MatchesBusinessRule(decimal stock, decimal minStock, StockLevel expected)
    {
        Assert.Equal(expected, StockLevelRules.Resolve(stock, minStock));
    }

    [Fact]
    public void IsLowStock_Equals_Critico_Or_Bajo_NoDuplicates()
    {
        var samples = new (decimal Stock, decimal Min)[]
        {
            (0, 10),
            (0, 0),
            (5, 10),
            (10, 10),
            (15, 10),
            (-2, 5),
            (1, 1),
            (0.5m, 1)
        };

        foreach (var (stock, min) in samples)
        {
            var level = StockLevelRules.Resolve(stock, min);
            var isLow = StockLevelRules.IsLowStock(stock, min);
            var fromParts = level is StockLevel.Critico or StockLevel.Bajo;
            Assert.Equal(fromParts, isLow);
        }
    }

    [Fact]
    public void Partition_CriticoPlusBajo_Equals_IsLowStock_Universe()
    {
        var materials = new (decimal Stock, decimal Min)[]
        {
            (0, 10),   // Critico
            (0, 0),    // Critico (borde)
            (3, 10),   // Bajo
            (9.9m, 10),// Bajo
            (10, 10),  // Ok
            (100, 10), // Ok
            (1, 0)     // Ok (MinStock 0)
        };

        var critico = materials.Count(m => StockLevelRules.IsCritical(m.Stock, m.Min));
        var bajo = materials.Count(m => StockLevelRules.IsBelowMinimum(m.Stock, m.Min));
        var isLow = materials.Count(m => StockLevelRules.IsLowStock(m.Stock, m.Min));

        Assert.Equal(2, critico);
        Assert.Equal(2, bajo);
        Assert.Equal(critico + bajo, isLow);
        Assert.Equal(4, isLow);
    }

    [Fact]
    public void StockZero_MinTen_IsCritico_NotBajo()
    {
        Assert.Equal(StockLevel.Critico, StockLevelRules.Resolve(0, 10));
        Assert.True(StockLevelRules.IsCritical(0, 10));
        Assert.False(StockLevelRules.IsBelowMinimum(0, 10));
        Assert.True(StockLevelRules.IsLowStock(0, 10));
    }

    [Fact]
    public void StockFive_MinTen_IsBajo()
    {
        Assert.Equal(StockLevel.Bajo, StockLevelRules.Resolve(5, 10));
        Assert.True(StockLevelRules.IsBelowMinimum(5, 10));
        Assert.False(StockLevelRules.IsCritical(5, 10));
        Assert.True(StockLevelRules.IsLowStock(5, 10));
    }

    [Fact]
    public void StockZero_MinZero_IsCritico_AndCountsAsIsLowStock()
    {
        // Documentado: sin existencias es crítico siempre, aunque MinStock=0
        // (antes 0 < 0 era false y no entraba en IsLowStock).
        Assert.Equal(StockLevel.Critico, StockLevelRules.Resolve(0, 0));
        Assert.True(StockLevelRules.IsLowStock(0, 0));
    }
}
