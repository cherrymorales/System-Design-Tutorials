using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.EventDriven.Contracts;
using SystemDesignTutorials.EventDriven.Core;

namespace SystemDesignTutorials.EventDriven.Tests;

public sealed class WorkflowServiceTests : IAsyncLifetime
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
    public async Task RegisterAssetCreatesProjectionAndOutboxRecord()
    {
        var service = new AssetWorkflowService(_dbContext);
        var currentUser = new CurrentUserDto(
            Guid.NewGuid(),
            "coordinator@eventdriven.local",
            "Coordinator",
            EventDrivenRoles.ContentOperationsCoordinator);

        var asset = await service.RegisterAssetAsync(
            new RegisterAssetRequest("asset-reg-001", "Launch trailer", false),
            currentUser,
            CancellationToken.None);

        var storedAsset = await _dbContext.Assets.SingleAsync(x => x.AssetId == asset.AssetId);
        var projection = await _dbContext.AssetProjections.SingleAsync(x => x.AssetId == asset.AssetId);
        var outbox = await _dbContext.OutboxMessages.SingleAsync();

        Assert.Equal("ASSET-REG-001", storedAsset.AssetKey);
        Assert.Equal(AssetLifecycleState.Registered, projection.LifecycleState);
        Assert.Contains("AssetRegisteredEvent", outbox.Type);
    }

    [Fact]
    public async Task UploadCompleteRecordsUploadCompletedOutboxMessage()
    {
        var service = new AssetWorkflowService(_dbContext);
        var currentUser = new CurrentUserDto(
            Guid.NewGuid(),
            "coordinator@eventdriven.local",
            "Coordinator",
            EventDrivenRoles.ContentOperationsCoordinator);

        var created = await service.RegisterAssetAsync(
            new RegisterAssetRequest("asset-upload-001", "Asset ready for upload", false),
            currentUser,
            CancellationToken.None);

        var uploaded = await service.MarkUploadCompleteAsync(created.AssetId, CancellationToken.None);
        var outboxMessages = (await _dbContext.OutboxMessages.ToListAsync()).OrderBy(x => x.CreatedAt).ToList();

        Assert.Equal(AssetLifecycleState.Uploaded, uploaded.LifecycleState);
        Assert.Equal(2, outboxMessages.Count);
        Assert.Contains("AssetUploadCompletedEvent", outboxMessages.Last().Type);
    }

    private EventDrivenDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EventDrivenDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new EventDrivenDbContext(options);
    }
}
