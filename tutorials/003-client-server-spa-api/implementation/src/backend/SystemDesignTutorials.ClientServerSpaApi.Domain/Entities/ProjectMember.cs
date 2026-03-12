using SystemDesignTutorials.ClientServerSpaApi.Domain.Enums;

namespace SystemDesignTutorials.ClientServerSpaApi.Domain.Entities;

public sealed class ProjectMember
{
    private ProjectMember()
    {
    }

    public ProjectMember(Guid projectId, Guid userId, ProjectMemberRole roleInProject)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        UserId = userId;
        RoleInProject = roleInProject;
        JoinedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public ProjectMemberRole RoleInProject { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    public void UpdateRole(ProjectMemberRole roleInProject)
    {
        RoleInProject = roleInProject;
    }
}
