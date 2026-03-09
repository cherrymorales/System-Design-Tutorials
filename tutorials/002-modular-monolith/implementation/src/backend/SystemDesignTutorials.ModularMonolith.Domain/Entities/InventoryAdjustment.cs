using SystemDesignTutorials.ModularMonolith.Domain.Enums;

namespace SystemDesignTutorials.ModularMonolith.Domain.Entities;

public sealed class InventoryAdjustment
{
    private InventoryAdjustment()
    {
    }

    public InventoryAdjustment(
        Guid warehouseId,
        Guid productId,
        int quantityDelta,
        string reasonCode,
        string submittedBy,
        string? notes)
    {
        if (quantityDelta == 0)
        {
            throw new InvalidOperationException("Adjustment quantity delta cannot be zero.");
        }

        Id = Guid.NewGuid();
        WarehouseId = warehouseId;
        ProductId = productId;
        QuantityDelta = quantityDelta;
        ReasonCode = reasonCode;
        SubmittedBy = submittedBy;
        Notes = notes;
        Status = AdjustmentStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid ProductId { get; private set; }
    public int QuantityDelta { get; private set; }
    public string ReasonCode { get; private set; } = string.Empty;
    public AdjustmentStatus Status { get; private set; }
    public string SubmittedBy { get; private set; } = string.Empty;
    public DateTimeOffset? SubmittedAt { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public string? RejectedBy { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public bool RequiresApproval { get; private set; }

    public void Submit(decimal unitCost)
    {
        if (Status != AdjustmentStatus.Draft)
        {
            throw new InvalidOperationException("Only draft adjustments can be submitted.");
        }

        var valueImpact = Math.Abs(QuantityDelta) * unitCost;
        RequiresApproval = Math.Abs(QuantityDelta) > 10 || valueImpact > 1000m;
        SubmittedAt = DateTimeOffset.UtcNow;

        if (RequiresApproval)
        {
            Status = AdjustmentStatus.PendingApproval;
            return;
        }

        Status = AdjustmentStatus.Approved;
        ApprovedBy = SubmittedBy;
        ApprovedAt = SubmittedAt;
    }

    public void Approve(string approvedBy, string? notes)
    {
        if (Status != AdjustmentStatus.PendingApproval)
        {
            throw new InvalidOperationException("Only pending adjustments can be approved.");
        }

        Status = AdjustmentStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTimeOffset.UtcNow;
        Notes = notes;
    }

    public void Reject(string rejectedBy, string? notes)
    {
        if (Status != AdjustmentStatus.PendingApproval)
        {
            throw new InvalidOperationException("Only pending adjustments can be rejected.");
        }

        Status = AdjustmentStatus.Rejected;
        RejectedBy = rejectedBy;
        RejectedAt = DateTimeOffset.UtcNow;
        Notes = notes;
    }
}

