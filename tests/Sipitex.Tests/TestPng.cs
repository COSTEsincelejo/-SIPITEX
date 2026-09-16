using Sipitex.Application.Helpers;

namespace Sipitex.Tests;

internal static class TestPng
{
    // PNG 1×1 válido para actas / PDF
    public static byte[] Bytes { get; } = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    public static string DataUrl => SignatureImage.ToDataUrl(Bytes)!;
}
