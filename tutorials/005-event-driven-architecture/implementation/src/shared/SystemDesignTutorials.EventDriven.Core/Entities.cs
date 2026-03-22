using System.Text.Json.Serialization;
using SystemDesignTutorials.EventDriven.Contracts;

namespace SystemDesignTutorials.EventDriven.Core;

public sealed class AssetRecord
{
    public Guid AssetId { get; set; }
    public string AssetKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool SimulateFailure { get; set; }
    public string SubmittedBy { get; set; } = string.Empty;
    public AssetLifecycleState LifecycleState { get; set; } = AssetLifecycleState.UploadPending;
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class AssetProjection
{
    public Guid AssetId { get; set; }
    public string AssetKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public AssetLifecycleState LifecycleState { get; set; } = AssetLifecycleState.UploadPending;
    public ProcessingStepStatus ScanStatus { get; set; } = ProcessingStepStatus.Pending;
    public ProcessingStepStatus MetadataStatus { get; set; } = ProcessingStepStatus.Pending;
    public ProcessingStepStatus ThumbnailStatus { get; set; } = ProcessingStepStatus.Pending;
    public ProcessingStepStatus TranscodeStatus { get; set; } = ProcessingStepStatus.Pending;
    public bool SimulateFailure { get; set; }
    public string SubmittedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ReadyAt { get; set; }
    public string? FailureReason { get; set; }
}

public sealed class NotificationRecord
{
    public Guid NotificationId { get; set; }
    public Guid AssetId { get; set; }
    public string AssetTitle { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; }
}

public sealed class OutboxMessage
{
    public Guid OutboxMessageId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }

    [JsonIgnore]
    public bool IsPublished => PublishedAt.HasValue;
}
