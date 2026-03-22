using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SystemDesignTutorials.EventDriven.Contracts;

namespace SystemDesignTutorials.EventDriven.Core;

public sealed class MessagingOptions
{
    public string Transport { get; set; } = "RabbitMq";
    public string Host { get; set; } = "localhost";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    public bool UseInMemory => string.Equals(Transport, "InMemory", StringComparison.OrdinalIgnoreCase);
}

public interface IDatabaseSeeder
{
    Task SeedAsync(EventDrivenDbContext dbContext, CancellationToken cancellationToken);
}

public sealed class NoOpSeeder : IDatabaseSeeder
{
    public Task SeedAsync(EventDrivenDbContext dbContext, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class DatabaseBootstrapHostedService(
    IServiceProvider serviceProvider,
    ILogger<DatabaseBootstrapHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 12;

        for (var attempt = 1; attempt <= maxAttempts; attempt += 1)
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<EventDrivenDbContext>();
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
                await seeder.SeedAsync(dbContext, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    ex,
                    "Database bootstrap attempt {Attempt}/{MaxAttempts} failed for EventDrivenDbContext. Retrying in 5 seconds.",
                    attempt,
                    maxAttempts);

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        throw new InvalidOperationException("Database bootstrap failed for EventDrivenDbContext.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public static class ProjectionMappings
{
    public static AssetSummaryDto ToSummaryDto(this AssetProjection asset)
        => new(
            asset.AssetId,
            asset.AssetKey,
            asset.Title,
            asset.LifecycleState,
            asset.ScanStatus,
            asset.MetadataStatus,
            asset.ThumbnailStatus,
            asset.TranscodeStatus,
            asset.FailureReason,
            asset.UpdatedAt);

    public static AssetDetailDto ToDetailDto(this AssetProjection asset)
        => new(
            asset.AssetId,
            asset.AssetKey,
            asset.Title,
            asset.LifecycleState,
            asset.ScanStatus,
            asset.MetadataStatus,
            asset.ThumbnailStatus,
            asset.TranscodeStatus,
            asset.SimulateFailure,
            asset.SubmittedBy,
            asset.CreatedAt,
            asset.UpdatedAt,
            asset.ReadyAt,
            asset.FailureReason);
}

public static class OutboxSerialization
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static OutboxMessage CreateMessage<TMessage>(TMessage message, DateTimeOffset timestamp)
        where TMessage : class
        => new()
        {
            OutboxMessageId = Guid.NewGuid(),
            Type = typeof(TMessage).AssemblyQualifiedName ?? throw new InvalidOperationException("Missing event type metadata."),
            Payload = JsonSerializer.Serialize(message, SerializerOptions),
            CreatedAt = timestamp,
        };

    public static object Deserialize(OutboxMessage message)
    {
        var messageType = Type.GetType(message.Type, throwOnError: true)
            ?? throw new InvalidOperationException($"Unable to resolve outbox type '{message.Type}'.");

        return JsonSerializer.Deserialize(message.Payload, messageType, SerializerOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize outbox payload for '{message.Type}'.");
    }
}
