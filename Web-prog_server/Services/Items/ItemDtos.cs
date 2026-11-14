namespace Web_prog_server.Services.Items;

public sealed record CreateItemRequest(string? Name, string? Category, decimal Price);

public sealed record UpdateItemRequest(string? Name, string? Category, decimal Price);

public sealed record ItemResponse(int Id, string Name, string Category, decimal Price);

public sealed record ErrorDetail(string Field, string Message);

public sealed record ErrorResponse(string Error, string Code, IReadOnlyList<ErrorDetail> Details);

public sealed class ItemServiceResult<T>
{
    private ItemServiceResult(T? value, ErrorResponse? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }
    public ErrorResponse? Error { get; }
    public bool IsSuccess => Error is null;

    public static ItemServiceResult<T> Success(T value) => new(value, null);

    public static ItemServiceResult<T> Failure(ErrorResponse error) => new(default, error);
}
