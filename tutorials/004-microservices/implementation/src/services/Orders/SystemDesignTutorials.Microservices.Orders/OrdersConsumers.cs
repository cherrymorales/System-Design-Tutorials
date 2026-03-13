using MassTransit;
using SystemDesignTutorials.Microservices.Contracts;

namespace SystemDesignTutorials.Microservices.Orders;

public sealed class InventoryReservedConsumer(OrdersService service, IPublishEndpoint publishEndpoint) : IConsumer<InventoryReservedIntegrationEvent>
{
    public Task Consume(ConsumeContext<InventoryReservedIntegrationEvent> context)
        => service.HandleInventoryReservedAsync(context.Message, publishEndpoint, context.CancellationToken);
}

public sealed class InventoryRejectedConsumer(OrdersService service, IPublishEndpoint publishEndpoint) : IConsumer<InventoryReservationRejectedIntegrationEvent>
{
    public Task Consume(ConsumeContext<InventoryReservationRejectedIntegrationEvent> context)
        => service.HandleInventoryRejectedAsync(context.Message, publishEndpoint, context.CancellationToken);
}

public sealed class PaymentAuthorizedConsumer(OrdersService service, IPublishEndpoint publishEndpoint) : IConsumer<PaymentAuthorizedIntegrationEvent>
{
    public Task Consume(ConsumeContext<PaymentAuthorizedIntegrationEvent> context)
        => service.HandlePaymentAuthorizedAsync(context.Message, publishEndpoint, context.CancellationToken);
}

public sealed class PaymentFailedConsumer(OrdersService service, IPublishEndpoint publishEndpoint) : IConsumer<PaymentAuthorizationFailedIntegrationEvent>
{
    public Task Consume(ConsumeContext<PaymentAuthorizationFailedIntegrationEvent> context)
        => service.HandlePaymentFailedAsync(context.Message, publishEndpoint, context.CancellationToken);
}

public sealed class ShipmentCreatedConsumer(OrdersService service) : IConsumer<ShipmentCreatedIntegrationEvent>
{
    public Task Consume(ConsumeContext<ShipmentCreatedIntegrationEvent> context)
        => service.HandleShipmentCreatedAsync(context.Message, context.CancellationToken);
}

public sealed class ShipmentDeliveredConsumer(OrdersService service) : IConsumer<ShipmentDeliveredIntegrationEvent>
{
    public Task Consume(ConsumeContext<ShipmentDeliveredIntegrationEvent> context)
        => service.HandleShipmentDeliveredAsync(context.Message, context.CancellationToken);
}
