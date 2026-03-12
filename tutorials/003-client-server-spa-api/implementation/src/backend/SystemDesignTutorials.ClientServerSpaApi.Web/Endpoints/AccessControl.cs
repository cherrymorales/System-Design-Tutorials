using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.ClientServerSpaApi.Domain.Entities;
using SystemDesignTutorials.ClientServerSpaApi.Domain.Enums;
using SystemDesignTutorials.ClientServerSpaApi.Infrastructure.Persistence;
using SystemDesignTutorials.ClientServerSpaApi.Infrastructure.Seeding;

namespace SystemDesignTutorials.ClientServerSpaApi.Web.Endpoints;

internal static class AccessControl
{
    public static string GetRequiredEmail(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Email)
            ?? user.Identity?.Name
            ?? throw new InvalidOperationException("Authenticated email claim is missing.");

    public static Guid GetRequiredUserId(ClaimsPrincipal user)
        => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new InvalidOperationException("Authenticated user id claim is missing.");

    public static bool IsWorkspaceAdmin(ClaimsPrincipal user) => user.IsInRole(ApplicationRoles.WorkspaceAdmin);
    public static bool IsProjectManager(ClaimsPrincipal user) => user.IsInRole(ApplicationRoles.ProjectManager);
    public static bool CanCreateProjects(ClaimsPrincipal user) => IsWorkspaceAdmin(user) || IsProjectManager(user);
    public static bool CanManageProject(ClaimsPrincipal user, Project project)
        => IsWorkspaceAdmin(user) || project.OwnerUserId == GetRequiredUserId(user);
    public static bool CanManageMembership(ClaimsPrincipal user, Project project) => CanManageProject(user, project);
    public static bool CanManageTaskStructure(ClaimsPrincipal user, Project project) => CanManageProject(user, project);
    public static bool CanUpdateTaskWorkflow(ClaimsPrincipal user, Project project, ProjectTask task)
        => IsWorkspaceAdmin(user)
            || project.OwnerUserId == GetRequiredUserId(user)
            || task.AssigneeUserId == GetRequiredUserId(user);

    public static bool IsAssignableRole(ProjectMemberRole role)
        => role is ProjectMemberRole.ProjectManager or ProjectMemberRole.Contributor;

    public static IResult Forbidden(string message)
        => Results.Json(new { message }, statusCode: StatusCodes.Status403Forbidden);

    public static async Task<ProjectMemberRole?> GetProjectMembershipRoleAsync(
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        if (IsWorkspaceAdmin(user))
        {
            return ProjectMemberRole.ProjectManager;
        }

        var userId = GetRequiredUserId(user);
        var project = await dbContext.Projects.AsNoTracking().SingleOrDefaultAsync(item => item.Id == projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        if (project.OwnerUserId == userId)
        {
            return ProjectMemberRole.ProjectManager;
        }

        var membership = await dbContext.ProjectMembers.AsNoTracking()
            .SingleOrDefaultAsync(member => member.ProjectId == projectId && member.UserId == userId, cancellationToken);

        return membership?.RoleInProject;
    }

    public static async Task<bool> CanCommentAsync(
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var membershipRole = await GetProjectMembershipRoleAsync(user, dbContext, projectId, cancellationToken);
        return membershipRole.HasValue && membershipRole.Value != ProjectMemberRole.Viewer;
    }

    public static IQueryable<Project> GetAccessibleProjectsQuery(ClaimsPrincipal user, ClientServerSpaApiDbContext dbContext)
    {
        if (IsWorkspaceAdmin(user))
        {
            return dbContext.Projects;
        }

        var userId = GetRequiredUserId(user);
        return dbContext.Projects.Where(project =>
            project.OwnerUserId == userId
            || dbContext.ProjectMembers.Any(member => member.ProjectId == project.Id && member.UserId == userId));
    }

    public static async Task<bool> HasProjectAccessAsync(
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        if (IsWorkspaceAdmin(user))
        {
            return true;
        }

        var userId = GetRequiredUserId(user);
        return await dbContext.Projects.AnyAsync(project =>
                project.Id == projectId
                && (project.OwnerUserId == userId
                    || dbContext.ProjectMembers.Any(member => member.ProjectId == projectId && member.UserId == userId)),
            cancellationToken);
    }

    public static string[] GetAvailableTaskActions(ClaimsPrincipal user, Project project, ProjectTask task)
    {
        if (!CanUpdateTaskWorkflow(user, project, task))
        {
            return [];
        }

        return task.Status switch
        {
            ProjectTaskStatus.Backlog => ["start", "cancel"],
            ProjectTaskStatus.InProgress => ["block", "submit-review", "cancel"],
            ProjectTaskStatus.Blocked => ["start"],
            ProjectTaskStatus.InReview => ["complete", "start", "block"],
            _ => [],
        };
    }
}
