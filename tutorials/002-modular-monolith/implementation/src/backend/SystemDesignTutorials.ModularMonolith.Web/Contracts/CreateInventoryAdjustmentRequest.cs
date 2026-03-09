namespace SystemDesignTutorials.ModularMonolith.Web.Contracts;

public sealed record CreateInventoryAdjustmentRequest(
    Guid WarehouseId,
    Guid ProductId,
    int QuantityDelta,
    string ReasonCode,
    string? Notes);

