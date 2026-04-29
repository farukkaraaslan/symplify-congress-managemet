namespace Core.Application.Localization;

public interface ICurrentCultureProvider
{
    CultureContext GetCurrent();
}
