using MassTransit;
using SystemDesignTutorials.Microservices.Contracts;

namespace SystemDesignTutorials.Microservices.OperationsQuery;

public sealed class OrderSubmittedProjectionConsumer(OperationsQueryService service) : IConsumer<OrderSubmittedIntegrationEvent>
{
    public Task Consume(ConsumeContext<OrderSubmittedIntegrationEvent> context)
        => service.ApplyOrderSubmittedAsync(context.Message, context.CancellationToken);
}

public sealed class InventoryReservedProjectionConsumer(OperationsQueryService service) : IConsumer<InventoryReservedIntegrationEvent>
{
    public Task Consume(ConsumeContext<InventoryReservedIntegrationEvent> context)
        => service.ApplyInventoryReservedAsync(context.Message, context.CancellationToken);
}

public sealed class InventoryRejectedProjectionConsumer(OperationsQueryService service) : IConsumer<InventoryReservationRejectedIntegrationEvent>
{
    public Task Consume(ConsumeContext<InventoryReservationRejectedIntegrationEvent> context)
        => service.ApplyInventoryRejectedAsync(context.Message, context.CancellationToken);
}

public sealed class PaymentAuthorizedProjectionConsumer(OperationsQueryService service) : IConsumer<PaymentAuthorizedIntegrationEvent>
{
    public Task Consume(ConsumeContext<PaymentAuthorizedIntegrationEvent> context)
        => service.ApplyPaymentAuthorizedAsync(context.Message, context.CancellationToken);
}

public sealed class PaymentFailedProjectionConsumer(OperationsQueryService service) : IConsumer<PaymentAuthorizationFailedIntegrationEvent>
{
    public Task Consume(ConsumeContext<PaymentAuthorizationFailedIntegrationEvent> context)
        => service.ApplyPaymentFailedAsync(context.Message, context.CancellationToken);
}

public sealed class OrderReadyProjectionConsumer(OperationsQueryService service) : IConsumer<OrderReadyForFulfillmentIntegrationEvent>
{
    public Task Consume(ConsumeContext<OrderReadyForFulfillmentIntegrationEvent> context)
        => service.ApplyReadyAsync(context.Message, context.CancellationToken);
}

public sealed class OrderFailedProjectionConsumer(OperationsQueryService service) : IConsumer<OrderSubmissionFailedIntegrationEvent>
{
    public Task Consume(ConsumeContext<OrderSubmissionFailedIntegrationEvent> context)
        => service.ApplyFailedAsync(context.Message, context.CancellationToken);
}

public sealed class ShipmentCreatedProjectionConsumer(OperationsQueryService service) : IConsumer<ShipmentCreatedIntegrationEvent>
{
    public Task Consume(ConsumeContext<ShipmentCreatedIntegrationEvent> context)
        => service.ApplyShipmentCreatedAsync(context.Message, context.CancellationToken);
}

public sealed class ShipmentStatusChangedProjectionConsumer(OperationsQueryService service) : IConsumer<ShipmentStatusChangedIntegrationEvent>
{
    public Task Consume(ConsumeContext<ShipmentStatusChangedIntegrationEvent> context)
        => service.ApplyShipmentStatusChangedAsync(context.Message, context.CancellationToken);
}
