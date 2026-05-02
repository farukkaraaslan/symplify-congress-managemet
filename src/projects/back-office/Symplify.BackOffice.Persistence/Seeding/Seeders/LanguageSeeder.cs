using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Symplify.BackOffice.Domain.Localization;
using Symplify.BackOffice.Persistence.Contexts;
using Symplify.BackOffice.Persistence.Seeding.Definitions;

namespace Symplify.BackOffice.Persistence.Seeding.Seeders;

public sealed class LanguageSeeder
{
    private readonly BackOfficeDbContext _context;
    private readonly ILogger<LanguageSeeder> _logger;

    public LanguageSeeder(
        BackOfficeDbContext context,
        ILogger<LanguageSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedLanguagesAsync(cancellationToken);
    }

    private async Task SeedLanguagesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Language> seedLanguages = LanguageSeedDefinition.GetLanguages();

        if (seedLanguages.Count == 0)
            throw new InvalidOperationException("Language seed definition is empty.");

        foreach (Language seedLanguage in seedLanguages)
        {
            Language? existingLanguage = await _context.Languages
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    language => language.Id == seedLanguage.Id || language.Culture == seedLanguage.Culture,
                    cancellationToken);

            if (existingLanguage is null)
            {
                Language language = new()
                {
                    Id = seedLanguage.Id,
                    Name = seedLanguage.Name,
                    Culture = seedLanguage.Culture,
                    TwoLetterIsoCode = seedLanguage.TwoLetterIsoCode,
                    IsDefault = seedLanguage.IsDefault,
                    IsActive = seedLanguage.IsActive,
                    Order = seedLanguage.Order
                };

                await _context.Languages.AddAsync(language, cancellationToken);

                _logger.LogInformation("Language seed added: {Culture}", seedLanguage.Culture);
                continue;
            }

            existingLanguage.Name = seedLanguage.Name;
            existingLanguage.Culture = seedLanguage.Culture;
            existingLanguage.TwoLetterIsoCode = seedLanguage.TwoLetterIsoCode;
            existingLanguage.IsDefault = seedLanguage.IsDefault;
            existingLanguage.IsActive = seedLanguage.IsActive;
            existingLanguage.Order = seedLanguage.Order;

            _logger.LogInformation("Language seed updated: {Culture}", seedLanguage.Culture);
        }

        await _context.SaveChangesAsync(cancellationToken);

        await EnsureSingleDefaultLanguageAsync(cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSingleDefaultLanguageAsync(CancellationToken cancellationToken)
    {
        List<Language> languages = await _context.Languages
            .IgnoreQueryFilters()
            .Where(language => language.IsActive)
            .OrderBy(language => language.Order)
            .ToListAsync(cancellationToken);

        if (languages.Count == 0)
            throw new InvalidOperationException("At least one active language must be seeded.");

        Language defaultLanguage = languages.FirstOrDefault(language => language.IsDefault) ?? languages.First();

        foreach (Language language in languages)
            language.IsDefault = language.Id == defaultLanguage.Id;

        _logger.LogInformation("Default language resolved: {Culture}", defaultLanguage.Culture);
    }
}