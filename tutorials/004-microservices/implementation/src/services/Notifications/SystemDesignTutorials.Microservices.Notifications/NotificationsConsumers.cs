using MassTransit;
using SystemDesignTutorials.Microservices.Contracts;

namespace SystemDesignTutorials.Microservices.Notifications;

public sealed class NotificationRequestedConsumer(NotificationsService service) : IConsumer<NotificationRequestedIntegrationEvent>
{
    public Task Consume(ConsumeContext<NotificationRequestedIntegrationEvent> context)
        => service.RecordAsync(context.Message, context.CancellationToken);
}
