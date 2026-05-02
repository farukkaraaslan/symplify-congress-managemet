using System.Text.Json;
using Core.CrossCuttingConcerns.Exceptions.Handlers;
using Core.CrossCuttingConcerns.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog;
using Microsoft.AspNetCore.Http;

namespace Core.CrossCuttingConcerns.Exceptions;

public class ExceptionMiddleware(
    RequestDelegate next,
    IHttpContextAccessor contextAccessor,
    LoggerServiceBase loggerService
)
{
    private readonly HttpExceptionHandler _httpExceptionHandler = new();

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await LogException(context, exception);
            await HandleExceptionAsync(context.Response, exception);
        }
    }

    private Task HandleExceptionAsync(HttpResponse response, Exception exception)
    {
        response.ContentType = "application/json";
        _httpExceptionHandler.Response = response;
        return _httpExceptionHandler.HandleExceptionAsync(exception);
    }

    private Task LogException(HttpContext context, Exception exception)
    {
        List<LogParameter> logParameters =
            new()
            {
                new LogParameter { Type = context.GetType().Name, Value = exception.ToString() }
            };

        LogDetail logDetail =
            new()
            {
                MethodName = next.Method.Name,
                Parameters = logParameters,
                User = contextAccessor.HttpContext?.User.Identity?.Name ?? "?"
            };

        loggerService.Info(JsonSerializer.Serialize(logDetail));
        return Task.CompletedTask;
    }
}
