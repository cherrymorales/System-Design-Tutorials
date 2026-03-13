using MassTransit;
using SystemDesignTutorials.Microservices.Contracts;

namespace SystemDesignTutorials.Microservices.Inventory;

public sealed class OrderSubmittedConsumer(InventoryService service, IPublishEndpoint publishEndpoint) : IConsumer<OrderSubmittedIntegrationEvent>
{
    public Task Consume(ConsumeContext<OrderSubmittedIntegrationEvent> context)
        => service.HandleOrderSubmittedAsync(context.Message, publishEndpoint, context.CancellationToken);
}

public sealed class ReservationReleaseRequestedConsumer(InventoryService service, IPublishEndpoint publishEndpoint) : IConsumer<ReservationReleaseRequestedIntegrationEvent>
{
    public Task Consume(ConsumeContext<ReservationReleaseRequestedIntegrationEvent> context)
        => service.HandleReleaseRequestedAsync(context.Message, publishEndpoint, context.CancellationToken);
}
