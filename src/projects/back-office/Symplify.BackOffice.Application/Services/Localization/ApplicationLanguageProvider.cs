using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Services.Repositories;

namespace Symplify.BackOffice.Application.Services.Localization;

public sealed class ApplicationLanguageProvider : IApplicationLanguageProvider
{
    private readonly ILanguageRepository _languageRepository;

    public ApplicationLanguageProvider(ILanguageRepository languageRepository)
    {
        _languageRepository = languageRepository;
    }

    public async Task<IReadOnlyList<ApplicationLanguageDto>> GetActiveLanguagesAsync(
        CancellationToken cancellationToken = default)
    {
        List<ApplicationLanguageDto> languages = await _languageRepository
            .Query()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Order)
            .ThenBy(x => x.Name)
            .Select(x => new ApplicationLanguageDto
            {
                Id = x.Id,
                Culture = x.Culture,
                Name = x.Name,
                IsDefault = x.IsDefault,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

        return languages;
    }

    public async Task<ApplicationLanguageDto> GetDefaultLanguageAsync(
        CancellationToken cancellationToken = default)
    {
        ApplicationLanguageDto? language = await _languageRepository
            .Query()
            .Where(x => x.IsActive && x.IsDefault)
            .OrderBy(x => x.Order)
            .Select(x => new ApplicationLanguageDto
            {
                Id = x.Id,
                Culture = x.Culture,
                Name = x.Name,
                IsDefault = x.IsDefault,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (language is null)
            throw new InvalidOperationException("Default language is not configured in database.");

        return language;
    }

    public async Task<ApplicationLanguageDto?> GetByIdAsync(
        Guid languageId,
        CancellationToken cancellationToken = default)
    {
        if (languageId == Guid.Empty)
            return null;

        return await _languageRepository
            .Query()
            .Where(x => x.Id == languageId && x.IsActive)
            .Select(x => new ApplicationLanguageDto
            {
                Id = x.Id,
                Culture = x.Culture,
                Name = x.Name,
                IsDefault = x.IsDefault,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ApplicationLanguageDto?> GetByCultureAsync(
        string culture,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return null;

        string normalizedCulture = culture.Trim();

        return await _languageRepository
            .Query()
            .Where(x => x.IsActive && x.Culture == normalizedCulture)
            .Select(x => new ApplicationLanguageDto
            {
                Id = x.Id,
                Culture = x.Culture,
                Name = x.Name,
                IsDefault = x.IsDefault,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}