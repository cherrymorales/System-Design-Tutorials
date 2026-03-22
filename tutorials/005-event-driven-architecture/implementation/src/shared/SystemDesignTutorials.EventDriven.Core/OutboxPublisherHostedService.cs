using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SystemDesignTutorials.EventDriven.Core;

public sealed class OutboxPublisherHostedService(
    IServiceProvider serviceProvider,
    ILogger<OutboxPublisherHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Outbox publish cycle failed.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(350), stoppingToken);
        }
    }

    private async Task PublishPendingMessagesAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventDrivenDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var pendingMessages = await dbContext.OutboxMessages
            .Where(message => message.PublishedAt == null)
            .OrderBy(message => message.CreatedAt)
            .Take(25)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
        {
            return;
        }

        foreach (var message in pendingMessages)
        {
            var deserialized = OutboxSerialization.Deserialize(message);
            await publishEndpoint.Publish(deserialized, deserialized.GetType(), cancellationToken);
            message.PublishedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
