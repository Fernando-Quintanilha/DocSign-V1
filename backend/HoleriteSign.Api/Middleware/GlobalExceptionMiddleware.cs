using System.Net;
using System.Text.Json;

namespace HoleriteSign.Api.Middleware;

/// <summary>
/// Catches unhandled exceptions and returns a structured JSON error response.
/// Prevents stack trace leaks in production.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var traceId = context.TraceIdentifier;
            _logger.LogError(ex, "Unhandled exception | TraceId={TraceId} | Path={Path} | Method={Method}",
                traceId, context.Request.Path, context.Request.Method);

            context.Response.StatusCode = ex switch
            {
                InvalidOperationException => (int)HttpStatusCode.BadRequest,
                UnauthorizedAccessException => (int)HttpStatusCode.Forbidden,
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                _ => (int)HttpStatusCode.InternalServerError,
            };

            context.Response.ContentType = "application/json";

            var response = new
            {
                error = context.Response.StatusCode switch
                {
                    400 => "BadRequest",
                    403 => "Forbidden",
                    404 => "NotFound",
                    _ => "InternalServerError",
                },
                message = context.Response.StatusCode == 500 && !_env.IsDevelopment()
                    ? "Ocorreu um erro interno. Tente novamente mais tarde."
                    : ex.Message,
                traceId,
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });

            await context.Response.WriteAsync(json);
        }
    }
}
