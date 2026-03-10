namespace SystemDesignTutorials.ClientServerSpaApi.Web.Contracts;

public sealed record UpdateTaskRequest(
    string Title,
    string Description,
    Guid AssigneeUserId,
    string Priority,
    DateOnly? DueDate);
