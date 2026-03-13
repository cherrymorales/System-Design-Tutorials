using MassTransit;
using SystemDesignTutorials.Microservices.Contracts;

namespace SystemDesignTutorials.Microservices.Fulfillment;

public sealed class OrderReadyForFulfillmentConsumer(FulfillmentService service, IPublishEndpoint publishEndpoint) : IConsumer<OrderReadyForFulfillmentIntegrationEvent>
{
    public Task Consume(ConsumeContext<OrderReadyForFulfillmentIntegrationEvent> context)
        => service.HandleReadyForFulfillmentAsync(context.Message, publishEndpoint, context.CancellationToken);
}
