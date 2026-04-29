namespace Core.Application.Localization;

public sealed record ApplicationLanguageInfo(
    Guid Id,
    string Culture,
    bool IsDefault,
    bool IsActive);
