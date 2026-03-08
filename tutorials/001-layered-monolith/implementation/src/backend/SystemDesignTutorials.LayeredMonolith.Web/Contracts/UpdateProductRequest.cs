namespace SystemDesignTutorials.LayeredMonolith.Web.Contracts;

public sealed record UpdateProductRequest(
    string Name,
    string Category,
    string SupplierCode,
    decimal UnitCost);
