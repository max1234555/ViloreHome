namespace Web_prog_server.Services.Items;

public interface IItemService
{
    Task<IReadOnlyList<ItemResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ItemServiceResult<ItemResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ItemServiceResult<ItemResponse>> CreateAsync(
        CreateItemRequest request, CancellationToken cancellationToken = default);
    Task<ItemServiceResult<ItemResponse>> CreateIdempotentAsync(
        CreateItemRequest request, string? idempotencyKey, CancellationToken cancellationToken = default);
    Task<ItemServiceResult<ItemResponse>> UpdateAsync(
        int id, UpdateItemRequest request, CancellationToken cancellationToken = default);
    Task<ItemServiceResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
