namespace SystemDesignTutorials.LayeredMonolith.Domain.Entities;

public sealed class InventoryReceipt
{
    private InventoryReceipt()
    {
    }

    public InventoryReceipt(Guid warehouseId, Guid productId, int quantityReceived, string supplierReference, string receivedBy)
    {
        if (quantityReceived <= 0)
        {
            throw new InvalidOperationException("Receipt quantity must be greater than zero.");
        }

        Id = Guid.NewGuid();
        WarehouseId = warehouseId;
        ProductId = productId;
        QuantityReceived = quantityReceived;
        SupplierReference = supplierReference;
        ReceivedBy = receivedBy;
        ReceivedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid ProductId { get; private set; }
    public int QuantityReceived { get; private set; }
    public string SupplierReference { get; private set; } = string.Empty;
    public string ReceivedBy { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; private set; }
}
