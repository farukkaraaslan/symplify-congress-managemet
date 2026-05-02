namespace Symplify.BackOffice.WebUI.Services.Localization;

public interface IBackOfficeCultureResolver
{
    Task<string> ResolveCurrentCultureAsync(CancellationToken cancellationToken = default);

    Task<Guid?> ResolveCurrentLanguageIdAsync(CancellationToken cancellationToken = default);

    Task<string> NormalizeCultureAsync(
        string? culture,
        CancellationToken cancellationToken = default);
}