using SystemDesignTutorials.ModularMonolith.Domain.Enums;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Shared;

namespace SystemDesignTutorials.ModularMonolith.Domain.Modules.Inventory;

public sealed class InventoryReservation
{
    private readonly List<InventoryReservationLine> _lines = [];

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public ReservationStatus Status { get; private set; } = ReservationStatus.Reserved;
    public DateTimeOffset ReservedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReleasedAt { get; private set; }
    public DateTimeOffset? CommittedAt { get; private set; }
    public IReadOnlyCollection<InventoryReservationLine> Lines => _lines;

    private InventoryReservation() { }

    public InventoryReservation(Guid orderId, IEnumerable<InventoryReservationLine> lines)
    {
        OrderId = orderId;
        _lines.AddRange(lines);
        if (_lines.Count == 0) throw new BusinessRuleException("Reservation must contain at least one line.");
    }

    public void Release()
    {
        if (Status != ReservationStatus.Reserved) throw new BusinessRuleException("Only reserved inventory can be released.");
        Status = ReservationStatus.Released;
        ReleasedAt = DateTimeOffset.UtcNow;
    }

    public void Commit()
    {
        if (Status != ReservationStatus.Reserved) throw new BusinessRuleException("Only reserved inventory can be committed.");
        Status = ReservationStatus.Committed;
        CommittedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class InventoryReservationLine
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid InventoryReservationId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public int Quantity { get; private set; }

    private InventoryReservationLine() { }

    public InventoryReservationLine(Guid productId, Guid warehouseId, int quantity)
    {
        if (quantity <= 0) throw new BusinessRuleException("Reservation line quantity must be greater than zero.");
        ProductId = productId;
        WarehouseId = warehouseId;
        Quantity = quantity;
    }
}
