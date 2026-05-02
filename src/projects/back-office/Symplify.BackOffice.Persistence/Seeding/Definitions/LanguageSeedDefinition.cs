using System;
using System.Collections.Generic;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Persistence.Seeding.Definitions;

internal static class LanguageSeedDefinition
{
    internal static readonly Guid TurkishLanguageId = Guid.Parse("7f4f3e86-5f71-4e1c-9b4e-13c5c4a0a101");
    internal static readonly Guid EnglishLanguageId = Guid.Parse("2d9c49a1-0f8e-4e22-9f1d-5b9d45a1b202");
    internal static readonly Guid RussianLanguageId = Guid.Parse("c5c13db9-1529-46b0-b8bb-1e92aa58e52e");

    // Yeni Eklenen 10 Dil
    internal static readonly Guid GermanLanguageId = Guid.Parse("d41c89e2-9b2a-41f1-a1e6-3b7c2d1e8fa1");
    internal static readonly Guid FrenchLanguageId = Guid.Parse("a2f1b7d3-c9e4-4a55-b8f2-6c1d4e3a9f02");
    internal static readonly Guid SpanishLanguageId = Guid.Parse("e8c5d3a1-f4b2-4d66-a7e1-8f2c3b5d9a03");
    internal static readonly Guid ItalianLanguageId = Guid.Parse("f1a2d3b4-c5e6-4a77-b8f9-0c1d2e3a4f04");
    internal static readonly Guid ArabicLanguageId = Guid.Parse("b2c3d4e5-f6a7-4b88-c9d0-1e2f3a4b5c05");
    internal static readonly Guid ChineseLanguageId = Guid.Parse("c3d4e5f6-a7b8-4c99-d0e1-2f3a4b5c6d06");
    internal static readonly Guid JapaneseLanguageId = Guid.Parse("d4e5f6a7-b8c9-4d00-e1f2-3a4b5c6d7e07");
    internal static readonly Guid PortugueseLanguageId = Guid.Parse("e5f6a7b8-c9d0-4e11-f2a3-4b5c6d7e8f08");
    internal static readonly Guid HindiLanguageId = Guid.Parse("f6a7b8c9-d0e1-4f22-a3b4-5c6d7e8f9a09");
    internal static readonly Guid KoreanLanguageId = Guid.Parse("a7b8c9d0-e1f2-4a33-b4c5-6d7e8f9a0b10");

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
            },
            new()
            {
                Id = RussianLanguageId,
                Name = "Russian", // Ufak bir düzeltme: Russia yerine Russian yaptım :)
                Culture = "ru-RU",
                TwoLetterIsoCode = "ru",
                IsDefault = false,
                IsActive = true,
                Order = 3
            },
            new()
            {
                Id = GermanLanguageId,
                Name = "Deutsch",
                Culture = "de-DE",
                TwoLetterIsoCode = "de",
                IsDefault = false,
                IsActive = true,
                Order = 4
            },
            new()
            {
                Id = FrenchLanguageId,
                Name = "Français",
                Culture = "fr-FR",
                TwoLetterIsoCode = "fr",
                IsDefault = false,
                IsActive = true,
                Order = 5
            },
            new()
            {
                Id = SpanishLanguageId,
                Name = "Español",
                Culture = "es-ES",
                TwoLetterIsoCode = "es",
                IsDefault = false,
                IsActive = true,
                Order = 6
            },
            new()
            {
                Id = ItalianLanguageId,
                Name = "Italiano",
                Culture = "it-IT",
                TwoLetterIsoCode = "it",
                IsDefault = false,
                IsActive = true,
                Order = 7
            },
            new()
            {
                Id = ArabicLanguageId,
                Name = "العربية", // Arapça
                Culture = "ar-SA",
                TwoLetterIsoCode = "ar",
                IsDefault = false,
                IsActive = true,
                Order = 8
            },
            new()
            {
                Id = ChineseLanguageId,
                Name = "中文", // Çince
                Culture = "zh-CN",
                TwoLetterIsoCode = "zh",
                IsDefault = false,
                IsActive = true,
                Order = 9
            },
            new()
            {
                Id = JapaneseLanguageId,
                Name = "日本語", // Japonca
                Culture = "ja-JP",
                TwoLetterIsoCode = "ja",
                IsDefault = false,
                IsActive = true,
                Order = 10
            },
            new()
            {
                Id = PortugueseLanguageId,
                Name = "Português",
                Culture = "pt-PT",
                TwoLetterIsoCode = "pt",
                IsDefault = false,
                IsActive = true,
                Order = 11
            },
            new()
            {
                Id = HindiLanguageId,
                Name = "हिन्दी", // Hintçe
                Culture = "hi-IN",
                TwoLetterIsoCode = "hi",
                IsDefault = false,
                IsActive = true,
                Order = 12
            },
            new()
            {
                Id = KoreanLanguageId,
                Name = "한국어", // Korece
                Culture = "ko-KR",
                TwoLetterIsoCode = "ko",
                IsDefault = false,
                IsActive = true,
                Order = 13
            }
        };
    }
}