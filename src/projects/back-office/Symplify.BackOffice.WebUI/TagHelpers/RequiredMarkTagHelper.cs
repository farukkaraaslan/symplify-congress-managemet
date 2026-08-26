using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Symplify.BackOffice.WebUI.TagHelpers;

[HtmlTargetElement("required-mark")]
public sealed class RequiredMarkTagHelper : TagHelper
{
    public bool Visible { get; set; } = true;
    public string CssClass { get; set; } = "text-danger";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Visible)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", CssClass);
        output.Content.SetContent("*");
    }
}
