using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.EventDriven.Contracts;

namespace SystemDesignTutorials.EventDriven.Core;

public sealed class ProjectionUpdateResult
{
    public bool TransitionedToReady { get; init; }
    public bool WasDuplicate { get; init; }
    public string? FailureReason { get; init; }
}

public sealed class ProjectionService(EventDrivenDbContext dbContext)
{
    public async Task MarkUploadCompletedAsync(Guid assetId, CancellationToken cancellationToken)
    {
        var projection = await GetProjectionAsync(assetId, cancellationToken);
        var asset = await GetAssetAsync(assetId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        asset.LifecycleState = AssetLifecycleState.Processing;
        asset.UpdatedAt = now;

        projection.LifecycleState = AssetLifecycleState.Processing;
        projection.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProjectionUpdateResult> MarkStepCompletedAsync(Guid assetId, string step, CancellationToken cancellationToken)
    {
        var projection = await GetProjectionAsync(assetId, cancellationToken);
        var asset = await GetAssetAsync(assetId, cancellationToken);

        if (projection.LifecycleState == AssetLifecycleState.Failed)
        {
            return new ProjectionUpdateResult { WasDuplicate = true, FailureReason = projection.FailureReason };
        }

        var wasDuplicate = step switch
        {
            "scan" => projection.ScanStatus == ProcessingStepStatus.Completed,
            "metadata" => projection.MetadataStatus == ProcessingStepStatus.Completed,
            "thumbnail" => projection.ThumbnailStatus == ProcessingStepStatus.Completed,
            "transcode" => projection.TranscodeStatus == ProcessingStepStatus.Completed,
            _ => throw new InvalidOperationException($"Unknown processing step '{step}'."),
        };

        var now = DateTimeOffset.UtcNow;

        switch (step)
        {
            case "scan":
                projection.ScanStatus = ProcessingStepStatus.Completed;
                break;
            case "metadata":
                projection.MetadataStatus = ProcessingStepStatus.Completed;
                break;
            case "thumbnail":
                projection.ThumbnailStatus = ProcessingStepStatus.Completed;
                break;
            case "transcode":
                projection.TranscodeStatus = ProcessingStepStatus.Completed;
                break;
        }

        projection.LifecycleState = AssetLifecycleState.Processing;
        projection.UpdatedAt = now;
        asset.LifecycleState = AssetLifecycleState.Processing;
        asset.UpdatedAt = now;

        var transitionedToReady =
            projection.ScanStatus == ProcessingStepStatus.Completed
            && projection.MetadataStatus == ProcessingStepStatus.Completed
            && projection.ThumbnailStatus == ProcessingStepStatus.Completed
            && projection.TranscodeStatus == ProcessingStepStatus.Completed
            && projection.ReadyAt is null;

        if (transitionedToReady)
        {
            projection.LifecycleState = AssetLifecycleState.Ready;
            projection.ReadyAt = now;
            asset.LifecycleState = AssetLifecycleState.Ready;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new ProjectionUpdateResult { TransitionedToReady = transitionedToReady, WasDuplicate = wasDuplicate };
    }

    public async Task<ProjectionUpdateResult> MarkFailedAsync(Guid assetId, string reason, CancellationToken cancellationToken)
    {
        var projection = await GetProjectionAsync(assetId, cancellationToken);
        var asset = await GetAssetAsync(assetId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (projection.LifecycleState == AssetLifecycleState.Failed && string.Equals(projection.FailureReason, reason, StringComparison.Ordinal))
        {
            return new ProjectionUpdateResult { WasDuplicate = true, FailureReason = reason };
        }

        projection.LifecycleState = AssetLifecycleState.Failed;
        projection.TranscodeStatus = ProcessingStepStatus.Failed;
        projection.FailureReason = reason;
        projection.UpdatedAt = now;

        asset.LifecycleState = AssetLifecycleState.Failed;
        asset.FailureReason = reason;
        asset.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new ProjectionUpdateResult { FailureReason = reason };
    }

    private async Task<AssetProjection> GetProjectionAsync(Guid assetId, CancellationToken cancellationToken)
        => await dbContext.AssetProjections.FirstAsync(asset => asset.AssetId == assetId, cancellationToken);

    private async Task<AssetRecord> GetAssetAsync(Guid assetId, CancellationToken cancellationToken)
        => await dbContext.Assets.FirstAsync(asset => asset.AssetId == assetId, cancellationToken);
}
