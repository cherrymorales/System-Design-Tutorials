namespace SystemDesignTutorials.ClientServerSpaApi.Web.Contracts;

public sealed record CreateProjectRequest(
    string Name,
    string Code,
    string Description,
    DateOnly StartDate,
    DateOnly? TargetDate);
