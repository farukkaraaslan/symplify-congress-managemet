using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.CrossCuttingConcerns.Localization;

public class LocalizationMiddleware
{
    private readonly RequestDelegate _next;

    public LocalizationMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task Invoke(HttpContext context, ILocalizationService localizationService)
    {
        IList<StringWithQualityHeaderValue> acceptLanguages = context.Request.GetTypedHeaders().AcceptLanguage;
        if (acceptLanguages.Count > 0)
            localizationService.AcceptLocales = acceptLanguages
                .OrderByDescending(x => x.Quality ?? 1)
                .Select(x => x.Value.ToString())
                .ToImmutableArray();

        await _next(context);
    }
}
