using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Application.Pipelines.Caching;

public class CachingBehavior<TRequest, TResponse>(
    IDistributedCache cache,
    ILogger<CachingBehavior<TRequest, TResponse>> logger,
    IConfiguration configuration
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICachableRequest
{
    private readonly CacheSettings _cacheSettings =
        configuration.GetSection("CacheSettings").Get<CacheSettings>() ?? throw new InvalidOperationException("CacheSettings is missing.");

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request.BypassCache)
            return await next();

        TResponse response;
        byte[]? cachedResponse = await cache.GetAsync(request.CacheKey, cancellationToken);
        if (cachedResponse != null)
        {
            response = JsonSerializer.Deserialize<TResponse>(Encoding.Default.GetString(cachedResponse))!;
            logger.LogInformation($"Fetched from Cache -> {request.CacheKey}");
        }
        else
        {
            response = await GetResponseAndAddToCache(request, next, cancellationToken);
        }

        return response;
    }

    private async Task<TResponse> GetResponseAndAddToCache(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        TResponse response = await next();

        TimeSpan? slidingExpiration = request.SlidingExpiration ?? TimeSpan.FromDays(_cacheSettings.SlidingExpiration);
        DistributedCacheEntryOptions cacheOptions = new() { SlidingExpiration = slidingExpiration };

        byte[] serializeData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));
        await cache.SetAsync(request.CacheKey, serializeData, cacheOptions, cancellationToken);
        logger.LogInformation($"Added to Cache -> {request.CacheKey}");

        if (request.CacheGroupKey != null)
        {
            byte[]? cachedGroup = await cache.GetAsync(request.CacheGroupKey, cancellationToken);
            HashSet<string> keysInGroup;
            if (cachedGroup != null)
            {
                keysInGroup = JsonSerializer.Deserialize<HashSet<string>>(Encoding.Default.GetString(cachedGroup))!;
                if (!keysInGroup.Contains(request.CacheKey))
                    keysInGroup.Add(request.CacheKey);
            }
            else
            {
                keysInGroup = new HashSet<string> { request.CacheKey };
            }

            byte[] serializeGroupData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(keysInGroup));
            await cache.SetAsync(request.CacheGroupKey, serializeGroupData, cacheOptions, cancellationToken);
            logger.LogInformation($"Added to Cache -> {request.CacheGroupKey}");
        }

        return response;
    }
}
