namespace Core.Application.Localization;

public interface IApplicationLanguageProvider
{
    Task<ApplicationLanguageInfo?> GetDefaultAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApplicationLanguageInfo>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<ApplicationLanguageInfo?> GetByIdAsync(Guid languageId, CancellationToken cancellationToken = default);
}
