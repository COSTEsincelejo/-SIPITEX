using Sipitex.Application.Helpers;

namespace Sipitex.Tests;

public class SignatureImageTests
{
    [Fact]
    public void FromDataUrl_PngValido_DevuelveBytes()
    {
        var parsed = SignatureImage.FromDataUrl(TestPng.DataUrl);

        Assert.NotNull(parsed);
        Assert.True(SignatureImage.IsPng(parsed));
    }

    [Fact]
    public void FromDataUrl_JpegOTexto_Nulo()
    {
        Assert.Null(SignatureImage.FromDataUrl("data:image/jpeg;base64,AAAA"));
        Assert.Null(SignatureImage.FromDataUrl("no-es-una-firma"));
        Assert.Null(SignatureImage.FromDataUrl(null));
    }

    [Fact]
    public void IsPng_RechazaVacioYDemasiadoGrande()
    {
        Assert.False(SignatureImage.IsPng(null));
        Assert.False(SignatureImage.IsPng([1, 2, 3]));
        var huge = new byte[SignatureImage.MaxBytes + 8];
        Buffer.BlockCopy(TestPng.Bytes, 0, huge, 0, TestPng.Bytes.Length);
        Assert.False(SignatureImage.IsPng(huge));
    }
}
