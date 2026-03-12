namespace SystemDesignTutorials.ClientServerSpaApi.Web.Contracts;

public sealed record AddProjectMemberRequest(Guid UserId, string RoleInProject);
