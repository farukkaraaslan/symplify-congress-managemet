using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Core.Application.Pipelines.Caching;

public class CacheRemovingBehavior<TRequest, TResponse>(
    IDistributedCache cache,
    ILogger<CacheRemovingBehavior<TRequest, TResponse>> logger
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICacheRemoverRequest
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request.BypassCache)
            return await next();

        TResponse response = await next();

        if (request.CacheGroupKey != null)
        {
            byte[]? cachedGroup = await cache.GetAsync(request.CacheGroupKey, cancellationToken);
            if (cachedGroup != null)
            {
                var keysInGroup = JsonSerializer.Deserialize<HashSet<string>>(Encoding.Default.GetString(cachedGroup))!;
                foreach (string key in keysInGroup)
                {
                    await cache.RemoveAsync(key, cancellationToken);
                    logger.LogInformation($"Removed Cache -> {key}");
                }

                await cache.RemoveAsync(request.CacheGroupKey, cancellationToken);
                logger.LogInformation($"Removed Cache -> {request.CacheGroupKey}");
            }
        }

        if (request.CacheKey != null)
        {
            await cache.RemoveAsync(request.CacheKey, cancellationToken);
            logger.LogInformation($"Removed Cache -> {request.CacheKey}");
        }

        return response;
    }
}
