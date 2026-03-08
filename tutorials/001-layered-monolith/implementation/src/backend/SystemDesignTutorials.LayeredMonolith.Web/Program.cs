using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.LayeredMonolith.Application.DependencyInjection;
using SystemDesignTutorials.LayeredMonolith.Domain.Entities;
using SystemDesignTutorials.LayeredMonolith.Infrastructure;
using SystemDesignTutorials.LayeredMonolith.Infrastructure.Persistence;
using SystemDesignTutorials.LayeredMonolith.Infrastructure.Seeding;
using SystemDesignTutorials.LayeredMonolith.Web.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await ApplicationDataSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "001-layered-monolith",
    timestamp = DateTimeOffset.UtcNow
}));

var products = api.MapGroup("/products");

products.MapGet("", async (LayeredMonolithDbContext dbContext, CancellationToken cancellationToken) =>
{
    var items = await dbContext.Products
        .AsNoTracking()
        .OrderBy(product => product.Name)
        .ToListAsync(cancellationToken);

    return Results.Ok(items.Select(ApiMappings.ToProductResponse));
});

products.MapGet("/{id:guid}", async (Guid id, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken) =>
{
    var product = await dbContext.Products
        .AsNoTracking()
        .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    return product is null
        ? Results.NotFound(new { message = "Product not found." })
        : Results.Ok(ApiMappings.ToProductResponse(product));
});

