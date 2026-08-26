using Microsoft.AspNetCore.Mvc;

namespace Symplify.BackOffice.WebUI.ViewComponents;

public sealed class NavbarCongressSelectorViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync()
    {
        return Task.FromResult<IViewComponentResult>(Content(string.Empty));
    }
}
