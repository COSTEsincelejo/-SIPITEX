using Sipitex.Application.Helpers;

namespace Sipitex.Tests;

public class AuthCodeHelperTests
{
    [Fact]
    public void Generate_SixDigits()
    {
        var code = AuthCodeHelper.Generate();
        Assert.Equal(6, code.Length);
        Assert.Matches(@"^\d{6}$", code);
    }

    [Fact]
    public void Normalize_StripsSpacesAndRejectsWrongLength()
    {
        Assert.Equal("123456", AuthCodeHelper.Normalize("123 456"));
        Assert.Equal(string.Empty, AuthCodeHelper.Normalize("12345"));
        Assert.Equal(string.Empty, AuthCodeHelper.Normalize("abcdef"));
    }

    [Fact]
    public void Hash_IsStableAndNotPlaintext()
    {
        var hash = AuthCodeHelper.Hash("123456");
        Assert.Equal(AuthCodeHelper.Hash("123 456"), hash);
        Assert.DoesNotContain("123456", hash, StringComparison.Ordinal);
        Assert.Equal(64, hash.Length);
    }
}

public class EmailFormatTests
{
    [Theory]
    [InlineData("user@sipitex.test", true)]
    [InlineData("a@b.co", true)]
    [InlineData("no-arroba", false)]
    [InlineData("a@b", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_SimpleFilter(string? email, bool expected) =>
        Assert.Equal(expected, EmailFormat.IsValid(email));
}