products.MapPost("", async (CreateProductRequest request, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken) =>
{
    var validationErrors = ValidateCreateProductRequest(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var normalizedSku = request.Sku.Trim().ToUpperInvariant();
    var skuExists = await dbContext.Products.AnyAsync(
        product => product.Sku.ToUpper() == normalizedSku,
        cancellationToken);

    if (skuExists)
    {
        return Results.Conflict(new { message = "A product with the same SKU already exists." });
    }

    var product = new Product(
        request.Sku.Trim(),
        request.Name.Trim(),
        request.Category.Trim(),
        request.SupplierCode.Trim(),
        request.UnitCost);

    dbContext.Products.Add(product);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Created($"/api/products/{product.Id}", ApiMappings.ToProductResponse(product));
});

products.MapPut("/{id:guid}", async (Guid id, UpdateProductRequest request, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken) =>
{
    var validationErrors = ValidateUpdateProductRequest(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var product = await dbContext.Products.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    if (product is null)
    {
        return Results.NotFound(new { message = "Product not found." });
    }

    product.UpdateDetails(
        request.Name.Trim(),
        request.Category.Trim(),
        request.SupplierCode.Trim(),
        request.UnitCost);

    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(ApiMappings.ToProductResponse(product));
});

products.MapPost("/{id:guid}/archive", async (Guid id, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken) =>
{
    var product = await dbContext.Products.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    if (product is null)
    {
        return Results.NotFound(new { message = "Product not found." });
    }

    product.Archive();
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(ApiMappings.ToProductResponse(product));
});

var warehouses = api.MapGroup("/warehouses");

warehouses.MapGet("", async (LayeredMonolithDbContext dbContext, CancellationToken cancellationToken) =>
{
    var inventoryCounts = await dbContext.InventoryItems
        .AsNoTracking()
        .GroupBy(item => item.WarehouseId)
        .Select(group => new WarehouseInventoryCounts(
            group.Key,
            group.Count(),
            group.Count(item => item.QuantityOnHand - item.QuantityReserved <= item.ReorderThreshold)))
        .ToDictionaryAsync(item => item.WarehouseId, cancellationToken);

    var items = await dbContext.Warehouses
        .AsNoTracking()
        .OrderBy(warehouse => warehouse.Name)
        .ToListAsync(cancellationToken);

    var responses = items.Select(warehouse =>
    {
        inventoryCounts.TryGetValue(warehouse.Id, out var counts);
        return ApiMappings.ToWarehouseResponse(
            warehouse,
            counts?.TotalSkuCount ?? 0,
            counts?.LowStockSkuCount ?? 0);
    });

    return Results.Ok(responses);
});

warehouses.MapGet("/{id:guid}", async (Guid id, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken) =>
{
    var warehouse = await dbContext.Warehouses
        .AsNoTracking()
        .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    if (warehouse is null)
    {
        return Results.NotFound(new { message = "Warehouse not found." });
    }

    var counts = await dbContext.InventoryItems
        .AsNoTracking()
        .Where(item => item.WarehouseId == id)
        .GroupBy(item => item.WarehouseId)
        .Select(group => new WarehouseInventoryCounts(
            group.Key,
            group.Count(),
            group.Count(item => item.QuantityOnHand - item.QuantityReserved <= item.ReorderThreshold)))
        .SingleOrDefaultAsync(cancellationToken);

    return Results.Ok(ApiMappings.ToWarehouseResponse(
        warehouse,
        counts?.TotalSkuCount ?? 0,
        counts?.LowStockSkuCount ?? 0));
});

warehouses.MapPost("", async (CreateWarehouseRequest request, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken) =>
{
    var validationErrors = ValidateCreateWarehouseRequest(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var normalizedCode = request.Code.Trim().ToUpperInvariant();
    var codeExists = await dbContext.Warehouses.AnyAsync(
        warehouse => warehouse.Code.ToUpper() == normalizedCode,
        cancellationToken);

    if (codeExists)
    {
        return Results.Conflict(new { message = "A warehouse with the same code already exists." });
    }

    var warehouse = new Warehouse(
        request.Code.Trim(),
        request.Name.Trim(),
        request.City.Trim());

    dbContext.Warehouses.Add(warehouse);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Created($"/api/warehouses/{warehouse.Id}", ApiMappings.ToWarehouseResponse(warehouse, 0, 0));
});

warehouses.MapPut("/{id:guid}", async (Guid id, UpdateWarehouseRequest request, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken) =>
{
    var validationErrors = ValidateUpdateWarehouseRequest(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var warehouse = await dbContext.Warehouses.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    if (warehouse is null)
    {
        return Results.NotFound(new { message = "Warehouse not found." });
    }

    warehouse.UpdateDetails(request.Name.Trim(), request.City.Trim());
    await dbContext.SaveChangesAsync(cancellationToken);

    var skuCount = await dbContext.InventoryItems.CountAsync(item => item.WarehouseId == id, cancellationToken);
    var lowStockSkuCount = await dbContext.InventoryItems.CountAsync(
        item => item.WarehouseId == id && item.QuantityOnHand - item.QuantityReserved <= item.ReorderThreshold,
        cancellationToken);

    return Results.Ok(ApiMappings.ToWarehouseResponse(warehouse, skuCount, lowStockSkuCount));
});

warehouses.MapPost("/{id:guid}/deactivate", async (Guid id, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken) =>
{
    var warehouse = await dbContext.Warehouses.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    if (warehouse is null)
    {
        return Results.NotFound(new { message = "Warehouse not found." });
    }

    warehouse.Deactivate();
    await dbContext.SaveChangesAsync(cancellationToken);

    var skuCount = await dbContext.InventoryItems.CountAsync(item => item.WarehouseId == id, cancellationToken);
    var lowStockSkuCount = await dbContext.InventoryItems.CountAsync(
        item => item.WarehouseId == id && item.QuantityOnHand - item.QuantityReserved <= item.ReorderThreshold,
        cancellationToken);

    return Results.Ok(ApiMappings.ToWarehouseResponse(warehouse, skuCount, lowStockSkuCount));
});

api.MapGet("/inventory/summary", async (LayeredMonolithDbContext dbContext, CancellationToken cancellationToken) =>
{
    var inventory = await (
        from item in dbContext.InventoryItems.AsNoTracking()
        join product in dbContext.Products.AsNoTracking() on item.ProductId equals product.Id
        join warehouse in dbContext.Warehouses.AsNoTracking() on item.WarehouseId equals warehouse.Id
        orderby warehouse.Name, product.Name
        select new InventorySummaryResponse(
            item.Id,
            item.ProductId,
            product.Sku,
            product.Name,
            item.WarehouseId,
            warehouse.Code,
            warehouse.Name,
            item.QuantityOnHand,
            item.QuantityReserved,
            item.AvailableQuantity,
            item.ReorderThreshold,
            item.IsLowStock,
            item.UpdatedAt))
        .ToListAsync(cancellationToken);

    return Results.Ok(inventory);
});

app.Run();

static Dictionary<string, string[]> ValidateCreateProductRequest(CreateProductRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Sku))
    {
        errors[nameof(request.Sku)] = ["SKU is required."];
    }

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        errors[nameof(request.Name)] = ["Name is required."];
    }

    if (string.IsNullOrWhiteSpace(request.Category))
    {
        errors[nameof(request.Category)] = ["Category is required."];
    }

    if (string.IsNullOrWhiteSpace(request.SupplierCode))
    {
        errors[nameof(request.SupplierCode)] = ["Supplier code is required."];
    }

    if (request.UnitCost <= 0)
    {
        errors[nameof(request.UnitCost)] = ["Unit cost must be greater than zero."];
    }

    return errors;
}

static Dictionary<string, string[]> ValidateUpdateProductRequest(UpdateProductRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        errors[nameof(request.Name)] = ["Name is required."];
    }

    if (string.IsNullOrWhiteSpace(request.Category))
    {
        errors[nameof(request.Category)] = ["Category is required."];
    }

    if (string.IsNullOrWhiteSpace(request.SupplierCode))
    {
        errors[nameof(request.SupplierCode)] = ["Supplier code is required."];
    }

    if (request.UnitCost <= 0)
    {
        errors[nameof(request.UnitCost)] = ["Unit cost must be greater than zero."];
    }

    return errors;
}

