namespace SystemDesignTutorials.Microservices.Contracts;

public sealed record SubmittedOrderLine(string Sku, string ProductName, int Quantity, decimal UnitPrice);

public sealed record OrderSubmittedIntegrationEvent(
    Guid OrderId,
    string OrderNumber,
    string CustomerReference,
    string Currency,
    decimal TotalAmount,
    string RequestedByEmail,
    IReadOnlyCollection<SubmittedOrderLine> Lines,
    DateTimeOffset SubmittedAt);

public sealed record InventoryReservedIntegrationEvent(
    Guid OrderId,
    Guid ReservationId,
    string WarehouseCode,
    DateTimeOffset ReservedAt);

public sealed record InventoryReservationRejectedIntegrationEvent(
    Guid OrderId,
    string Reason,
    DateTimeOffset RejectedAt);

public sealed record ReservationReleaseRequestedIntegrationEvent(
    Guid OrderId,
    string Reason,
    DateTimeOffset RequestedAt);

public sealed record ReservationReleasedIntegrationEvent(
    Guid OrderId,
    Guid ReservationId,
    DateTimeOffset ReleasedAt);

public sealed record PaymentAuthorizedIntegrationEvent(
    Guid OrderId,
    Guid PaymentId,
    decimal Amount,
    string AuthorizationReference,
    DateTimeOffset AuthorizedAt);

public sealed record PaymentAuthorizationFailedIntegrationEvent(
    Guid OrderId,
    decimal Amount,
    string Reason,
    DateTimeOffset FailedAt);

public sealed record PaymentVoidRequestedIntegrationEvent(
    Guid OrderId,
    string Reason,
    DateTimeOffset RequestedAt);

public sealed record PaymentVoidedIntegrationEvent(
    Guid OrderId,
    Guid PaymentId,
    DateTimeOffset VoidedAt);

public sealed record OrderReadyForFulfillmentIntegrationEvent(
    Guid OrderId,
    string OrderNumber,
    DateTimeOffset ReadyAt);

public sealed record OrderSubmissionFailedIntegrationEvent(
    Guid OrderId,
    string Reason,
    DateTimeOffset FailedAt);

public sealed record ShipmentCreatedIntegrationEvent(
    Guid OrderId,
    Guid ShipmentId,
    string OrderNumber,
    string WarehouseCode,
    string TrackingReference,
    DateTimeOffset CreatedAt);

public sealed record ShipmentStatusChangedIntegrationEvent(
    Guid OrderId,
    Guid ShipmentId,
    ShipmentStatus Status,
    DateTimeOffset ChangedAt);

public sealed record ShipmentDeliveredIntegrationEvent(
    Guid OrderId,
    Guid ShipmentId,
    DateTimeOffset DeliveredAt);

public sealed record NotificationRequestedIntegrationEvent(
    Guid OrderId,
    string Category,
    string Message,
    DateTimeOffset RequestedAt);
