using SystemDesignTutorials.ClientServerSpaApi.Domain.Enums;

namespace SystemDesignTutorials.ClientServerSpaApi.Domain.Entities;

public sealed class TaskActivity
{
    private TaskActivity()
    {
    }

    public TaskActivity(Guid taskId, TaskActivityType type, Guid actorUserId, string summary)
    {
        Id = Guid.NewGuid();
        TaskId = taskId;
        Type = type;
        ActorUserId = actorUserId;
        Summary = summary.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TaskId { get; private set; }
    public TaskActivityType Type { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
}
