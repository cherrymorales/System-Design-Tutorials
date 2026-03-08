using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.LayeredMonolith.Domain.Enums;
using SystemDesignTutorials.LayeredMonolith.Infrastructure.Persistence;
using SystemDesignTutorials.LayeredMonolith.Web.Contracts;

namespace SystemDesignTutorials.LayeredMonolith.Web.Endpoints;

internal static class WorkflowEndpoints
{
    public static void MapWorkflowEndpoints(this RouteGroupBuilder api)
    {
        var transfers = api.MapGroup("/transfers");
        transfers.MapGet("", GetTransfersAsync);
        transfers.MapPost("", CreateTransferAsync);
        transfers.MapPost("/{id:guid}/approve", ApproveTransferAsync);
        transfers.MapPost("/{id:guid}/dispatch", DispatchTransferAsync);
        transfers.MapPost("/{id:guid}/receive", ReceiveTransferAsync);
        transfers.MapPost("/{id:guid}/cancel", CancelTransferAsync);

        var adjustments = api.MapGroup("/adjustments");
        adjustments.MapGet("", GetAdjustmentsAsync);
        adjustments.MapGet("/pending", GetPendingAdjustmentsAsync);
        adjustments.MapPost("", CreateAdjustmentAsync);
        adjustments.MapPost("/{id:guid}/approve", ApproveAdjustmentAsync);
        adjustments.MapPost("/{id:guid}/reject", RejectAdjustmentAsync);
    }

    private static async Task<IResult> GetTransfersAsync(ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        var data = await BuildTransferResponsesAsync(user, dbContext, cancellationToken);
        return Results.Ok(data.OrderByDescending(item => item.CreatedAt));
    }

    private static async Task<IResult> CreateTransferAsync(CreateStockTransferRequest request, ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanCreateTransfers(user))
        {
            return AccessControl.Forbidden("Only inventory planners and operations managers can create transfers.");
        }

