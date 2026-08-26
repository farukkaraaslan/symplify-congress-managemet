using Microsoft.AspNetCore.Mvc.Rendering;

namespace Symplify.BackOffice.WebUI.Models.Congresses;

public sealed class CongressesIndexViewModel
{
    public Guid? OrganizationId { get; set; }

    public string? OrganizationName { get; set; }

    public CongressStatusFilterViewModel StatusFilter { get; set; } = new();

    public List<SelectListItem> OrganizationOptions { get; set; } = new();

    public List<SelectListItem> StatusOptions { get; set; } = new();
}

public sealed class CongressStatusFilterViewModel
{
    public const int DefaultPublishedStatusValue = 2;

    public int Value { get; set; } = DefaultPublishedStatusValue;
}

