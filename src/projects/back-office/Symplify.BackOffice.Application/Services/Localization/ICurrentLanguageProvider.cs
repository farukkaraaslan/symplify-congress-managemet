namespace Symplify.BackOffice.Application.Services.Localization;
public interface ICurrentLanguageProvider
{
    Task<ApplicationLanguageDto> GetCurrentLanguageAsync(CancellationToken cancellationToken = default);
}
