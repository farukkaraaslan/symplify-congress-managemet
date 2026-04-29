namespace Core.Application.Localization;

public sealed record CultureContext(
    string RequestedCulture,
    string? BaseCulture);
