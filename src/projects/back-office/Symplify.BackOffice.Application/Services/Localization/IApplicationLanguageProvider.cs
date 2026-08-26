namespace Symplify.BackOffice.Application.Services.Localization;

public interface IApplicationLanguageProvider
{
    Task<IReadOnlyList<ApplicationLanguageDto>> GetActiveLanguagesAsync(
        CancellationToken cancellationToken = default);

    Task<ApplicationLanguageDto> GetDefaultLanguageAsync(
        CancellationToken cancellationToken = default);

    Task<ApplicationLanguageDto?> GetByIdAsync(
        Guid languageId,
        CancellationToken cancellationToken = default);

    Task<ApplicationLanguageDto?> GetByCultureAsync(
        string culture,
        CancellationToken cancellationToken = default);
}