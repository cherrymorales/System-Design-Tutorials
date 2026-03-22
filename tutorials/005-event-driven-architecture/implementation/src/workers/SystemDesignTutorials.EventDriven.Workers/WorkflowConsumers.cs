using MassTransit;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.EventDriven.Contracts;
using SystemDesignTutorials.EventDriven.Core;

namespace SystemDesignTutorials.EventDriven.Workers;

public sealed class UploadProjectionConsumer(ProjectionService projectionService) : IConsumer<AssetUploadCompletedEvent>
{
    public async Task Consume(ConsumeContext<AssetUploadCompletedEvent> context)
        => await projectionService.MarkUploadCompletedAsync(context.Message.AssetId, context.CancellationToken);
}

public sealed class ScanFanOutConsumer : IConsumer<AssetUploadCompletedEvent>
{
    public async Task Consume(ConsumeContext<AssetUploadCompletedEvent> context)
    {
        await Task.Delay(250, context.CancellationToken);
        await context.Publish(new AssetScanCompletedEvent(Guid.NewGuid(), context.Message.AssetId, context.Message.CorrelationId, DateTimeOffset.UtcNow));
    }
}

public sealed class MetadataFanOutConsumer : IConsumer<AssetUploadCompletedEvent>
{
    public async Task Consume(ConsumeContext<AssetUploadCompletedEvent> context)
    {
        await Task.Delay(400, context.CancellationToken);
        await context.Publish(new MetadataExtractedEvent(Guid.NewGuid(), context.Message.AssetId, context.Message.CorrelationId, DateTimeOffset.UtcNow));
    }
}

public sealed class ThumbnailFanOutConsumer : IConsumer<AssetUploadCompletedEvent>
{
    public async Task Consume(ConsumeContext<AssetUploadCompletedEvent> context)
    {
        await Task.Delay(600, context.CancellationToken);
        await context.Publish(new ThumbnailGenerationCompletedEvent(Guid.NewGuid(), context.Message.AssetId, context.Message.CorrelationId, DateTimeOffset.UtcNow));
    }
}

public sealed class TranscodeFanOutConsumer : IConsumer<AssetUploadCompletedEvent>
{
    public async Task Consume(ConsumeContext<AssetUploadCompletedEvent> context)
    {
        await Task.Delay(900, context.CancellationToken);

        if (context.Message.SimulateFailure)
        {
            await context.Publish(
                new AssetProcessingFailedEvent(
                    Guid.NewGuid(),
                    context.Message.AssetId,
                    context.Message.CorrelationId,
                    "Transcode pipeline rejected the asset because the media payload is marked as a simulated failure case.",
                    DateTimeOffset.UtcNow));
            return;
        }

        await context.Publish(new TranscodeCompletedEvent(Guid.NewGuid(), context.Message.AssetId, context.Message.CorrelationId, DateTimeOffset.UtcNow));
    }
}

public sealed class ScanProjectionConsumer(ProjectionService projectionService) : IConsumer<AssetScanCompletedEvent>
{
    public async Task Consume(ConsumeContext<AssetScanCompletedEvent> context)
        => await projectionService.MarkStepCompletedAsync(context.Message.AssetId, "scan", context.CancellationToken);
}

public sealed class MetadataProjectionConsumer(ProjectionService projectionService) : IConsumer<MetadataExtractedEvent>
{
    public async Task Consume(ConsumeContext<MetadataExtractedEvent> context)
        => await projectionService.MarkStepCompletedAsync(context.Message.AssetId, "metadata", context.CancellationToken);
}

public sealed class ThumbnailProjectionConsumer(ProjectionService projectionService) : IConsumer<ThumbnailGenerationCompletedEvent>
{
    public async Task Consume(ConsumeContext<ThumbnailGenerationCompletedEvent> context)
        => await projectionService.MarkStepCompletedAsync(context.Message.AssetId, "thumbnail", context.CancellationToken);
}

public sealed class TranscodeProjectionConsumer(ProjectionService projectionService) : IConsumer<TranscodeCompletedEvent>
{
    public async Task Consume(ConsumeContext<TranscodeCompletedEvent> context)
    {
        var result = await projectionService.MarkStepCompletedAsync(context.Message.AssetId, "transcode", context.CancellationToken);
        if (result.TransitionedToReady)
        {
            await context.Publish(new AssetReadyEvent(Guid.NewGuid(), context.Message.AssetId, context.Message.CorrelationId, DateTimeOffset.UtcNow));
        }
    }
}

public sealed class AssetFailedProjectionConsumer(ProjectionService projectionService) : IConsumer<AssetProcessingFailedEvent>
{
    public async Task Consume(ConsumeContext<AssetProcessingFailedEvent> context)
        => await projectionService.MarkFailedAsync(context.Message.AssetId, context.Message.FailureReason, context.CancellationToken);
}

public sealed class NotificationRequestConsumer : IConsumer<AssetReadyEvent>
{
    public async Task Consume(ConsumeContext<AssetReadyEvent> context)
    {
        await context.Publish(
            new NotificationRequestedEvent(
                Guid.NewGuid(),
                context.Message.AssetId,
                context.Message.CorrelationId,
                "Asset processing completed successfully. The asset is ready for downstream publishing.",
                DateTimeOffset.UtcNow));
    }
}

public sealed class NotificationProjectionConsumer(EventDrivenDbContext dbContext) : IConsumer<NotificationRequestedEvent>
{
    public async Task Consume(ConsumeContext<NotificationRequestedEvent> context)
    {
        var asset = await dbContext.AssetProjections.AsNoTracking()
            .FirstAsync(x => x.AssetId == context.Message.AssetId, context.CancellationToken);

        var exists = await dbContext.Notifications.AnyAsync(
            notification => notification.AssetId == context.Message.AssetId && notification.Message == context.Message.Message,
            context.CancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.Notifications.Add(new NotificationRecord
        {
            NotificationId = Guid.NewGuid(),
            AssetId = context.Message.AssetId,
            AssetTitle = asset.Title,
            Message = context.Message.Message,
            SentAt = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
