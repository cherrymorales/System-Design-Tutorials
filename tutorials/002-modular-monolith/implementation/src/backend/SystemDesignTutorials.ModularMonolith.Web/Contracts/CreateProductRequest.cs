namespace SystemDesignTutorials.ModularMonolith.Web.Contracts;

public sealed record CreateProductRequest(
    string Sku,
    string Name,
    string Category,
    string SupplierCode,
    decimal UnitCost);

