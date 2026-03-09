using SystemDesignTutorials.ModularMonolith.Domain.Enums;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Shared;

namespace SystemDesignTutorials.ModularMonolith.Domain.Modules.Orders;

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public Guid? ReservationId { get; private set; }
    public Guid? InvoiceId { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ReadyForInvoicingAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines;

    private Order() { }

    public Order(Guid customerId, IEnumerable<OrderLine> lines, string createdBy)
    {
        CustomerId = customerId;
        CreatedBy = Require(createdBy, nameof(createdBy), 256);
        _lines.AddRange(lines);
        if (_lines.Count == 0) throw new BusinessRuleException("Order must contain at least one line.");
    }

    public decimal TotalAmount => _lines.Sum(line => line.LineTotal);

    public void Submit()
    {
        if (Status != OrderStatus.Draft) throw new BusinessRuleException("Only draft orders can be submitted.");
        Status = OrderStatus.Submitted;
        SubmittedAt = DateTimeOffset.UtcNow;
    }

    public void AttachReservation(Guid reservationId)
    {
        if (Status != OrderStatus.Submitted) throw new BusinessRuleException("Only submitted orders can attach a reservation.");
        ReservationId = reservationId;
        Status = OrderStatus.Reserved;
    }

    public void MarkReadyForInvoicing()
    {
        if (Status != OrderStatus.Reserved) throw new BusinessRuleException("Only reserved orders can become invoice ready.");
        Status = OrderStatus.ReadyForInvoicing;
        ReadyForInvoicingAt = DateTimeOffset.UtcNow;
    }

    public void LinkInvoice(Guid invoiceId)
    {
        if (Status != OrderStatus.ReadyForInvoicing) throw new BusinessRuleException("Only invoice-ready orders can link an invoice.");
        InvoiceId = invoiceId;
    }

    public void MarkInvoiced()
    {
        if (Status != OrderStatus.ReadyForInvoicing) throw new BusinessRuleException("Only invoice-ready orders can be marked invoiced.");
        if (InvoiceId is null) throw new BusinessRuleException("Invoice reference is required before marking an order invoiced.");
        Status = OrderStatus.Invoiced;
    }

    public void Complete()
    {
        if (Status != OrderStatus.Invoiced) throw new BusinessRuleException("Only invoiced orders can be completed.");
        Status = OrderStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Completed or OrderStatus.Invoiced) throw new BusinessRuleException("Completed or invoiced orders cannot be cancelled.");
        if (Status == OrderStatus.Cancelled) throw new BusinessRuleException("Order is already cancelled.");
        Status = OrderStatus.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;
    }

    private static string Require(string value, string field, int maxLength)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) throw new BusinessRuleException($"{field} is required.");
        if (trimmed.Length > maxLength) throw new BusinessRuleException($"{field} exceeds {maxLength} characters.");
        return trimmed;
    }
}

public sealed class OrderLine
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    private OrderLine() { }

    public OrderLine(Guid productId, int quantity, decimal unitPrice)
    {
        if (quantity <= 0) throw new BusinessRuleException("Order line quantity must be greater than zero.");
        if (unitPrice <= 0) throw new BusinessRuleException("Order line unit price must be greater than zero.");
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);
    }

    public decimal LineTotal => UnitPrice * Quantity;
}
