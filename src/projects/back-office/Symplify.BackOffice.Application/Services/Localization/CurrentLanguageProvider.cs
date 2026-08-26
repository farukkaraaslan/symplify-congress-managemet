namespace Symplify.BackOffice.Application.Services.Localization;

public sealed class CurrentLanguageProvider : ICurrentLanguageProvider
{
    private readonly ICurrentCultureProvider _currentCultureProvider;
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;

    public CurrentLanguageProvider(
        ICurrentCultureProvider currentCultureProvider,
        IApplicationLanguageProvider applicationLanguageProvider)
    {
        _currentCultureProvider = currentCultureProvider;
        _applicationLanguageProvider = applicationLanguageProvider;
    }

    public async Task<ApplicationLanguageDto> GetCurrentLanguageAsync(
        CancellationToken cancellationToken = default)
    {
        string? currentCulture = _currentCultureProvider.GetCurrentCulture();

        if (!string.IsNullOrWhiteSpace(currentCulture))
        {
            ApplicationLanguageDto? language =
                await _applicationLanguageProvider.GetByCultureAsync(
                    currentCulture,
                    cancellationToken);

            if (language is not null)
                return language;
        }

        return await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);
    }
}