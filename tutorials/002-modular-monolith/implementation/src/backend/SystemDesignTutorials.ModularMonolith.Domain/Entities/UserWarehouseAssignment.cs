namespace SystemDesignTutorials.ModularMonolith.Domain.Entities;

public sealed class UserWarehouseAssignment
{
    private UserWarehouseAssignment()
    {
    }

    public UserWarehouseAssignment(Guid userId, Guid warehouseId)
    {
        UserId = userId;
        WarehouseId = warehouseId;
        AssignedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
}

