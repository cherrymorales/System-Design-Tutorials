namespace SystemDesignTutorials.ClientServerSpaApi.Web.Contracts;

public sealed record CreateTaskRequest(
    Guid ProjectId,
    string Title,
    string Description,
    Guid AssigneeUserId,
    string Priority,
    DateOnly? DueDate);
