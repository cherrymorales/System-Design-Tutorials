using SystemDesignTutorials.EventDriven.Contracts;

namespace SystemDesignTutorials.EventDriven.Contracts.Tests;

public sealed class EventContractsTests
{
    [Fact]
    public void WorkflowEventsImplementSharedMessageContract()
    {
        var eventTypes = typeof(IEventMessage).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(IEventMessage).IsAssignableFrom(type))
            .ToList();

        Assert.NotEmpty(eventTypes);
        Assert.All(eventTypes, type => Assert.True(typeof(IEventMessage).IsAssignableFrom(type)));
    }

    [Fact]
    public void AssetUploadCompletedCarriesCorrelationAndAssetIdentifiers()
    {
        var assetId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var message = new AssetUploadCompletedEvent(Guid.NewGuid(), assetId, correlationId, "ASSET-123", "Campaign hero", false, "coordinator@eventdriven.local", DateTimeOffset.UtcNow);

        Assert.Equal(assetId, message.AssetId);
        Assert.Equal(correlationId, message.CorrelationId);
        Assert.Equal("ASSET-123", message.AssetKey);
    }

    [Fact]
    public void FailureEventPreservesFailureReason()
    {
        var message = new AssetProcessingFailedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Transcode failed for unsupported codec.", DateTimeOffset.UtcNow);

        Assert.Equal("Transcode failed for unsupported codec.", message.FailureReason);
    }
}
