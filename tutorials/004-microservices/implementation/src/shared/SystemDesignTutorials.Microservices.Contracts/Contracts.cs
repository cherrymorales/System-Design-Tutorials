namespace SystemDesignTutorials.Microservices.Contracts;

public static class ServiceRoles
{
    public const string CatalogManager = "CatalogManager";
    public const string OrderOpsAgent = "OrderOpsAgent";
    public const string InventoryCoordinator = "InventoryCoordinator";
    public const string FinanceReviewer = "FinanceReviewer";
    public const string FulfillmentOperator = "FulfillmentOperator";
    public const string OperationsManager = "OperationsManager";

    public static readonly string[] All =
    [
        CatalogManager,
        OrderOpsAgent,
        InventoryCoordinator,
        FinanceReviewer,
        FulfillmentOperator,
        OperationsManager,
    ];
}

public enum OrderStatus
{
    Draft,
    AwaitingDependencies,
    ReadyForFulfillment,
    FulfillmentInProgress,
    Completed,
    Cancelled,
    Failed,
}

public enum ReservationStatus
{
    Pending,
    Reserved,
    Rejected,
    Released,
}

public enum PaymentStatus
{
    Pending,
    Authorized,
    Failed,
    Voided,
}

public enum ShipmentStatus
{
    Pending,
    Picking,
    Packed,
    Shipped,
    Delivered,
    Cancelled,
}

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(bool Succeeded, CurrentUserDto? User, string? ErrorMessage);

public sealed record CurrentUserDto(Guid UserId, string Email, string DisplayName, string Role);

public sealed record SeedUserDto(string Email, string DisplayName, string Role);

public sealed record ProductDto(
    string Sku,
    string Name,
    string Category,
    decimal UnitPrice,
    bool IsSellable,
    string OperationalStatus);

public sealed record AvailabilityDto(
    string Sku,
    int AvailableQuantity,
    int ReservedQuantity,
    IReadOnlyCollection<AvailabilityByWarehouseDto> Warehouses);

public sealed record AvailabilityByWarehouseDto(string WarehouseCode, int AvailableQuantity, int ReservedQuantity);

public sealed record CreateOrderRequest(string CustomerReference, string Currency, IReadOnlyCollection<CreateOrderLineRequest> Lines);

public sealed record CreateOrderLineRequest(string Sku, int Quantity);

public sealed record InternalCreateOrderRequest(
    string CustomerReference,
    string Currency,
    IReadOnlyCollection<InternalCreateOrderLineRequest> Lines,
    Guid CreatedByUserId,
    string CreatedByEmail);

public sealed record InternalCreateOrderLineRequest(string Sku, string ProductName, int Quantity, decimal UnitPrice);

public sealed record OrderSummaryDto(
    Guid OrderId,
    string OrderNumber,
    string CustomerReference,
    OrderStatus Status,
    ReservationStatus ReservationStatus,
    PaymentStatus PaymentStatus,
    ShipmentStatus? ShipmentStatus,
    decimal TotalAmount,
    DateTimeOffset UpdatedAt);

public sealed record OrderLineDto(string Sku, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record OrderDetailDto(
    Guid OrderId,
    string OrderNumber,
    string CustomerReference,
    string Currency,
    OrderStatus Status,
    ReservationStatus ReservationStatus,
    PaymentStatus PaymentStatus,
    ShipmentStatus? ShipmentStatus,
    decimal TotalAmount,
    string CreatedByEmail,
    string? FailureReason,
    Guid? ShipmentId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<OrderLineDto> Lines);

public sealed record ShipmentDto(
    Guid ShipmentId,
    Guid OrderId,
    string OrderNumber,
    ShipmentStatus Status,
    string WarehouseCode,
    string TrackingReference,
    DateTimeOffset UpdatedAt);

public sealed record DashboardSummaryDto(
    int TotalOrders,
    int AwaitingDependencies,
    int ReadyForFulfillment,
    int FulfillmentInProgress,
    int Failed,
    int Completed,
    IReadOnlyCollection<OrderSummaryDto> RecentOrders);

public sealed record NotificationDto(Guid NotificationId, Guid OrderId, string Category, string Message, DateTimeOffset CreatedAt);
