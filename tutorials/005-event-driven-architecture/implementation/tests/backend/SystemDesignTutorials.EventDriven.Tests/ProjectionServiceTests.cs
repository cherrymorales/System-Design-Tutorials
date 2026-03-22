using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.EventDriven.Contracts;
using SystemDesignTutorials.EventDriven.Core;

namespace SystemDesignTutorials.EventDriven.Tests;

public sealed class ProjectionServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private EventDrivenDbContext _dbContext = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _dbContext = CreateDbContext();
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task MarkStepCompletedOnlyTransitionsToReadyAfterAllRequiredStagesSucceed()
    {
        var assetId = await SeedAssetAsync();
        var service = new ProjectionService(_dbContext);

        var scanResult = await service.MarkStepCompletedAsync(assetId, "scan", CancellationToken.None);
        var metadataResult = await service.MarkStepCompletedAsync(assetId, "metadata", CancellationToken.None);
        var thumbnailResult = await service.MarkStepCompletedAsync(assetId, "thumbnail", CancellationToken.None);
        var transcodeResult = await service.MarkStepCompletedAsync(assetId, "transcode", CancellationToken.None);

        var projection = await _dbContext.AssetProjections.SingleAsync(asset => asset.AssetId == assetId);
        var asset = await _dbContext.Assets.SingleAsync(item => item.AssetId == assetId);

        Assert.False(scanResult.TransitionedToReady);
        Assert.False(metadataResult.TransitionedToReady);
        Assert.False(thumbnailResult.TransitionedToReady);
        Assert.True(transcodeResult.TransitionedToReady);
        Assert.Equal(AssetLifecycleState.Ready, projection.LifecycleState);
        Assert.Equal(AssetLifecycleState.Ready, asset.LifecycleState);
        Assert.NotNull(projection.ReadyAt);
    }

    [Fact]
    public async Task MarkFailedMovesProjectionAndAssetToFailedState()
    {
        var assetId = await SeedAssetAsync();
        var service = new ProjectionService(_dbContext);

        var result = await service.MarkFailedAsync(assetId, "Transcode worker rejected the stream.", CancellationToken.None);

        var projection = await _dbContext.AssetProjections.SingleAsync(asset => asset.AssetId == assetId);
        var asset = await _dbContext.Assets.SingleAsync(item => item.AssetId == assetId);

        Assert.Equal("Transcode worker rejected the stream.", result.FailureReason);
        Assert.Equal(AssetLifecycleState.Failed, projection.LifecycleState);
        Assert.Equal(ProcessingStepStatus.Failed, projection.TranscodeStatus);
        Assert.Equal(AssetLifecycleState.Failed, asset.LifecycleState);
        Assert.Equal("Transcode worker rejected the stream.", asset.FailureReason);
    }

    private EventDrivenDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EventDrivenDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new EventDrivenDbContext(options);
    }

    private async Task<Guid> SeedAssetAsync()
    {
        var assetId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        _dbContext.Assets.Add(new AssetRecord
        {
            AssetId = assetId,
            AssetKey = $"ASSET-{assetId.ToString()[..6]}",
            Title = "Hero video",
            SubmittedBy = "coordinator@eventdriven.local",
            LifecycleState = AssetLifecycleState.Processing,
            CreatedAt = now,
            UpdatedAt = now,
        });

        _dbContext.AssetProjections.Add(new AssetProjection
        {
            AssetId = assetId,
            AssetKey = $"ASSET-{assetId.ToString()[..6]}",
            Title = "Hero video",
            SubmittedBy = "coordinator@eventdriven.local",
            LifecycleState = AssetLifecycleState.Processing,
            CreatedAt = now,
            UpdatedAt = now,
        });

        await _dbContext.SaveChangesAsync();
        return assetId;
    }
}
