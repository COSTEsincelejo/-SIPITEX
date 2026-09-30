using Sipitex.Web.Helpers;

namespace Sipitex.Tests;

public class BuildLabelTests
{
    [Fact]
    public void BuildLabel_SinCommit_EsLocal()
    {
        Assert.Equal("local", DisplayHelper.BuildLabel(""));
        Assert.Equal("local", DisplayHelper.BuildLabel("  "));
    }

    [Fact]
    public void BuildLabel_CommitLargo_QuedaEnSieteCaracteres()
    {
        Assert.Equal("9817db4", DisplayHelper.BuildLabel("9817db41ce3f55b8b70abb38c77c0e000c409c8f"));
    }

    [Fact]
    public void BuildLabel_CommitCorto_SeConserva()
    {
        Assert.Equal("abc1234", DisplayHelper.BuildLabel(" abc1234 "));
    }
}
