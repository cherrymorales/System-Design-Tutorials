namespace SystemDesignTutorials.ModularMonolith.Web.Endpoints;

public sealed record LoginRequest(string Email, string Password);
public sealed record CreateCustomerRequest(string AccountCode, string Name, string BillingContactName, string BillingContactEmail, string ShippingContactName, string ShippingContactEmail);
public sealed record UpdateCustomerRequest(string Name, string BillingContactName, string BillingContactEmail, string ShippingContactName, string ShippingContactEmail, bool IsActive);
public sealed record CreateProductRequest(string Sku, string Name, string Category, decimal UnitPrice);
public sealed record UpdateProductRequest(string Name, string Category, decimal UnitPrice, bool IsActive);
public sealed record CreateOrderLineRequest(Guid ProductId, int Quantity);
public sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyList<CreateOrderLineRequest> Lines);
public sealed record CreateInvoiceDraftRequest(Guid OrderId);
