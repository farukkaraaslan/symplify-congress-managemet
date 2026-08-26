namespace Symplify.BackOffice.WebUI.Localization;

public sealed class BackOfficeLanguageOption
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Culture { get; set; } = string.Empty;

    public string TwoLetterIsoCode { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; }

    public int Order { get; set; }
}
