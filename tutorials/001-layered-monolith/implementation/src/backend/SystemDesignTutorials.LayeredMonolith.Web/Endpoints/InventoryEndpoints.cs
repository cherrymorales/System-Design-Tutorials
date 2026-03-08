using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.LayeredMonolith.Domain.Enums;
using SystemDesignTutorials.LayeredMonolith.Infrastructure.Persistence;
using SystemDesignTutorials.LayeredMonolith.Web.Contracts;

namespace SystemDesignTutorials.LayeredMonolith.Web.Endpoints;

internal static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this RouteGroupBuilder api)
    {
        var inventory = api.MapGroup("/inventory");
        inventory.MapGet("/summary", GetInventorySummaryAsync);

        var receipts = inventory.MapGroup("/receipts");
        receipts.MapGet("", GetInventoryReceiptsAsync);
        receipts.MapPost("", CreateInventoryReceiptAsync);

        var reports = api.MapGroup("/reports");
        reports.MapGet("/low-stock", GetLowStockReportAsync);
    }

    private static async Task<IResult> GetInventorySummaryAsync(LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        var rows = await (
            from item in dbContext.InventoryItems.AsNoTracking()
            join product in dbContext.Products.AsNoTracking() on item.ProductId equals product.Id
            join warehouse in dbContext.Warehouses.AsNoTracking() on item.WarehouseId equals warehouse.Id
            orderby warehouse.Name, product.Name
            select new { item, product, warehouse })
            .ToListAsync(cancellationToken);

        return Results.Ok(rows.Select(row => ApiMappings.ToInventorySummaryResponse(row.item, row.product, row.warehouse)));
    }

    private static async Task<IResult> GetInventoryReceiptsAsync(LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        var rows = await (
            from receipt in dbContext.InventoryReceipts.AsNoTracking()
            join product in dbContext.Products.AsNoTracking() on receipt.ProductId equals product.Id
            join warehouse in dbContext.Warehouses.AsNoTracking() on receipt.WarehouseId equals warehouse.Id
            orderby receipt.ReceivedAt descending
            select new { receipt, product, warehouse })
            .ToListAsync(cancellationToken);

        return Results.Ok(rows.Select(row => ApiMappings.ToInventoryReceiptResponse(row.receipt, row.product, row.warehouse)));
    }

    private static async Task<IResult> CreateInventoryReceiptAsync(CreateInventoryReceiptRequest request, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        var validationErrors = ValidateCreateInventoryReceiptRequest(request);
        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var product = await dbContext.Products.SingleOrDefaultAsync(item => item.Id == request.ProductId, cancellationToken);
        if (product is null)
        {
            return Results.NotFound(new { message = "Product not found." });
        }

        if (product.Status != ProductStatus.Active)
        {
            return Results.Conflict(new { message = "Archived products cannot receive stock." });
        }

        var warehouse = await dbContext.Warehouses.SingleOrDefaultAsync(item => item.Id == request.WarehouseId, cancellationToken);
        if (warehouse is null)
        {
            return Results.NotFound(new { message = "Warehouse not found." });
        }

        if (warehouse.Status != WarehouseStatus.Active)
        {
            return Results.Conflict(new { message = "Inactive warehouses cannot receive stock." });
        }

        var inventoryItem = await InventoryUtilities.GetOrCreateInventoryItemAsync(dbContext, request.ProductId, request.WarehouseId, cancellationToken);

        try
        {
            inventoryItem.Receive(request.QuantityReceived);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new { message = exception.Message });
        }

        var receipt = new Domain.Entities.InventoryReceipt(request.WarehouseId, request.ProductId, request.QuantityReceived, request.SupplierReference.Trim(), request.ReceivedBy.Trim());
        dbContext.InventoryReceipts.Add(receipt);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/inventory/receipts/{receipt.Id}", ApiMappings.ToInventoryReceiptResponse(receipt, product, warehouse));
    }

    private static async Task<IResult> GetLowStockReportAsync(LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        var rows = await (
            from item in dbContext.InventoryItems.AsNoTracking()
            join product in dbContext.Products.AsNoTracking() on item.ProductId equals product.Id
            join warehouse in dbContext.Warehouses.AsNoTracking() on item.WarehouseId equals warehouse.Id
            let availableQuantity = item.QuantityOnHand - item.QuantityReserved
            where availableQuantity <= item.ReorderThreshold
            orderby warehouse.Name, availableQuantity, product.Name
            select new { item, product, warehouse })
            .ToListAsync(cancellationToken);

        return Results.Ok(rows.Select(row => ApiMappings.ToLowStockReportResponse(row.item, row.product, row.warehouse)));
    }

    private static Dictionary<string, string[]> ValidateCreateInventoryReceiptRequest(CreateInventoryReceiptRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.WarehouseId == Guid.Empty) errors[nameof(request.WarehouseId)] = ["Warehouse is required."];
        if (request.ProductId == Guid.Empty) errors[nameof(request.ProductId)] = ["Product is required."];
        if (request.QuantityReceived <= 0) errors[nameof(request.QuantityReceived)] = ["Quantity must be greater than zero."];
        if (string.IsNullOrWhiteSpace(request.SupplierReference)) errors[nameof(request.SupplierReference)] = ["Supplier reference is required."];
        if (string.IsNullOrWhiteSpace(request.ReceivedBy)) errors[nameof(request.ReceivedBy)] = ["Received by is required."];
        return errors;
    }
}

internal static class InventoryUtilities
{
    public static async Task<Domain.Entities.InventoryItem> GetOrCreateInventoryItemAsync(LayeredMonolithDbContext dbContext, Guid productId, Guid warehouseId, CancellationToken cancellationToken)
    {
        var inventoryItem = await dbContext.InventoryItems.SingleOrDefaultAsync(item => item.ProductId == productId && item.WarehouseId == warehouseId, cancellationToken);
        if (inventoryItem is not null)
        {
            return inventoryItem;
        }

        inventoryItem = new Domain.Entities.InventoryItem(productId, warehouseId, reorderThreshold: 10);
        dbContext.InventoryItems.Add(inventoryItem);
        return inventoryItem;
    }
}
