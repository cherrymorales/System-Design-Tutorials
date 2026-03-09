using SystemDesignTutorials.ModularMonolith.Domain.Enums;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Shared;

namespace SystemDesignTutorials.ModularMonolith.Domain.Modules.Customers;

public sealed class Customer
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string AccountCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public CustomerStatus Status { get; private set; } = CustomerStatus.Active;
    public string BillingContactName { get; private set; } = string.Empty;
    public string BillingContactEmail { get; private set; } = string.Empty;
    public string ShippingContactName { get; private set; } = string.Empty;
    public string ShippingContactEmail { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Customer() { }

    public Customer(string accountCode, string name, string billingContactName, string billingContactEmail, string shippingContactName, string shippingContactEmail)
    {
        AccountCode = Require(accountCode, nameof(accountCode), 32);
        Name = Require(name, nameof(name), 200);
        BillingContactName = Require(billingContactName, nameof(billingContactName), 200);
        BillingContactEmail = Require(billingContactEmail, nameof(billingContactEmail), 200);
        ShippingContactName = Require(shippingContactName, nameof(shippingContactName), 200);
        ShippingContactEmail = Require(shippingContactEmail, nameof(shippingContactEmail), 200);
    }

    public bool IsActive => Status == CustomerStatus.Active;

    public void Update(string name, string billingContactName, string billingContactEmail, string shippingContactName, string shippingContactEmail, bool isActive)
    {
        Name = Require(name, nameof(name), 200);
        BillingContactName = Require(billingContactName, nameof(billingContactName), 200);
        BillingContactEmail = Require(billingContactEmail, nameof(billingContactEmail), 200);
        ShippingContactName = Require(shippingContactName, nameof(shippingContactName), 200);
        ShippingContactEmail = Require(shippingContactEmail, nameof(shippingContactEmail), 200);
        Status = isActive ? CustomerStatus.Active : CustomerStatus.Inactive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string Require(string value, string field, int maxLength)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) throw new BusinessRuleException($"{field} is required.");
        if (trimmed.Length > maxLength) throw new BusinessRuleException($"{field} exceeds {maxLength} characters.");
        return trimmed;
    }
}
