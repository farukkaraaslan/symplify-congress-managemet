using System;
using System.Collections.Generic;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Persistence.Seeding.Definitions;

internal static class LanguageSeedDefinition
{
    internal static readonly Guid TurkishLanguageId = Guid.Parse("7f4f3e86-5f71-4e1c-9b4e-13c5c4a0a101");
    internal static readonly Guid EnglishLanguageId = Guid.Parse("2d9c49a1-0f8e-4e22-9f1d-5b9d45a1b202");
 
    internal static IReadOnlyList<Language> GetLanguages()
    {
        return new List<Language>
        {
            new()
            {
                Id = TurkishLanguageId,
                Name = "Türkçe",
                Culture = "tr-TR",
                TwoLetterIsoCode = "tr",
                IsDefault = true,
                IsActive = true,
                Order = 1
            },
            new()
            {
                Id = EnglishLanguageId,
                Name = "English",
                Culture = "en-US",
                TwoLetterIsoCode = "en",
                IsDefault = false,
                IsActive = true,
                Order = 2
            }
        };
    }
}