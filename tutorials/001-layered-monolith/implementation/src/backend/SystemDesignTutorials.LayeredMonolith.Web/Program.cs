using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.LayeredMonolith.Application.DependencyInjection;
using SystemDesignTutorials.LayeredMonolith.Infrastructure;
using SystemDesignTutorials.LayeredMonolith.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LayeredMonolithDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "001-layered-monolith",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/api/products", async (LayeredMonolithDbContext dbContext, CancellationToken cancellationToken) =>
{
    var products = await dbContext.Products
        .OrderBy(product => product.Name)
        .Select(product => new
        {
            product.Id,
            product.Sku,
            product.Name,
            product.Category,
            product.SupplierCode,
            product.UnitCost,
            product.Status
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(products);
});

app.MapGet("/api/warehouses", async (LayeredMonolithDbContext dbContext, CancellationToken cancellationToken) =>
{
    var warehouses = await dbContext.Warehouses
        .OrderBy(warehouse => warehouse.Name)
        .Select(warehouse => new
        {
            warehouse.Id,
            warehouse.Code,
            warehouse.Name,
            warehouse.City,
            warehouse.Status
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(warehouses);
});

app.MapGet("/api/inventory/summary", async (LayeredMonolithDbContext dbContext, CancellationToken cancellationToken) =>
{
    var inventory = await dbContext.InventoryItems
        .OrderBy(item => item.UpdatedAt)
        .Select(item => new
        {
            item.Id,
            item.ProductId,
            item.WarehouseId,
            item.QuantityOnHand,
            item.QuantityReserved,
            item.AvailableQuantity,
            item.ReorderThreshold,
            item.IsLowStock,
            item.UpdatedAt
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(inventory);
});

app.Run();
