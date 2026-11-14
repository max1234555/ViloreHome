using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Web_prog_server.Data;
using Web_prog_server.Services.Items;

namespace Web_prog_server.Tests.Services.Items;

public sealed class ItemServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidData_PersistsAndReturnsItem()
    {
        await using var database = await TestDatabase.CreateAsync();

        var result = await database.Service.CreateAsync(
            new CreateItemRequest("  Floor lamp  ", "  Lighting  ", 129.50m));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.Id > 0);
        Assert.Equal("Floor lamp", result.Value.Name);
        Assert.Equal("Lighting", result.Value.Category);
        Assert.Equal(129.50m, result.Value.Price);

        var storedItem = await database.Context.Items.SingleAsync();
        Assert.Equal(result.Value.Id, storedItem.Id);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidData_ReturnsValidationErrorWithoutPersisting()
    {
        await using var database = await TestDatabase.CreateAsync();

        var result = await database.Service.CreateAsync(
            new CreateItemRequest(" ", "Lighting", 0));

        Assert.False(result.IsSuccess);
        Assert.Equal("ITEM_VALIDATION_FAILED", result.Error!.Code);
        Assert.Contains(result.Error.Details, detail => detail.Field == "Name");
        Assert.Contains(result.Error.Details, detail => detail.Field == "Price");
        Assert.Empty(await database.Context.Items.ToListAsync());
    }

    [Fact]
    public async Task CrudOperations_CreateReadUpdateAndDeleteItem()
    {
        await using var database = await TestDatabase.CreateAsync();
        var created = await database.Service.CreateAsync(
            new CreateItemRequest("Desk", "Furniture", 200m));

        var itemId = Assert.IsType<ItemResponse>(created.Value).Id;
        var read = await database.Service.GetByIdAsync(itemId);
        var updated = await database.Service.UpdateAsync(
            itemId, new UpdateItemRequest("Standing desk", "Furniture", 350m));
        var deleted = await database.Service.DeleteAsync(itemId);
        var afterDelete = await database.Service.GetByIdAsync(itemId);

        Assert.True(read.IsSuccess);
        Assert.Equal("Desk", read.Value!.Name);
        Assert.True(updated.IsSuccess);
        Assert.Equal("Standing desk", updated.Value!.Name);
        Assert.Equal(350m, updated.Value.Price);
        Assert.True(deleted.IsSuccess);
        Assert.False(afterDelete.IsSuccess);
        Assert.Equal("ITEM_NOT_FOUND", afterDelete.Error!.Code);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private TestDatabase(SqliteConnection connection, AppDbContext context)
        {
            _connection = connection;
            Context = context;
            Service = new ItemService(context);
        }

        public AppDbContext Context { get; }
        public IItemService Service { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();

            return new TestDatabase(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
