using Microsoft.AspNetCore.Mvc.Localization;

namespace Symplify.BackOffice.WebUI.Localization;

public interface IBackOfficeViewLocalizer : IViewLocalizer
{
    string GetStringValue(string key);
}
