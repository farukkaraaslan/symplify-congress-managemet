using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.Congresses.Queries.GetForUpdate;

public class GetCongressForUpdateResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public int? EditionNumber { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public CongressStatus Status { get; set; }
    public string? ContactName { get; set; }
    public string? ContactTitle { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactAddress { get; set; }
    public string? VenueName { get; set; }
    public string? LogoLightPath { get; set; }
    public string? LogoDarkPath { get; set; }
    public string? LogoLightUrl { get; set; }
    public string? LogoDarkUrl { get; set; }
    public Guid? CountryId { get; set; }
    public Guid? CityId { get; set; }
    public Guid? StateId { get; set; }
    public List<GetCongressContactEmailForUpdateDto> ContactEmails { get; set; } = new();
    public List<LocalizedTranslationDto> Translations { get; set; } = new();
}
