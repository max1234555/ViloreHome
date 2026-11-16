using System.Text.Json;
using Web_prog_server.Services.Items;

namespace Web_prog_server.Middleware;

public sealed class ApiExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            if (context.Response.StatusCode == StatusCodes.Status404NotFound &&
                !context.Response.HasStarted &&
                context.Response.ContentLength is null)
            {
                await WriteErrorAsync(
                    context,
                    StatusCodes.Status404NotFound,
                    new ErrorResponse(
                        "NotFound",
                        "RESOURCE_NOT_FOUND",
                        [new ErrorDetail("path", "The requested resource was not found.")],
                        context.RequestId()));
            }
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception while processing request {RequestId}.",
                context.RequestId());

            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                new ErrorResponse(
                    "InternalServerError",
                    "INTERNAL_SERVER_ERROR",
                    [],
                    context.RequestId()));
        }
    }

    public static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        ErrorResponse error,
        Action<HttpResponse>? configureResponse = null)
    {
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        context.Response.Headers[RequestIdMiddleware.HeaderName] = context.RequestId();
        configureResponse?.Invoke(context.Response);
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            error,
            JsonOptions,
            context.RequestAborted);
    }
}
