namespace SystemDesignTutorials.ModularMonolith.Application.Modules;

public sealed record CustomerDto(
    Guid Id,
    string AccountCode,
    string Name,
    string Status,
    string BillingContactName,
    string BillingContactEmail,
    string ShippingContactName,
    string ShippingContactEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string Category,
    decimal UnitPrice,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record WarehouseDto(Guid Id, string Code, string Name, string City, string Status);

public sealed record StockItemDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    int QuantityOnHand,
    int QuantityReserved,
    int AvailableQuantity,
    DateTimeOffset UpdatedAt);

public sealed record OrderLineDto(Guid ProductId, string ProductSku, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record OrderSummaryDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string CustomerAccountCode,
    string Status,
    Guid? ReservationId,
    Guid? InvoiceId,
    decimal TotalAmount,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ReadyForInvoicingAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? CancelledAt,
    IReadOnlyList<OrderLineDto> Lines);

public sealed record InvoiceDto(
    Guid Id,
    Guid OrderId,
    Guid CustomerId,
    string CustomerName,
    string InvoiceNumber,
    string Status,
    decimal TotalAmount,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? IssuedAt,
    string? IssuedBy,
    DateTimeOffset? PaidAt,
    string? PaidBy);

public sealed record ReportSummaryDto(
    int TotalCustomers,
    int ActiveProducts,
    int DraftOrders,
    int ReservedOrders,
    int ReadyForInvoicingOrders,
    int IssuedInvoices,
    int PaidInvoices,
    decimal TotalReservedValue,
    decimal TotalPaidValue,
    IReadOnlyList<LowStockDto> LowStockItems,
    IReadOnlyList<ModuleHealthDto> ModuleHealth);

public sealed record LowStockDto(
    Guid StockItemId,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    int QuantityOnHand,
    int QuantityReserved,
    int AvailableQuantity,
    int ReorderThreshold);

public sealed record ModuleHealthDto(string ModuleName, string Summary, string Status);

public sealed record CustomerValidationDto(Guid Id, bool IsActive, string Name, string AccountCode);
public sealed record ProductValidationDto(Guid Id, bool IsActive, string Sku, string Name, decimal UnitPrice);
public sealed record OrderBillingSnapshotDto(Guid OrderId, Guid CustomerId, string CustomerName, decimal TotalAmount, Guid? InvoiceId, string Status);
public sealed record InvoiceStatusSnapshotDto(Guid InvoiceId, string Status, bool IsPaid);
public sealed record ReservationItemCommand(Guid ProductId, int Quantity);
public sealed record ReservationResultDto(Guid ReservationId, string Status, DateTimeOffset ReservedAt);

public sealed record CreateCustomerCommand(string AccountCode, string Name, string BillingContactName, string BillingContactEmail, string ShippingContactName, string ShippingContactEmail);
public sealed record UpdateCustomerCommand(string Name, string BillingContactName, string BillingContactEmail, string ShippingContactName, string ShippingContactEmail, bool IsActive);
public sealed record CreateProductCommand(string Sku, string Name, string Category, decimal UnitPrice);
public sealed record UpdateProductCommand(string Name, string Category, decimal UnitPrice, bool IsActive);
public sealed record CreateOrderLineCommand(Guid ProductId, int Quantity);
public sealed record CreateOrderCommand(Guid CustomerId, IReadOnlyList<CreateOrderLineCommand> Lines);
public sealed record CreateInvoiceDraftCommand(Guid OrderId, Guid CustomerId, string CustomerName, decimal TotalAmount);

public interface ICustomersModule
{
    Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken);
    Task<CustomerDto?> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerCommand command, CancellationToken cancellationToken);
    Task<CustomerDto> UpdateCustomerAsync(Guid customerId, UpdateCustomerCommand command, CancellationToken cancellationToken);
    Task<CustomerValidationDto> GetCustomerValidationAsync(Guid customerId, CancellationToken cancellationToken);
}

public interface ICatalogModule
{
    Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken);
    Task<ProductDto?> GetProductAsync(Guid productId, CancellationToken cancellationToken);
    Task<ProductDto> CreateProductAsync(CreateProductCommand command, CancellationToken cancellationToken);
    Task<ProductDto> UpdateProductAsync(Guid productId, UpdateProductCommand command, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, ProductValidationDto>> GetProductValidationMapAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken);
}

public interface IInventoryModule
{
    Task<IReadOnlyList<WarehouseDto>> GetWarehousesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<StockItemDto>> GetStockAsync(CancellationToken cancellationToken);
    Task<ReservationResultDto> ReserveOrderAsync(Guid orderId, IReadOnlyList<ReservationItemCommand> items, string actor, CancellationToken cancellationToken);
    Task ReleaseReservationAsync(Guid reservationId, string actor, CancellationToken cancellationToken);
    Task CommitReservationAsync(Guid reservationId, string actor, CancellationToken cancellationToken);
}

public interface IOrdersModule
{
    Task<IReadOnlyList<OrderSummaryDto>> GetOrdersAsync(CancellationToken cancellationToken);
    Task<OrderSummaryDto?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken);
    Task<OrderSummaryDto> CreateOrderAsync(CreateOrderCommand command, string actor, CancellationToken cancellationToken);
    Task<OrderSummaryDto> SubmitOrderAsync(Guid orderId, CancellationToken cancellationToken);
    Task<OrderSummaryDto> ReserveOrderAsync(Guid orderId, string actor, CancellationToken cancellationToken);
    Task<OrderSummaryDto> MarkReadyForInvoicingAsync(Guid orderId, string actor, CancellationToken cancellationToken);
    Task<OrderBillingSnapshotDto> GetOrderForInvoiceAsync(Guid orderId, CancellationToken cancellationToken);
    Task LinkInvoiceAsync(Guid orderId, Guid invoiceId, CancellationToken cancellationToken);
    Task MarkOrderInvoicedAsync(Guid orderId, CancellationToken cancellationToken);
    Task<OrderSummaryDto> CompleteOrderAsync(Guid orderId, string actor, CancellationToken cancellationToken);
    Task<OrderSummaryDto> CancelOrderAsync(Guid orderId, string actor, CancellationToken cancellationToken);
}

public interface IBillingModule
{
    Task<IReadOnlyList<InvoiceDto>> GetInvoicesAsync(CancellationToken cancellationToken);
    Task<InvoiceDto> CreateDraftAsync(CreateInvoiceDraftCommand command, string actor, CancellationToken cancellationToken);
    Task<InvoiceDto> IssueInvoiceAsync(Guid invoiceId, string actor, CancellationToken cancellationToken);
    Task<InvoiceDto> MarkPaidAsync(Guid invoiceId, string actor, CancellationToken cancellationToken);
    Task<InvoiceStatusSnapshotDto?> GetInvoiceStatusAsync(Guid invoiceId, CancellationToken cancellationToken);
}

public interface IReportingModule
{
    Task<ReportSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
}
