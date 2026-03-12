using SystemDesignTutorials.ClientServerSpaApi.Domain.Enums;

namespace SystemDesignTutorials.ClientServerSpaApi.Domain.Entities;

public sealed class ProjectTask
{
    private ProjectTask()
    {
    }

    public ProjectTask(
        Guid projectId,
        string title,
        string description,
        Guid assigneeUserId,
        Guid createdByUserId,
        ProjectTaskPriority priority,
        DateOnly? dueDate)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        Title = RequireText(title, nameof(title), 200);
        Description = RequireText(description, nameof(description), 4000);
        AssigneeUserId = assigneeUserId;
        CreatedByUserId = createdByUserId;
        Priority = priority;
        DueDate = dueDate;
        Status = ProjectTaskStatus.Backlog;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ProjectTaskStatus Status { get; private set; }
    public ProjectTaskPriority Priority { get; private set; }
    public Guid AssigneeUserId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string? BlockerNote { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void UpdateDetails(string title, string description, Guid assigneeUserId, ProjectTaskPriority priority, DateOnly? dueDate)
    {
        EnsureMutable();
        Title = RequireText(title, nameof(title), 200);
        Description = RequireText(description, nameof(description), 4000);
        AssigneeUserId = assigneeUserId;
        Priority = priority;
        DueDate = dueDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Start(Guid actorUserId)
    {
        if (Status is not ProjectTaskStatus.Backlog and not ProjectTaskStatus.Blocked)
        {
            throw new BusinessRuleException("Only backlog or blocked tasks can move to in progress.");
        }

        Status = ProjectTaskStatus.InProgress;
        BlockerNote = null;
        CompletedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Block(Guid actorUserId, string blockerNote)
    {
        if (Status is not ProjectTaskStatus.InProgress and not ProjectTaskStatus.InReview)
        {
            throw new BusinessRuleException("Only active or review tasks can be blocked.");
        }

        BlockerNote = RequireText(blockerNote, nameof(blockerNote), 1000);
        Status = ProjectTaskStatus.Blocked;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SubmitReview(Guid actorUserId)
    {
        if (Status != ProjectTaskStatus.InProgress)
        {
            throw new BusinessRuleException("Only in-progress tasks can move to review.");
        }

        Status = ProjectTaskStatus.InReview;
        BlockerNote = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Complete(Guid actorUserId)
    {
        if (Status != ProjectTaskStatus.InReview)
        {
            throw new BusinessRuleException("Only review tasks can be completed.");
        }

        Status = ProjectTaskStatus.Done;
        BlockerNote = null;
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CompletedAt.Value;
    }

    public void Cancel(Guid actorUserId)
    {
        if (Status == ProjectTaskStatus.Done)
        {
            throw new BusinessRuleException("Completed tasks cannot be cancelled.");
        }

        if (Status == ProjectTaskStatus.Cancelled)
        {
            throw new BusinessRuleException("Task is already cancelled.");
        }

        Status = ProjectTaskStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsOverdue(DateOnly today)
    {
        return DueDate.HasValue
            && DueDate.Value < today
            && Status is not ProjectTaskStatus.Done
            && Status is not ProjectTaskStatus.Cancelled;
    }

    private void EnsureMutable()
    {
        if (Status is ProjectTaskStatus.Done or ProjectTaskStatus.Cancelled)
        {
            throw new BusinessRuleException("Completed or cancelled tasks are not editable.");
        }
    }

    private static string RequireText(string value, string parameterName, int maxLength)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BusinessRuleException($"{parameterName} is required.");
        }

        if (normalized.Length > maxLength)
        {
            throw new BusinessRuleException($"{parameterName} must be {maxLength} characters or fewer.");
        }

        return normalized;
    }
}