        var validationErrors = ValidateCreateStockTransferRequest(request);
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
            return Results.Conflict(new { message = "Archived products cannot be transferred." });
        }

        var source = await dbContext.Warehouses.SingleOrDefaultAsync(item => item.Id == request.SourceWarehouseId, cancellationToken);
        var destination = await dbContext.Warehouses.SingleOrDefaultAsync(item => item.Id == request.DestinationWarehouseId, cancellationToken);
        if (source is null || destination is null)
        {
            return Results.NotFound(new { message = "Source or destination warehouse not found." });
        }

        if (source.Status != WarehouseStatus.Active || destination.Status != WarehouseStatus.Active)
        {
            return Results.Conflict(new { message = "Transfers require both warehouses to be active." });
        }

        var sourceInventory = await dbContext.InventoryItems.SingleOrDefaultAsync(
            item => item.ProductId == request.ProductId && item.WarehouseId == request.SourceWarehouseId,
            cancellationToken);

        if (sourceInventory is null)
        {
            return Results.Conflict(new { message = "Source warehouse does not have inventory for this product." });
        }

        try
        {
            sourceInventory.Reserve(request.Quantity);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new { message = exception.Message });
        }

        var transfer = new Domain.Entities.StockTransfer(
            request.SourceWarehouseId,
            request.DestinationWarehouseId,
            request.ProductId,
            request.Quantity,
            AccessControl.GetRequiredEmail(user),
            request.Reason.Trim());

        dbContext.StockTransfers.Add(transfer);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/transfers/{transfer.Id}", ApiMappings.ToStockTransferResponse(transfer, product, source, destination));
    }

    private static async Task<IResult> ApproveTransferAsync(Guid id, ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanApproveTransfers(user))
        {
            return AccessControl.Forbidden("Only inventory planners and operations managers can approve transfers.");
        }

        var transfer = await dbContext.StockTransfers.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (transfer is null)
        {
            return Results.NotFound(new { message = "Transfer not found." });
        }

        try
        {
            transfer.Approve(AccessControl.GetRequiredEmail(user));
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new { message = exception.Message });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildTransferResultAsync(user, dbContext, transfer.Id, cancellationToken);
    }

    private static async Task<IResult> DispatchTransferAsync(Guid id, ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanDispatchTransfers(user))
        {
            return AccessControl.Forbidden("Only warehouse operators and operations managers can dispatch transfers.");
        }

        var transfer = await dbContext.StockTransfers.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (transfer is null)
        {
            return Results.NotFound(new { message = "Transfer not found." });
        }

        if (!await AccessControl.CanAccessWarehouseAsync(user, dbContext, transfer.SourceWarehouseId, cancellationToken))
        {
            return AccessControl.Forbidden("You do not have access to dispatch stock from this warehouse.");
        }

        var inventoryItem = await dbContext.InventoryItems.SingleOrDefaultAsync(
            item => item.ProductId == transfer.ProductId && item.WarehouseId == transfer.SourceWarehouseId,
            cancellationToken);

        if (inventoryItem is null)
        {
            return Results.Conflict(new { message = "Source inventory record not found for transfer dispatch." });
        }

        try
        {
            transfer.Dispatch(AccessControl.GetRequiredEmail(user));
            inventoryItem.DispatchReserved(transfer.Quantity);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new { message = exception.Message });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildTransferResultAsync(user, dbContext, transfer.Id, cancellationToken);
    }

    private static async Task<IResult> ReceiveTransferAsync(Guid id, ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanReceiveTransfers(user))
        {
            return AccessControl.Forbidden("Only warehouse operators and operations managers can receive transfers.");
        }

        var transfer = await dbContext.StockTransfers.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (transfer is null)
        {
            return Results.NotFound(new { message = "Transfer not found." });
        }

        if (!await AccessControl.CanAccessWarehouseAsync(user, dbContext, transfer.DestinationWarehouseId, cancellationToken))
        {
            return AccessControl.Forbidden("You do not have access to receive stock into this warehouse.");
        }

        var destinationInventory = await InventoryUtilities.GetOrCreateInventoryItemAsync(
            dbContext,
            transfer.ProductId,
            transfer.DestinationWarehouseId,
            cancellationToken);

        try
        {
            transfer.Receive(AccessControl.GetRequiredEmail(user));
            destinationInventory.Receive(transfer.Quantity);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new { message = exception.Message });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildTransferResultAsync(user, dbContext, transfer.Id, cancellationToken);
    }

    private static async Task<IResult> CancelTransferAsync(Guid id, CancelStockTransferRequest request, ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanCancelTransfers(user))
        {
            return AccessControl.Forbidden("Only operations managers can cancel transfers.");
        }

        var transfer = await dbContext.StockTransfers.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (transfer is null)
        {
            return Results.NotFound(new { message = "Transfer not found." });
        }

        try
        {
            transfer.Cancel(AccessControl.GetRequiredEmail(user), request.CancellationReason?.Trim());
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new { message = exception.Message });
        }

        var sourceInventory = await dbContext.InventoryItems.SingleOrDefaultAsync(
            item => item.ProductId == transfer.ProductId && item.WarehouseId == transfer.SourceWarehouseId,
            cancellationToken);

        if (sourceInventory is not null)
        {
            sourceInventory.ReleaseReservation(transfer.Quantity);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildTransferResultAsync(user, dbContext, transfer.Id, cancellationToken);
    }

    private static async Task<IResult> GetAdjustmentsAsync(ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        var data = await BuildAdjustmentResponsesAsync(user, dbContext, cancellationToken);
        return Results.Ok(data.OrderByDescending(item => item.CreatedAt));
    }

    private static async Task<IResult> GetPendingAdjustmentsAsync(ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanReviewAdjustments(user))
        {
            return AccessControl.Forbidden("Only operations managers can review pending adjustments.");
        }

        var rows = await (
            from adjustment in dbContext.InventoryAdjustments.AsNoTracking()
            where adjustment.Status == AdjustmentStatus.PendingApproval
            join product in dbContext.Products.AsNoTracking() on adjustment.ProductId equals product.Id
            join warehouse in dbContext.Warehouses.AsNoTracking() on adjustment.WarehouseId equals warehouse.Id
            select new { adjustment, product, warehouse })
            .ToListAsync(cancellationToken);

        return Results.Ok(rows
            .Select(row => ApiMappings.ToInventoryAdjustmentResponse(row.adjustment, row.product, row.warehouse))
            .OrderByDescending(item => item.CreatedAt));
    }

    private static async Task<IResult> CreateAdjustmentAsync(CreateInventoryAdjustmentRequest request, ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanCreateAdjustments(user))
        {
            return AccessControl.Forbidden("Only warehouse operators and operations managers can create adjustments.");
        }

        var validationErrors = ValidateCreateInventoryAdjustmentRequest(request);
        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        if (!await AccessControl.CanAccessWarehouseAsync(user, dbContext, request.WarehouseId, cancellationToken))
        {
            return AccessControl.Forbidden("You do not have access to adjust stock for this warehouse.");
        }

        var product = await dbContext.Products.SingleOrDefaultAsync(item => item.Id == request.ProductId, cancellationToken);
        if (product is null)
        {
            return Results.NotFound(new { message = "Product not found." });
        }

        if (product.Status != ProductStatus.Active)
        {
            return Results.Conflict(new { message = "Archived products cannot be adjusted." });
        }

        var warehouse = await dbContext.Warehouses.SingleOrDefaultAsync(item => item.Id == request.WarehouseId, cancellationToken);
        if (warehouse is null)
        {
            return Results.NotFound(new { message = "Warehouse not found." });
        }

        if (warehouse.Status != WarehouseStatus.Active)
        {
            return Results.Conflict(new { message = "Inactive warehouses cannot be adjusted." });
        }

        var inventoryItem = await InventoryUtilities.GetOrCreateInventoryItemAsync(dbContext, request.ProductId, request.WarehouseId, cancellationToken);
        var adjustment = new Domain.Entities.InventoryAdjustment(
            request.WarehouseId,
            request.ProductId,
            request.QuantityDelta,
            request.ReasonCode.Trim(),
            AccessControl.GetRequiredEmail(user),
            request.Notes?.Trim());

        try
        {
            adjustment.Submit(product.UnitCost);
            if (adjustment.Status == AdjustmentStatus.Approved)
            {
                inventoryItem.Adjust(adjustment.QuantityDelta);
            }
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new { message = exception.Message });
        }

        dbContext.InventoryAdjustments.Add(adjustment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildAdjustmentResultAsync(user, dbContext, adjustment.Id, cancellationToken, created: true);
    }

    private static async Task<IResult> ApproveAdjustmentAsync(Guid id, ApproveInventoryAdjustmentRequest request, ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanReviewAdjustments(user))
        {
            return AccessControl.Forbidden("Only operations managers can approve adjustments.");
        }

        var adjustment = await dbContext.InventoryAdjustments.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (adjustment is null)
        {
            return Results.NotFound(new { message = "Adjustment not found." });
        }

        var inventoryItem = await InventoryUtilities.GetOrCreateInventoryItemAsync(dbContext, adjustment.ProductId, adjustment.WarehouseId, cancellationToken);

        try
        {
            adjustment.Approve(AccessControl.GetRequiredEmail(user), request.Notes?.Trim());
            inventoryItem.Adjust(adjustment.QuantityDelta);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new { message = exception.Message });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildAdjustmentResultAsync(user, dbContext, adjustment.Id, cancellationToken, created: false);
    }

    private static async Task<IResult> RejectAdjustmentAsync(Guid id, RejectInventoryAdjustmentRequest request, ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanReviewAdjustments(user))
        {
            return AccessControl.Forbidden("Only operations managers can reject adjustments.");
        }

        var adjustment = await dbContext.InventoryAdjustments.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (adjustment is null)
        {
            return Results.NotFound(new { message = "Adjustment not found." });
        }

        try
        {
            adjustment.Reject(AccessControl.GetRequiredEmail(user), request.Notes?.Trim());
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new { message = exception.Message });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildAdjustmentResultAsync(user, dbContext, adjustment.Id, cancellationToken, created: false);
    }

    private static async Task<List<StockTransferResponse>> BuildTransferResponsesAsync(ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        var assignedWarehouseIds = await AccessControl.GetAssignedWarehouseIdsAsync(user, dbContext, cancellationToken);
        var query =
            from transfer in dbContext.StockTransfers.AsNoTracking()
            join product in dbContext.Products.AsNoTracking() on transfer.ProductId equals product.Id
            join source in dbContext.Warehouses.AsNoTracking() on transfer.SourceWarehouseId equals source.Id
            join destination in dbContext.Warehouses.AsNoTracking() on transfer.DestinationWarehouseId equals destination.Id
            select new { transfer, product, source, destination };

        if (AccessControl.IsOperator(user))
        {
            query = query.Where(row =>
                assignedWarehouseIds.Contains(row.source.Id) ||
                assignedWarehouseIds.Contains(row.destination.Id));
        }

        var rows = await query.ToListAsync(cancellationToken);
        return rows.Select(row => ApiMappings.ToStockTransferResponse(row.transfer, row.product, row.source, row.destination)).ToList();
    }

    private static async Task<List<InventoryAdjustmentResponse>> BuildAdjustmentResponsesAsync(ClaimsPrincipal user, LayeredMonolithDbContext dbContext, CancellationToken cancellationToken)
    {
        var assignedWarehouseIds = await AccessControl.GetAssignedWarehouseIdsAsync(user, dbContext, cancellationToken);
        var query =
            from adjustment in dbContext.InventoryAdjustments.AsNoTracking()
            join product in dbContext.Products.AsNoTracking() on adjustment.ProductId equals product.Id
            join warehouse in dbContext.Warehouses.AsNoTracking() on adjustment.WarehouseId equals warehouse.Id
            select new { adjustment, product, warehouse };

        if (AccessControl.IsOperator(user))
        {
            query = query.Where(row => assignedWarehouseIds.Contains(row.warehouse.Id));
        }

        var rows = await query.ToListAsync(cancellationToken);
        return rows.Select(row => ApiMappings.ToInventoryAdjustmentResponse(row.adjustment, row.product, row.warehouse)).ToList();
    }

    private static async Task<IResult> BuildTransferResultAsync(ClaimsPrincipal user, LayeredMonolithDbContext dbContext, Guid transferId, CancellationToken cancellationToken)
    {
        var responses = await BuildTransferResponsesAsync(user, dbContext, cancellationToken);
        var transfer = responses.SingleOrDefault(item => item.Id == transferId);
        return transfer is null
            ? AccessControl.Forbidden("You do not have access to this transfer.")
            : Results.Ok(transfer);
    }

    private static async Task<IResult> BuildAdjustmentResultAsync(ClaimsPrincipal user, LayeredMonolithDbContext dbContext, Guid adjustmentId, CancellationToken cancellationToken, bool created)
    {
        var responses = await BuildAdjustmentResponsesAsync(user, dbContext, cancellationToken);
        var adjustment = responses.SingleOrDefault(item => item.Id == adjustmentId);
        if (adjustment is null)
        {
            return AccessControl.Forbidden("You do not have access to this adjustment.");
        }

        return created ? Results.Created($"/api/adjustments/{adjustment.Id}", adjustment) : Results.Ok(adjustment);
    }

    private static Dictionary<string, string[]> ValidateCreateStockTransferRequest(CreateStockTransferRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.SourceWarehouseId == Guid.Empty) errors[nameof(request.SourceWarehouseId)] = ["Source warehouse is required."];
        if (request.DestinationWarehouseId == Guid.Empty) errors[nameof(request.DestinationWarehouseId)] = ["Destination warehouse is required."];
        if (request.SourceWarehouseId == request.DestinationWarehouseId && request.SourceWarehouseId != Guid.Empty) errors[nameof(request.DestinationWarehouseId)] = ["Source and destination must be different."];
        if (request.ProductId == Guid.Empty) errors[nameof(request.ProductId)] = ["Product is required."];
        if (request.Quantity <= 0) errors[nameof(request.Quantity)] = ["Quantity must be greater than zero."];
        if (string.IsNullOrWhiteSpace(request.Reason)) errors[nameof(request.Reason)] = ["Reason is required."];
        return errors;
    }

    private static Dictionary<string, string[]> ValidateCreateInventoryAdjustmentRequest(CreateInventoryAdjustmentRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.WarehouseId == Guid.Empty) errors[nameof(request.WarehouseId)] = ["Warehouse is required."];
        if (request.ProductId == Guid.Empty) errors[nameof(request.ProductId)] = ["Product is required."];
        if (request.QuantityDelta == 0) errors[nameof(request.QuantityDelta)] = ["Quantity delta cannot be zero."];
        if (string.IsNullOrWhiteSpace(request.ReasonCode)) errors[nameof(request.ReasonCode)] = ["Reason code is required."];
        return errors;
    }
}
