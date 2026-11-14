using Microsoft.EntityFrameworkCore;
using Web_prog_server.Data;
using Web_prog_server.Models;

namespace Web_prog_server.Services.Items;

public sealed class ItemService : IItemService
{
    private readonly AppDbContext _db;

    public ItemService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ItemResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await _db.Items
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);

        return items.Select(ToResponse).ToList();
    }

    public async Task<ItemServiceResult<ItemResponse>> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var item = await _db.Items
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return item is null
            ? ItemServiceResult<ItemResponse>.Failure(NotFound(id))
            : ItemServiceResult<ItemResponse>.Success(ToResponse(item));
    }

    public async Task<ItemServiceResult<ItemResponse>> CreateAsync(
        CreateItemRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = Validate(request.Name, request.Category, request.Price);
        if (validationError is not null)
        {
            return ItemServiceResult<ItemResponse>.Failure(validationError);
        }

        var item = new Item
        {
            Name = request.Name!.Trim(),
            Category = request.Category!.Trim(),
            Price = request.Price
        };

        _db.Items.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return ItemServiceResult<ItemResponse>.Success(ToResponse(item));
    }

    public async Task<ItemServiceResult<ItemResponse>> UpdateAsync(
        int id, UpdateItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _db.Items
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (item is null)
        {
            return ItemServiceResult<ItemResponse>.Failure(NotFound(id));
        }

        var validationError = Validate(request.Name, request.Category, request.Price);
        if (validationError is not null)
        {
            return ItemServiceResult<ItemResponse>.Failure(validationError);
        }

        item.Name = request.Name!.Trim();
        item.Category = request.Category!.Trim();
        item.Price = request.Price;
        await _db.SaveChangesAsync(cancellationToken);

        return ItemServiceResult<ItemResponse>.Success(ToResponse(item));
    }

    public async Task<ItemServiceResult<bool>> DeleteAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var item = await _db.Items
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (item is null)
        {
            return ItemServiceResult<bool>.Failure(NotFound(id));
        }

        _db.Items.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);

        return ItemServiceResult<bool>.Success(true);
    }

    private static ErrorResponse? Validate(string? name, string? category, decimal price)
    {
        var details = new List<ErrorDetail>();

        if (string.IsNullOrWhiteSpace(name))
        {
            details.Add(new ErrorDetail(nameof(Item.Name), "Name is required."));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            details.Add(new ErrorDetail(nameof(Item.Category), "Category is required."));
        }

        if (price <= 0)
        {
            details.Add(new ErrorDetail(nameof(Item.Price), "Price must be greater than zero."));
        }

        return details.Count == 0
            ? null
            : new ErrorResponse("ValidationError", "ITEM_VALIDATION_FAILED", details);
    }

    private static ErrorResponse NotFound(int id) =>
        new("NotFound", "ITEM_NOT_FOUND",
            [new ErrorDetail(nameof(Item.Id), $"Item with ID {id} was not found.")]);

    private static ItemResponse ToResponse(Item item) =>
        new(item.Id, item.Name, item.Category, item.Price);
}
