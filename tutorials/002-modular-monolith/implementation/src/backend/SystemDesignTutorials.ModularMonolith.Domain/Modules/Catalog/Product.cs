using SystemDesignTutorials.ModularMonolith.Domain.Enums;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Shared;

namespace SystemDesignTutorials.ModularMonolith.Domain.Modules.Catalog;

public sealed class Product
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public ProductStatus Status { get; private set; } = ProductStatus.Active;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Product() { }

    public Product(string sku, string name, string category, decimal unitPrice)
    {
        Sku = Require(sku, nameof(sku), 64);
        Name = Require(name, nameof(name), 200);
        Category = Require(category, nameof(category), 128);
        UnitPrice = RequirePrice(unitPrice);
    }

    public bool IsActive => Status == ProductStatus.Active;

    public void Update(string name, string category, decimal unitPrice, bool isActive)
    {
        Name = Require(name, nameof(name), 200);
        Category = Require(category, nameof(category), 128);
        UnitPrice = RequirePrice(unitPrice);
        Status = isActive ? ProductStatus.Active : ProductStatus.Archived;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string Require(string value, string field, int maxLength)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) throw new BusinessRuleException($"{field} is required.");
        if (trimmed.Length > maxLength) throw new BusinessRuleException($"{field} exceeds {maxLength} characters.");
        return trimmed;
    }

    private static decimal RequirePrice(decimal value)
    {
        if (value <= 0) throw new BusinessRuleException("Unit price must be greater than zero.");
        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
