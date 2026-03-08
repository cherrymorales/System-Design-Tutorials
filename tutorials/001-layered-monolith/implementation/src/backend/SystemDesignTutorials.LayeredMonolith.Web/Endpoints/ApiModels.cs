using SystemDesignTutorials.LayeredMonolith.Domain.Entities;
using SystemDesignTutorials.LayeredMonolith.Domain.Enums;

namespace SystemDesignTutorials.LayeredMonolith.Web.Endpoints;

internal sealed record ProductResponse(Guid Id, string Sku, string Name, string Category, string SupplierCode, decimal UnitCost, string Status);
internal sealed record WarehouseResponse(Guid Id, string Code, string Name, string City, string Status, int TotalSkuCount, int LowStockSkuCount);
internal sealed record WarehouseInventoryCounts(Guid WarehouseId, int TotalSkuCount, int LowStockSkuCount);
internal sealed record InventorySummaryResponse(Guid Id, Guid ProductId, string ProductSku, string ProductName, Guid WarehouseId, string WarehouseCode, string WarehouseName, int QuantityOnHand, int QuantityReserved, int AvailableQuantity, int ReorderThreshold, bool IsLowStock, DateTimeOffset UpdatedAt);
internal sealed record InventoryReceiptResponse(Guid Id, Guid WarehouseId, string WarehouseCode, string WarehouseName, Guid ProductId, string ProductSku, string ProductName, int QuantityReceived, string SupplierReference, string ReceivedBy, DateTimeOffset ReceivedAt);
internal sealed record StockTransferResponse(Guid Id, Guid ProductId, string ProductSku, string ProductName, Guid SourceWarehouseId, string SourceWarehouseCode, string SourceWarehouseName, Guid DestinationWarehouseId, string DestinationWarehouseCode, string DestinationWarehouseName, int Quantity, string Status, string RequestedBy, string? ApprovedBy, string? DispatchedBy, string? ReceivedBy, string? CancelledBy, DateTimeOffset CreatedAt, DateTimeOffset? ApprovedAt, DateTimeOffset? DispatchedAt, DateTimeOffset? ReceivedAt, DateTimeOffset? CancelledAt, string Reason, string? CancellationReason);
internal sealed record InventoryAdjustmentResponse(Guid Id, Guid ProductId, string ProductSku, string ProductName, Guid WarehouseId, string WarehouseCode, string WarehouseName, int QuantityDelta, decimal EstimatedValueImpact, string ReasonCode, string Status, bool RequiresApproval, string SubmittedBy, DateTimeOffset? SubmittedAt, string? ApprovedBy, DateTimeOffset? ApprovedAt, string? RejectedBy, DateTimeOffset? RejectedAt, string? Notes, DateTimeOffset CreatedAt);
internal sealed record LowStockReportResponse(Guid InventoryItemId, Guid ProductId, string ProductSku, string ProductName, Guid WarehouseId, string WarehouseCode, string WarehouseName, int QuantityOnHand, int QuantityReserved, int AvailableQuantity, int ReorderThreshold, int ShortfallQuantity, DateTimeOffset UpdatedAt);

internal static class ApiMappings
{
    public static ProductResponse ToProductResponse(Product product) =>
        new(product.Id, product.Sku, product.Name, product.Category, product.SupplierCode, product.UnitCost, product.Status.ToString());

    public static WarehouseResponse ToWarehouseResponse(Warehouse warehouse, int totalSkuCount, int lowStockSkuCount) =>
        new(warehouse.Id, warehouse.Code, warehouse.Name, warehouse.City, warehouse.Status.ToString(), totalSkuCount, lowStockSkuCount);

    public static InventorySummaryResponse ToInventorySummaryResponse(InventoryItem item, Product product, Warehouse warehouse) =>
        new(item.Id, item.ProductId, product.Sku, product.Name, item.WarehouseId, warehouse.Code, warehouse.Name, item.QuantityOnHand, item.QuantityReserved, item.AvailableQuantity, item.ReorderThreshold, item.IsLowStock, item.UpdatedAt);

    public static InventoryReceiptResponse ToInventoryReceiptResponse(InventoryReceipt receipt, Product product, Warehouse warehouse) =>
        new(receipt.Id, receipt.WarehouseId, warehouse.Code, warehouse.Name, receipt.ProductId, product.Sku, product.Name, receipt.QuantityReceived, receipt.SupplierReference, receipt.ReceivedBy, receipt.ReceivedAt);

    public static StockTransferResponse ToStockTransferResponse(StockTransfer transfer, Product product, Warehouse source, Warehouse destination) =>
        new(transfer.Id, transfer.ProductId, product.Sku, product.Name, transfer.SourceWarehouseId, source.Code, source.Name, transfer.DestinationWarehouseId, destination.Code, destination.Name, transfer.Quantity, transfer.Status.ToString(), transfer.RequestedBy, transfer.ApprovedBy, transfer.DispatchedBy, transfer.ReceivedBy, transfer.CancelledBy, transfer.CreatedAt, transfer.ApprovedAt, transfer.DispatchedAt, transfer.ReceivedAt, transfer.CancelledAt, transfer.Reason, transfer.CancellationReason);

    public static InventoryAdjustmentResponse ToInventoryAdjustmentResponse(InventoryAdjustment adjustment, Product product, Warehouse warehouse) =>
        new(adjustment.Id, adjustment.ProductId, product.Sku, product.Name, adjustment.WarehouseId, warehouse.Code, warehouse.Name, adjustment.QuantityDelta, Math.Abs(adjustment.QuantityDelta) * product.UnitCost, adjustment.ReasonCode, adjustment.Status.ToString(), adjustment.RequiresApproval, adjustment.SubmittedBy, adjustment.SubmittedAt, adjustment.ApprovedBy, adjustment.ApprovedAt, adjustment.RejectedBy, adjustment.RejectedAt, adjustment.Notes, adjustment.CreatedAt);

    public static LowStockReportResponse ToLowStockReportResponse(InventoryItem item, Product product, Warehouse warehouse) =>
        new(item.Id, item.ProductId, product.Sku, product.Name, item.WarehouseId, warehouse.Code, warehouse.Name, item.QuantityOnHand, item.QuantityReserved, item.AvailableQuantity, item.ReorderThreshold, Math.Max(0, item.ReorderThreshold - item.AvailableQuantity), item.UpdatedAt);
}
