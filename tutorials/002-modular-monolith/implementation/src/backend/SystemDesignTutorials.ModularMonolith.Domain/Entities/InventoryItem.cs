namespace SystemDesignTutorials.ModularMonolith.Domain.Entities;

public sealed class InventoryItem
{
    private InventoryItem()
    {
    }

    public InventoryItem(Guid productId, Guid warehouseId, int reorderThreshold)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        WarehouseId = warehouseId;
        ReorderThreshold = reorderThreshold;
        LastReceiptAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public int QuantityOnHand { get; private set; }
    public int QuantityReserved { get; private set; }
    public int ReorderThreshold { get; private set; }
    public DateTimeOffset LastReceiptAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public int AvailableQuantity => QuantityOnHand - QuantityReserved;
    public bool IsLowStock => AvailableQuantity <= ReorderThreshold;

    public void Receive(int quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Receipt quantity must be greater than zero.");
        }

        QuantityOnHand += quantity;
        LastReceiptAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Reserved quantity must be greater than zero.");
        }

        if (quantity > AvailableQuantity)
        {
            throw new InvalidOperationException("Insufficient available stock to reserve.");
        }

        QuantityReserved += quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void DispatchReserved(int quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Dispatch quantity must be greater than zero.");
        }

        if (quantity > QuantityReserved)
        {
            throw new InvalidOperationException("Cannot dispatch more than the reserved quantity.");
        }

        QuantityReserved -= quantity;
        QuantityOnHand -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ReleaseReservation(int quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Released quantity must be greater than zero.");
        }

        if (quantity > QuantityReserved)
        {
            throw new InvalidOperationException("Cannot release more than the reserved quantity.");
        }

        QuantityReserved -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Adjust(int quantityDelta)
    {
        if (QuantityOnHand + quantityDelta < 0)
        {
            throw new InvalidOperationException("Inventory cannot be reduced below zero.");
        }

        QuantityOnHand += quantityDelta;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

