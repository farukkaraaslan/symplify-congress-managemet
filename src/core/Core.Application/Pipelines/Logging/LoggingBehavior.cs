using System.Text.Json;
using Core.CrossCuttingConcerns.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Core.Application.Pipelines.Logging;

public class LoggingBehavior<TRequest, TResponse>(LoggerServiceBase loggerServiceBase, IHttpContextAccessor httpContextAccessor)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ILoggableRequest
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        LogDetail logDetail =
            new()
            {
                MethodName = next.Method.Name,
                Parameters = new List<LogParameter> { new() { Type = request.GetType().Name, Value = request } },
                User = httpContextAccessor.HttpContext?.User.Identity?.Name ?? "?"
            };

        loggerServiceBase.Info(JsonSerializer.Serialize(logDetail));
        return await next();
    }
}
