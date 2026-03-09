using SystemDesignTutorials.ModularMonolith.Domain.Modules.Shared;

namespace SystemDesignTutorials.ModularMonolith.Domain.Modules.Inventory;

public sealed class StockItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public int QuantityOnHand { get; private set; }
    public int QuantityReserved { get; private set; }
    public int ReorderThreshold { get; private set; } = 10;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private StockItem() { }

    public StockItem(Guid productId, Guid warehouseId, int quantityOnHand, int reorderThreshold = 10)
    {
        if (quantityOnHand < 0) throw new BusinessRuleException("Quantity on hand cannot be negative.");
        if (reorderThreshold < 0) throw new BusinessRuleException("Reorder threshold cannot be negative.");
        ProductId = productId;
        WarehouseId = warehouseId;
        QuantityOnHand = quantityOnHand;
        ReorderThreshold = reorderThreshold;
    }

    public int AvailableQuantity => QuantityOnHand - QuantityReserved;

    public void Reserve(int quantity)
    {
        if (quantity <= 0) throw new BusinessRuleException("Reservation quantity must be greater than zero.");
        if (AvailableQuantity < quantity) throw new BusinessRuleException("Insufficient stock for reservation.");
        QuantityReserved += quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Release(int quantity)
    {
        if (quantity <= 0) throw new BusinessRuleException("Release quantity must be greater than zero.");
        if (QuantityReserved < quantity) throw new BusinessRuleException("Reserved quantity cannot fall below zero.");
        QuantityReserved -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Commit(int quantity)
    {
        if (quantity <= 0) throw new BusinessRuleException("Commit quantity must be greater than zero.");
        if (QuantityReserved < quantity) throw new BusinessRuleException("Reserved quantity cannot fall below zero.");
        if (QuantityOnHand < quantity) throw new BusinessRuleException("On-hand quantity cannot fall below zero.");
        QuantityReserved -= quantity;
        QuantityOnHand -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
