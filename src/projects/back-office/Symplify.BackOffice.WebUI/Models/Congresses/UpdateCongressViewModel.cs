using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.WebUI.Models.Congresses;

public sealed class UpdateCongressViewModel
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string? Slug { get; set; }

    public int? EditionNumber { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public CongressStatus Status { get; set; } = CongressStatus.Draft;

    public string? ContactName { get; set; }

    public string? ContactTitle { get; set; }

    public string? ContactPhone { get; set; }

    public string? ContactAddress { get; set; }

    public string? VenueName { get; set; }

    public string? ExistingLogoLightPath { get; set; }

    public string? ExistingLogoDarkPath { get; set; }

    public string? ExistingLogoLightUrl { get; set; }

    public string? ExistingLogoDarkUrl { get; set; }

    public IFormFile? LogoLightFile { get; set; }

    public IFormFile? LogoDarkFile { get; set; }

    public Guid? CountryId { get; set; }

    public Guid? StateId { get; set; }

    public List<CreateCongressContactEmailViewModel> ContactEmails { get; set; } = new();

    public List<UpdateCongressTranslationViewModel> Translations { get; set; } = new();

    public List<SelectListItem> OrganizationOptions { get; set; } = new();

    public List<SelectListItem> CountryOptions { get; set; } = new();

    public List<SelectListItem> StateOptions { get; set; } = new();

    public List<SelectListItem> StatusOptions { get; set; } = new();
}
