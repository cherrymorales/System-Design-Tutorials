using MassTransit;
using SystemDesignTutorials.Microservices.Contracts;

namespace SystemDesignTutorials.Microservices.Payments;

public sealed class OrderSubmittedConsumer(PaymentsService service, IPublishEndpoint publishEndpoint) : IConsumer<OrderSubmittedIntegrationEvent>
{
    public Task Consume(ConsumeContext<OrderSubmittedIntegrationEvent> context)
        => service.HandleOrderSubmittedAsync(context.Message, publishEndpoint, context.CancellationToken);
}

public sealed class PaymentVoidRequestedConsumer(PaymentsService service, IPublishEndpoint publishEndpoint) : IConsumer<PaymentVoidRequestedIntegrationEvent>
{
    public Task Consume(ConsumeContext<PaymentVoidRequestedIntegrationEvent> context)
        => service.HandleVoidRequestedAsync(context.Message, publishEndpoint, context.CancellationToken);
}
