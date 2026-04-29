using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.CrossCuttingConcerns.Localization;

public class TranslateLocalizationManager : ILocalizationService
{
    private const string _defaultLocale = "tr";
    public ICollection<string>? AcceptLocales { get; set; }

    private readonly ITranslationService _translationService;

    public TranslateLocalizationManager(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    public Task<string> GetLocalizedAsync(string key, string? keySection = null)
    {
        return GetLocalizedAsync(key, AcceptLocales ?? throw new NoNullAllowedException(nameof(AcceptLocales)));
    }

    public async Task<string> GetLocalizedAsync(string key, ICollection<string> acceptLocales, string? keySection = null)
    {
        string? localization;

        if (acceptLocales is not null)
            foreach (string locale in acceptLocales)
            {
                localization = await _translationService.TranslateAsync(key, locale);
                if (!string.IsNullOrWhiteSpace(localization))
                    return localization;
            }

        localization = await _translationService.TranslateAsync(key, _defaultLocale);
        if (!string.IsNullOrWhiteSpace(localization))
            return localization;

        return key;
    }
}
