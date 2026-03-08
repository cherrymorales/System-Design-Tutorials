using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.LayeredMonolith.Infrastructure.Persistence;
using SystemDesignTutorials.LayeredMonolith.Web.Contracts;

namespace SystemDesignTutorials.LayeredMonolith.Web.Endpoints;

internal static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this RouteGroupBuilder api)
    {
        var products = api.MapGroup("/products");
        products.MapGet("", GetProductsAsync);
        products.MapGet("/{id:guid}", GetProductByIdAsync);
        products.MapPost("", CreateProductAsync);
        products.MapPut("/{id:guid}", UpdateProductAsync);
        products.MapPost("/{id:guid}/archive", ArchiveProductAsync);

        var warehouses = api.MapGroup("/warehouses");
        warehouses.MapGet("", GetWarehousesAsync);
        warehouses.MapGet("/{id:guid}", GetWarehouseByIdAsync);
        warehouses.MapPost("", CreateWarehouseAsync);
        warehouses.MapPut("/{id:guid}", UpdateWarehouseAsync);
        warehouses.MapPost("/{id:guid}/deactivate", DeactivateWarehouseAsync);
    }

    private static async Task<IResult> GetProductsAsync(LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        var items = await dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);

        return Results.Ok(items.Select(ApiMappings.ToProductResponse));
    }

    private static async Task<IResult> GetProductByIdAsync(Guid id, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return product is null
            ? Results.NotFound(new { message = "Product not found." })
            : Results.Ok(ApiMappings.ToProductResponse(product));
    }

    private static async Task<IResult> CreateProductAsync(CreateProductRequest request, ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageProducts(user))
        {
            return AccessControl.Forbidden("Only purchasing officers and operations managers can manage products.");
        }

        var validationErrors = ValidateCreateProductRequest(request);
        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var normalizedSku = request.Sku.Trim().ToUpperInvariant();
        var skuExists = await dbContext.Products.AnyAsync(product => product.Sku.ToUpper() == normalizedSku, cancellationToken);
        if (skuExists)
        {
            return Results.Conflict(new { message = "A product with the same SKU already exists." });
        }

        var product = new Domain.Entities.Product(
            request.Sku.Trim(),
            request.Name.Trim(),
            request.Category.Trim(),
            request.SupplierCode.Trim(),
            request.UnitCost);

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/products/{product.Id}", ApiMappings.ToProductResponse(product));
    }

    private static async Task<IResult> UpdateProductAsync(Guid id, UpdateProductRequest request, ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageProducts(user))
        {
            return AccessControl.Forbidden("Only purchasing officers and operations managers can manage products.");
        }

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
    }

    private static async Task<IResult> ArchiveProductAsync(Guid id, ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageProducts(user))
        {
            return AccessControl.Forbidden("Only purchasing officers and operations managers can manage products.");
        }

        var product = await dbContext.Products.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (product is null)
        {
            return Results.NotFound(new { message = "Product not found." });
        }

        product.Archive();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ApiMappings.ToProductResponse(product));
    }

    private static async Task<IResult> GetWarehousesAsync(ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        var assignedWarehouseIds = await AccessControl.GetAssignedWarehouseIdsAsync(user, dbContext, cancellationToken);

        var inventoryCounts = await dbContext.InventoryItems
            .AsNoTracking()
            .GroupBy(item => item.WarehouseId)
            .Select(group => new WarehouseInventoryCounts(
                group.Key,
                group.Count(),
                group.Count(item => item.QuantityOnHand - item.QuantityReserved <= item.ReorderThreshold)))
            .ToDictionaryAsync(item => item.WarehouseId, cancellationToken);

        var query = dbContext.Warehouses.AsNoTracking();
        if (AccessControl.IsOperator(user))
        {
            query = query.Where(warehouse => assignedWarehouseIds.Contains(warehouse.Id));
        }

        var items = await query.OrderBy(warehouse => warehouse.Name).ToListAsync(cancellationToken);
        return Results.Ok(items.Select(warehouse =>
        {
            inventoryCounts.TryGetValue(warehouse.Id, out var counts);
            return ApiMappings.ToWarehouseResponse(warehouse, counts?.TotalSkuCount ?? 0, counts?.LowStockSkuCount ?? 0);
        }));
    }

    private static async Task<IResult> GetWarehouseByIdAsync(Guid id, ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!await AccessControl.CanAccessWarehouseAsync(user, dbContext, id, cancellationToken))
        {
            return AccessControl.Forbidden("You do not have access to this warehouse.");
        }

        var warehouse = await dbContext.Warehouses.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
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

        return Results.Ok(ApiMappings.ToWarehouseResponse(warehouse, counts?.TotalSkuCount ?? 0, counts?.LowStockSkuCount ?? 0));
    }

    private static async Task<IResult> CreateWarehouseAsync(CreateWarehouseRequest request, ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageWarehouses(user))
        {
            return AccessControl.Forbidden("Only operations managers can manage warehouses.");
        }

        var validationErrors = ValidateCreateWarehouseRequest(request);
        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var codeExists = await dbContext.Warehouses.AnyAsync(warehouse => warehouse.Code.ToUpper() == normalizedCode, cancellationToken);
        if (codeExists)
        {
            return Results.Conflict(new { message = "A warehouse with the same code already exists." });
        }

        var warehouse = new Domain.Entities.Warehouse(request.Code.Trim(), request.Name.Trim(), request.City.Trim());
        dbContext.Warehouses.Add(warehouse);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/warehouses/{warehouse.Id}", ApiMappings.ToWarehouseResponse(warehouse, 0, 0));
    }

    private static async Task<IResult> UpdateWarehouseAsync(Guid id, UpdateWarehouseRequest request, ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageWarehouses(user))
        {
            return AccessControl.Forbidden("Only operations managers can manage warehouses.");
        }

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
        return await GetWarehouseByIdAsync(id, user, dbContext, cancellationToken);
    }

    private static async Task<IResult> DeactivateWarehouseAsync(Guid id, ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageWarehouses(user))
        {
            return AccessControl.Forbidden("Only operations managers can manage warehouses.");
        }

        var warehouse = await dbContext.Warehouses.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (warehouse is null)
        {
            return Results.NotFound(new { message = "Warehouse not found." });
        }

        warehouse.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetWarehouseByIdAsync(id, user, dbContext, cancellationToken);
    }

    private static Dictionary<string, string[]> ValidateCreateProductRequest(CreateProductRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Sku)) errors[nameof(request.Sku)] = ["SKU is required."];
        if (string.IsNullOrWhiteSpace(request.Name)) errors[nameof(request.Name)] = ["Name is required."];
        if (string.IsNullOrWhiteSpace(request.Category)) errors[nameof(request.Category)] = ["Category is required."];
        if (string.IsNullOrWhiteSpace(request.SupplierCode)) errors[nameof(request.SupplierCode)] = ["Supplier code is required."];
        if (request.UnitCost <= 0) errors[nameof(request.UnitCost)] = ["Unit cost must be greater than zero."];
        return errors;
    }

    private static Dictionary<string, string[]> ValidateUpdateProductRequest(UpdateProductRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name)) errors[nameof(request.Name)] = ["Name is required."];
        if (string.IsNullOrWhiteSpace(request.Category)) errors[nameof(request.Category)] = ["Category is required."];
        if (string.IsNullOrWhiteSpace(request.SupplierCode)) errors[nameof(request.SupplierCode)] = ["Supplier code is required."];
        if (request.UnitCost <= 0) errors[nameof(request.UnitCost)] = ["Unit cost must be greater than zero."];
        return errors;
    }

    private static Dictionary<string, string[]> ValidateCreateWarehouseRequest(CreateWarehouseRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Code)) errors[nameof(request.Code)] = ["Warehouse code is required."];
        if (string.IsNullOrWhiteSpace(request.Name)) errors[nameof(request.Name)] = ["Name is required."];
        if (string.IsNullOrWhiteSpace(request.City)) errors[nameof(request.City)] = ["City is required."];
        return errors;
    }

    private static Dictionary<string, string[]> ValidateUpdateWarehouseRequest(UpdateWarehouseRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name)) errors[nameof(request.Name)] = ["Name is required."];
        if (string.IsNullOrWhiteSpace(request.City)) errors[nameof(request.City)] = ["City is required."];
        return errors;
    }
}
