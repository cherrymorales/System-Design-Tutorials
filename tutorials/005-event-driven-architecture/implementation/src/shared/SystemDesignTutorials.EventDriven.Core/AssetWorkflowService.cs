using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.EventDriven.Contracts;

namespace SystemDesignTutorials.EventDriven.Core;

public sealed class AssetWorkflowService(EventDrivenDbContext dbContext)
{
    public async Task<AssetDetailDto> RegisterAssetAsync(RegisterAssetRequest request, CurrentUserDto user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AssetKey))
        {
            throw new InvalidOperationException("Asset key is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new InvalidOperationException("Title is required.");
        }

        var normalizedKey = request.AssetKey.Trim().ToUpperInvariant();
        var exists = await dbContext.Assets.AnyAsync(asset => asset.AssetKey == normalizedKey, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Asset key '{normalizedKey}' is already registered.");
        }

        var now = DateTimeOffset.UtcNow;
        var assetId = Guid.NewGuid();
        var asset = new AssetRecord
        {
            AssetId = assetId,
            AssetKey = normalizedKey,
            Title = request.Title.Trim(),
            SimulateFailure = request.SimulateFailure,
            SubmittedBy = user.Email,
            LifecycleState = AssetLifecycleState.Registered,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var projection = new AssetProjection
        {
            AssetId = assetId,
            AssetKey = normalizedKey,
            Title = request.Title.Trim(),
            SimulateFailure = request.SimulateFailure,
            SubmittedBy = user.Email,
            LifecycleState = AssetLifecycleState.Registered,
            CreatedAt = now,
            UpdatedAt = now,
        };

        dbContext.Assets.Add(asset);
        dbContext.AssetProjections.Add(projection);
        dbContext.OutboxMessages.Add(
            OutboxSerialization.CreateMessage(
                new AssetRegisteredEvent(Guid.NewGuid(), assetId, assetId, asset.AssetKey, asset.Title, asset.SimulateFailure, user.Email, now),
                now));

        await dbContext.SaveChangesAsync(cancellationToken);
        return projection.ToDetailDto();
    }

    public async Task<AssetDetailDto> MarkUploadCompleteAsync(Guid assetId, CancellationToken cancellationToken)
    {
        var asset = await dbContext.Assets.FirstOrDefaultAsync(x => x.AssetId == assetId, cancellationToken)
            ?? throw new KeyNotFoundException($"Asset '{assetId}' was not found.");

        var projection = await dbContext.AssetProjections.FirstAsync(x => x.AssetId == assetId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (asset.LifecycleState is AssetLifecycleState.Ready or AssetLifecycleState.Failed or AssetLifecycleState.Processing)
        {
            return projection.ToDetailDto();
        }

        asset.LifecycleState = AssetLifecycleState.Uploaded;
        asset.UpdatedAt = now;
        projection.LifecycleState = AssetLifecycleState.Uploaded;
        projection.UpdatedAt = now;

        dbContext.OutboxMessages.Add(
            OutboxSerialization.CreateMessage(
                new AssetUploadCompletedEvent(Guid.NewGuid(), asset.AssetId, asset.AssetId, asset.AssetKey, asset.Title, asset.SimulateFailure, asset.SubmittedBy, now),
                now));

        await dbContext.SaveChangesAsync(cancellationToken);
        return projection.ToDetailDto();
    }

    public async Task<IReadOnlyList<AssetSummaryDto>> ListAssetsAsync(CancellationToken cancellationToken)
        => (await dbContext.AssetProjections
                .AsNoTracking()
                .ToListAsync(cancellationToken))
            .OrderByDescending(asset => asset.UpdatedAt)
            .Select(asset => asset.ToSummaryDto())
            .ToList();

    public async Task<AssetDetailDto?> GetAssetAsync(Guid assetId, CancellationToken cancellationToken)
    {
        var asset = await dbContext.AssetProjections
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.AssetId == assetId, cancellationToken);

        return asset?.ToDetailDto();
    }

    public async Task<DashboardSummaryDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var projections = await dbContext.AssetProjections.AsNoTracking().ToListAsync(cancellationToken);
        var notificationCount = await dbContext.Notifications.CountAsync(cancellationToken);

        return new DashboardSummaryDto(
            projections.Count,
            projections.Count(asset => asset.LifecycleState == AssetLifecycleState.Processing),
            projections.Count(asset => asset.LifecycleState == AssetLifecycleState.Ready),
            projections.Count(asset => asset.LifecycleState == AssetLifecycleState.Failed),
            projections.Count(asset => asset.LifecycleState is AssetLifecycleState.Registered or AssetLifecycleState.UploadPending or AssetLifecycleState.Uploaded),
            notificationCount);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(CancellationToken cancellationToken)
        => (await dbContext.Notifications
                .AsNoTracking()
                .ToListAsync(cancellationToken))
            .OrderByDescending(notification => notification.SentAt)
            .Select(notification => new NotificationDto(
                notification.NotificationId,
                notification.AssetId,
                notification.AssetTitle,
                notification.Message,
                notification.SentAt))
            .ToList();
}
