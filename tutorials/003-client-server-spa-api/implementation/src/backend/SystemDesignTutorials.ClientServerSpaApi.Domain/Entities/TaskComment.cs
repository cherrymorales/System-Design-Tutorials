namespace SystemDesignTutorials.ClientServerSpaApi.Domain.Entities;

public sealed class TaskComment
{
    private TaskComment()
    {
    }

    public TaskComment(Guid taskId, Guid authorUserId, string body)
    {
        Id = Guid.NewGuid();
        TaskId = taskId;
        AuthorUserId = authorUserId;
        Body = RequireText(body);
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void UpdateBody(string body, DateTimeOffset now, TimeSpan gracePeriod)
    {
        if (now - CreatedAt > gracePeriod)
        {
            throw new BusinessRuleException("Comment edit window has expired.");
        }

        Body = RequireText(body);
        UpdatedAt = now;
    }

    private static string RequireText(string value)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BusinessRuleException("Comment body is required.");
        }

        if (normalized.Length > 4000)
        {
            throw new BusinessRuleException("Comment body must be 4000 characters or fewer.");
        }

        return normalized;
    }
}
