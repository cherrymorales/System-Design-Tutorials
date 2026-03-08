namespace SystemDesignTutorials.LayeredMonolith.Web.Contracts;

public sealed record CreateStockTransferRequest(
    Guid SourceWarehouseId,
    Guid DestinationWarehouseId,
    Guid ProductId,
    int Quantity,
    string Reason);
