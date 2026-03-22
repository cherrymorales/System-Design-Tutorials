namespace SystemDesignTutorials.EventDriven.Contracts;

public enum AssetLifecycleState
{
    Registered = 0,
    UploadPending = 1,
    Uploaded = 2,
    Processing = 3,
    Ready = 4,
    Failed = 5,
}

public enum ProcessingStepStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
}

public static class EventDrivenRoles
{
    public const string ContentOperationsCoordinator = "ContentOperationsCoordinator";
    public const string OperationsManager = "OperationsManager";
}

public sealed record SeedUserDto(Guid UserId, string Email, string DisplayName, string Role);

public sealed record CurrentUserDto(Guid UserId, string Email, string DisplayName, string Role);

public sealed record LoginRequest(string Email, string Password);

public sealed record RegisterAssetRequest(string AssetKey, string Title, bool SimulateFailure);

public sealed record AssetSummaryDto(
    Guid AssetId,
    string AssetKey,
    string Title,
    AssetLifecycleState LifecycleState,
    ProcessingStepStatus ScanStatus,
    ProcessingStepStatus MetadataStatus,
    ProcessingStepStatus ThumbnailStatus,
    ProcessingStepStatus TranscodeStatus,
    string? FailureReason,
    DateTimeOffset UpdatedAt);

public sealed record AssetDetailDto(
    Guid AssetId,
    string AssetKey,
    string Title,
    AssetLifecycleState LifecycleState,
    ProcessingStepStatus ScanStatus,
    ProcessingStepStatus MetadataStatus,
    ProcessingStepStatus ThumbnailStatus,
    ProcessingStepStatus TranscodeStatus,
    bool SimulateFailure,
    string SubmittedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ReadyAt,
    string? FailureReason);

public sealed record DashboardSummaryDto(
    int TotalAssets,
    int ProcessingAssets,
    int ReadyAssets,
    int FailedAssets,
    int PendingUploads,
    int NotificationsSent);

public sealed record NotificationDto(
    Guid NotificationId,
    Guid AssetId,
    string AssetTitle,
    string Message,
    DateTimeOffset SentAt);

public interface IEventMessage
{
    Guid EventId { get; }
    Guid AssetId { get; }
    Guid CorrelationId { get; }
    DateTimeOffset OccurredAt { get; }
}

public sealed record AssetRegisteredEvent(
    Guid EventId,
    Guid AssetId,
    Guid CorrelationId,
    string AssetKey,
    string Title,
    bool SimulateFailure,
    string SubmittedBy,
    DateTimeOffset OccurredAt) : IEventMessage;

public sealed record AssetUploadCompletedEvent(
    Guid EventId,
    Guid AssetId,
    Guid CorrelationId,
    string AssetKey,
    string Title,
    bool SimulateFailure,
    string SubmittedBy,
    DateTimeOffset OccurredAt) : IEventMessage;

public sealed record AssetScanCompletedEvent(
    Guid EventId,
    Guid AssetId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt) : IEventMessage;

public sealed record MetadataExtractedEvent(
    Guid EventId,
    Guid AssetId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt) : IEventMessage;

public sealed record ThumbnailGenerationCompletedEvent(
    Guid EventId,
    Guid AssetId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt) : IEventMessage;

public sealed record TranscodeCompletedEvent(
    Guid EventId,
    Guid AssetId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt) : IEventMessage;

public sealed record AssetReadyEvent(
    Guid EventId,
    Guid AssetId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt) : IEventMessage;

public sealed record AssetProcessingFailedEvent(
    Guid EventId,
    Guid AssetId,
    Guid CorrelationId,
    string FailureReason,
    DateTimeOffset OccurredAt) : IEventMessage;

public sealed record NotificationRequestedEvent(
    Guid EventId,
    Guid AssetId,
    Guid CorrelationId,
    string Message,
    DateTimeOffset OccurredAt) : IEventMessage;
