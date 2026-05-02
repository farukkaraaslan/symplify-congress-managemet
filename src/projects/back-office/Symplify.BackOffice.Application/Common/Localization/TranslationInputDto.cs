namespace Symplify.BackOffice.Application.Common.Localization;
public sealed class TranslationInputDto
{
    public Guid LanguageId { get; set; }
    public Dictionary<string, string?> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
