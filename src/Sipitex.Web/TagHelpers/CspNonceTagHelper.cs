using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Sipitex.Web.Security;

namespace Sipitex.Web.TagHelpers;

// Pone el nonce en los <script> escritos en la vista. Los de type=application/json no se ejecutan.
[HtmlTargetElement("script")]
public sealed class CspNonceTagHelper : TagHelper
{
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (output.Attributes.ContainsName("src"))
            return;

        var tipo = output.Attributes["type"]?.Value?.ToString() ?? "";
        if (tipo.Length > 0
            && !tipo.Contains("javascript", StringComparison.OrdinalIgnoreCase)
            && !tipo.Equals("module", StringComparison.OrdinalIgnoreCase))
            return;

        var nonce = CspNonce.De(ViewContext.HttpContext);
        if (nonce.Length > 0)
            output.Attributes.SetAttribute("nonce", nonce);
    }
}
