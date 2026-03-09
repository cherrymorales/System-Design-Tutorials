using SystemDesignTutorials.ModularMonolith.Domain.Enums;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Shared;

namespace SystemDesignTutorials.ModularMonolith.Domain.Modules.Billing;

public sealed class Invoice
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Draft;
    public decimal TotalAmount { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? IssuedAt { get; private set; }
    public string? IssuedBy { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public string? PaidBy { get; private set; }

    private Invoice() { }

    public Invoice(Guid orderId, Guid customerId, string invoiceNumber, decimal totalAmount, string createdBy)
    {
        if (totalAmount <= 0) throw new BusinessRuleException("Invoice total amount must be greater than zero.");
        OrderId = orderId;
        CustomerId = customerId;
        InvoiceNumber = Require(invoiceNumber, nameof(invoiceNumber), 32);
        TotalAmount = decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero);
        CreatedBy = Require(createdBy, nameof(createdBy), 256);
    }

    public void Issue(string actor)
    {
        if (Status != InvoiceStatus.Draft) throw new BusinessRuleException("Only draft invoices can be issued.");
        Status = InvoiceStatus.Issued;
        IssuedAt = DateTimeOffset.UtcNow;
        IssuedBy = Require(actor, nameof(actor), 256);
    }

    public void MarkPaid(string actor)
    {
        if (Status != InvoiceStatus.Issued) throw new BusinessRuleException("Only issued invoices can be marked paid.");
        Status = InvoiceStatus.Paid;
        PaidAt = DateTimeOffset.UtcNow;
        PaidBy = Require(actor, nameof(actor), 256);
    }

    private static string Require(string value, string field, int maxLength)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) throw new BusinessRuleException($"{field} is required.");
        if (trimmed.Length > maxLength) throw new BusinessRuleException($"{field} exceeds {maxLength} characters.");
        return trimmed;
    }
}
