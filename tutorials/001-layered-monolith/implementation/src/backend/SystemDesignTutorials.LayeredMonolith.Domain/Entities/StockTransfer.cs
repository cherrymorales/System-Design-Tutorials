using SystemDesignTutorials.LayeredMonolith.Domain.Enums;

namespace SystemDesignTutorials.LayeredMonolith.Domain.Entities;

public sealed class StockTransfer
{
    private StockTransfer()
    {
    }

    public StockTransfer(
        Guid sourceWarehouseId,
        Guid destinationWarehouseId,
        Guid productId,
        int quantity,
        string requestedBy,
        string reason)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Transfer quantity must be greater than zero.");
        }

        if (sourceWarehouseId == destinationWarehouseId)
        {
            throw new InvalidOperationException("Source and destination warehouses must be different.");
        }

        Id = Guid.NewGuid();
        SourceWarehouseId = sourceWarehouseId;
        DestinationWarehouseId = destinationWarehouseId;
        ProductId = productId;
        Quantity = quantity;
        RequestedBy = requestedBy;
        Reason = reason;
        Status = TransferStatus.Requested;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid SourceWarehouseId { get; private set; }
    public Guid DestinationWarehouseId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public TransferStatus Status { get; private set; }
    public string RequestedBy { get; private set; } = string.Empty;
    public string? ApprovedBy { get; private set; }
    public string? DispatchedBy { get; private set; }
    public string? ReceivedBy { get; private set; }
    public string? CancelledBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? DispatchedAt { get; private set; }
    public DateTimeOffset? ReceivedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string? CancellationReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void Approve(string approvedBy)
    {
        if (Status != TransferStatus.Requested)
        {
            throw new InvalidOperationException("Only requested transfers can be approved.");
        }

        Status = TransferStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTimeOffset.UtcNow;
    }

    public void Dispatch(string dispatchedBy)
    {
        if (Status != TransferStatus.Approved)
        {
            throw new InvalidOperationException("Only approved transfers can be dispatched.");
        }

        Status = TransferStatus.Dispatched;
        DispatchedBy = dispatchedBy;
        DispatchedAt = DateTimeOffset.UtcNow;
    }

    public void Receive(string receivedBy)
    {
        if (Status != TransferStatus.Dispatched)
        {
            throw new InvalidOperationException("Only dispatched transfers can be received.");
        }

        Status = TransferStatus.Received;
        ReceivedBy = receivedBy;
        ReceivedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel(string cancelledBy, string? cancellationReason)
    {
        if (Status is TransferStatus.Dispatched or TransferStatus.Received or TransferStatus.Cancelled)
        {
            throw new InvalidOperationException("Transfers can only be cancelled before dispatch.");
        }

        Status = TransferStatus.Cancelled;
        CancelledBy = cancelledBy;
        CancellationReason = cancellationReason;
        CancelledAt = DateTimeOffset.UtcNow;
    }
}
