namespace SystemDesignTutorials.ModularMonolith.Web.Contracts;

public sealed record CreateInventoryReceiptRequest(
    Guid WarehouseId,
    Guid ProductId,
    int QuantityReceived,
    string SupplierReference);

