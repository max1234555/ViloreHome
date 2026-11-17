using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;
using Web_prog_server.Data;
using Web_prog_server.Middleware;
using Web_prog_server.Services.Items;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

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
    public async Task CreateIdempotentAsync_RepeatedKey_ReturnsSameItemWithoutDuplicate()
    {
        await using var database = await TestDatabase.CreateAsync();
        var request = new CreateItemRequest("Lamp", "Lighting", 50m);

        var results = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => database.Service.CreateIdempotentAsync(request, "same-key")));

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.All(results, result => Assert.Equal(results[0].Value!.Id, result.Value!.Id));
        Assert.Single(await database.Context.Items.ToListAsync());
    }

    [Fact]
    public async Task CreateIdempotentAsync_ReusedKeyWithDifferentPayload_ReturnsConflict()
    {
        await using var database = await TestDatabase.CreateAsync();

        await database.Service.CreateIdempotentAsync(
            new CreateItemRequest("Lamp", "Lighting", 50m), "same-key");
        var retry = await database.Service.CreateIdempotentAsync(
            new CreateItemRequest("Chair", "Furniture", 75m), "same-key");

        Assert.False(retry.IsSuccess);
        Assert.Equal("IDEMPOTENCY_KEY_REUSED", retry.Error!.Code);
        Assert.Single(await database.Context.Items.ToListAsync());
    }

    [Fact]
    public async Task RequestId_IsReturnedInHeaderAndStandardErrorBody()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = "test-request-123";
        context.Response.Body = new MemoryStream();
        var exceptionMiddleware = new ApiExceptionMiddleware(
            httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            },
            NullLogger<ApiExceptionMiddleware>.Instance);
        var requestIdMiddleware = new RequestIdMiddleware(exceptionMiddleware.InvokeAsync);

        await requestIdMiddleware.InvokeAsync(context);

        Assert.Equal("test-request-123",
            context.Response.Headers[RequestIdMiddleware.HeaderName].ToString());
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        var error = await JsonSerializer.DeserializeAsync<ErrorResponse>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(error);
        Assert.Equal("test-request-123", error.RequestId);
        Assert.Equal("RESOURCE_NOT_FOUND", error.Code);
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
            Service = new ItemService(context, new ItemIdempotencyStore());
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


