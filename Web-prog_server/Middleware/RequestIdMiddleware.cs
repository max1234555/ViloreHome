namespace Web_prog_server.Middleware;

public sealed class RequestIdMiddleware
{
    public const string HeaderName = "X-Request-Id";
    private const string ItemKey = "RequestId";
    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var suppliedId = context.Request.Headers[HeaderName].ToString().Trim();
        var requestId = IsValid(suppliedId)
            ? suppliedId
            : Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = requestId;
        context.Response.Headers[HeaderName] = requestId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = requestId;
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private static bool IsValid(string value) =>
        value.Length is > 0 and <= 128 &&
        value.All(character => character is >= (char)0x21 and <= (char)0x7e);
}

public static class RequestIdExtensions
{
    public static string RequestId(this HttpContext context) =>
        context.Items.TryGetValue("RequestId", out var requestId)
            ? (string)requestId!
            : context.TraceIdentifier;
}
