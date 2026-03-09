using SystemDesignTutorials.ModularMonolith.Domain.Enums;

namespace SystemDesignTutorials.ModularMonolith.Domain.Entities;

public sealed class Product
{
    private Product()
    {
    }

    public Product(string sku, string name, string category, string supplierCode, decimal unitCost)
    {
        Id = Guid.NewGuid();
        Sku = sku;
        Name = name;
        Category = category;
        SupplierCode = supplierCode;
        UnitCost = unitCost;
        Status = ProductStatus.Active;
    }

    public Guid Id { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string SupplierCode { get; private set; } = string.Empty;
    public decimal UnitCost { get; private set; }
    public ProductStatus Status { get; private set; }

    public void UpdateDetails(string name, string category, string supplierCode, decimal unitCost)
    {
        Name = name;
        Category = category;
        SupplierCode = supplierCode;
        UnitCost = unitCost;
    }

    public void Archive()
    {
        Status = ProductStatus.Archived;
    }
}

