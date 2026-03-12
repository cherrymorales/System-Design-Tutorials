namespace SystemDesignTutorials.ClientServerSpaApi.Web.Contracts;

public sealed record UpdateProjectRequest(
    string Name,
    string Code,
    string Description,
    DateOnly StartDate,
    DateOnly? TargetDate);
