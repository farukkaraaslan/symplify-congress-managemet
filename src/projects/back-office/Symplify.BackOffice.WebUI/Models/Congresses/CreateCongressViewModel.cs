using Microsoft.AspNetCore.Mvc.Rendering;
using Symplify.BackOffice.Application.Features.Congresses.Cloning;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.WebUI.Models.Congresses;

public sealed class CreateCongressViewModel
{
    public Guid OrganizationId { get; set; }

    public int? EditionNumber { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public CongressStatus Status { get; set; } = CongressStatus.Draft;

    public string? ContactName { get; set; }

    public string? ContactTitle { get; set; }

    public string? ContactPhone { get; set; }

    /// <summary>
    /// Portal iletişim ve harita alanında gösterilecek açık adres.
    /// Online kongrelerde boş bırakılabilir.
    /// </summary>
    public string? ContactAddress { get; set; }

    /// <summary>
    /// Otel, üniversite veya kongre merkezi gibi fiziksel mekân adı.
    /// Online kongrelerde boş bırakılabilir.
    /// </summary>
    public string? VenueName { get; set; }

    public Guid? CountryId { get; set; }

    public Guid? StateId { get; set; }

    public List<CreateCongressContactEmailViewModel> ContactEmails { get; set; } = new();

    public bool CopyFromPreviousCongress { get; set; }

    public Guid? SourceCongressId { get; set; }

    public bool ShiftRelativeDates { get; set; } = true;

    public List<CongressCloneModule> CloneModules { get; set; } = new();

    public List<CreateCongressTranslationViewModel> Translations { get; set; } = new();

    public List<SelectListItem> OrganizationOptions { get; set; } = new();

    public List<SelectListItem> CountryOptions { get; set; } = new();

    public List<SelectListItem> StateOptions { get; set; } = new();

    public List<SelectListItem> StatusOptions { get; set; } = new();

    public List<CongressCloneSourceOptionViewModel> CloneSourceOptions { get; set; } = new();
}