static Dictionary<string, string[]> ValidateCreateWarehouseRequest(CreateWarehouseRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Code))
    {
        errors[nameof(request.Code)] = ["Warehouse code is required."];
    }

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        errors[nameof(request.Name)] = ["Name is required."];
    }

    if (string.IsNullOrWhiteSpace(request.City))
    {
        errors[nameof(request.City)] = ["City is required."];
    }

    return errors;
}

static Dictionary<string, string[]> ValidateUpdateWarehouseRequest(UpdateWarehouseRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        errors[nameof(request.Name)] = ["Name is required."];
    }

    if (string.IsNullOrWhiteSpace(request.City))
    {
        errors[nameof(request.City)] = ["City is required."];
    }

    return errors;
}

internal sealed record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    string Category,
    string SupplierCode,
    decimal UnitCost,
    string Status);

internal sealed record WarehouseResponse(
    Guid Id,
    string Code,
    string Name,
    string City,
    string Status,
    int TotalSkuCount,
    int LowStockSkuCount);

internal sealed record WarehouseInventoryCounts(
    Guid WarehouseId,
    int TotalSkuCount,
    int LowStockSkuCount);

internal sealed record InventorySummaryResponse(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    int QuantityOnHand,
    int QuantityReserved,
    int AvailableQuantity,
    int ReorderThreshold,
    bool IsLowStock,
    DateTimeOffset UpdatedAt);

internal static class ApiMappings
{
    public static ProductResponse ToProductResponse(Product product) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.Category,
        product.SupplierCode,
        product.UnitCost,
        product.Status.ToString());

    public static WarehouseResponse ToWarehouseResponse(Warehouse warehouse, int totalSkuCount, int lowStockSkuCount) => new(
        warehouse.Id,
        warehouse.Code,
        warehouse.Name,
        warehouse.City,
        warehouse.Status.ToString(),
        totalSkuCount,
        lowStockSkuCount);
}
