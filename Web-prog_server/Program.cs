using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_prog_server.Data;
using Web_prog_server.Middleware;
using Web_prog_server.Models;
using Web_prog_server.Services.Items;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<ItemIdempotencyStore>();
builder.Services.AddScoped<IItemService, ItemService>();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var details = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(error =>
                    new ErrorDetail(entry.Key, error.ErrorMessage)))
                .ToArray();
            var errorResponse = new ErrorResponse(
                "ValidationError",
                "INVALID_REQUEST",
                details,
                context.HttpContext.RequestId());

            return new BadRequestObjectResult(errorResponse);
        };
    });

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("item-write", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromSeconds(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        var retryAfter = context.Lease.TryGetMetadata(
            MetadataName.RetryAfter, out var retryAfterValue)
            ? retryAfterValue
            : TimeSpan.FromSeconds(2);
        var retryAfterSeconds = Math.Max(
            1, (int)Math.Ceiling(retryAfter.TotalSeconds))
            .ToString(CultureInfo.InvariantCulture);

        await ApiExceptionMiddleware.WriteErrorAsync(
            context.HttpContext,
            StatusCodes.Status429TooManyRequests,
            new ErrorResponse(
                "TooManyRequests",
                "RATE_LIMIT_EXCEEDED",
                [new ErrorDetail("request", "Too many item creation requests.")],
                context.HttpContext.RequestId()),
            response => response.Headers["Retry-After"] = retryAfterSeconds);
    };
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://127.0.0.1:5500",
                "http://localhost:5173")
                //"http://localhost:5500")
            .AllowAnyHeader()
            .WithExposedHeaders(RequestIdMiddleware.HeaderName, "Retry-After")
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    db.Database.EnsureCreated();

    if (!db.Items.Any())
    {
        db.Items.AddRange(
            new Item
            {
                Name = "Ароматична свічка",
                Category = "Свічки",
                Price = 450
            },
            new Item
            {
                Name = "Керамічна ваза",
                Category = "Вази",
                Price = 780
            },
            new Item
            {
                Name = "Декоративний підсвічник",
                Category = "Підсвічники",
                Price = 320
            }
        );

        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<ApiExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

app.Run();
