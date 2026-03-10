using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.ClientServerSpaApi.Domain;
using SystemDesignTutorials.ClientServerSpaApi.Domain.Entities;
using SystemDesignTutorials.ClientServerSpaApi.Domain.Enums;
using SystemDesignTutorials.ClientServerSpaApi.Infrastructure.Persistence;
using SystemDesignTutorials.ClientServerSpaApi.Web.Contracts;

namespace SystemDesignTutorials.ClientServerSpaApi.Web.Endpoints;

internal static class ProjectsEndpoints
{
    public static void MapProjectsEndpoints(this RouteGroupBuilder api)
    {
        var projects = api.MapGroup("/projects");
        projects.MapGet("/", GetProjectsAsync);
        projects.MapPost("/", CreateProjectAsync);
        projects.MapGet("/{id:guid}", GetProjectByIdAsync);
        projects.MapPut("/{id:guid}", UpdateProjectAsync);
        projects.MapPost("/{id:guid}/activate", ActivateProjectAsync);
        projects.MapPost("/{id:guid}/mark-at-risk", MarkAtRiskProjectAsync);
        projects.MapPost("/{id:guid}/complete", CompleteProjectAsync);
        projects.MapPost("/{id:guid}/archive", ArchiveProjectAsync);
        projects.MapGet("/{id:guid}/members", GetProjectMembersAsync);
        projects.MapPost("/{id:guid}/members", AddProjectMemberAsync);
        projects.MapDelete("/{id:guid}/members/{memberId:guid}", RemoveProjectMemberAsync);
    }

    private static async Task<IResult> GetProjectsAsync(
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var projects = await AccessControl.GetAccessibleProjectsQuery(user, dbContext)
            .AsNoTracking()
            .OrderBy(project => project.TargetDate)
            .ThenBy(project => project.Name)
            .ToListAsync(cancellationToken);

        var projectIds = projects.Select(project => project.Id).ToList();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tasks = projectIds.Count == 0
            ? []
            : await dbContext.Tasks.AsNoTracking().Where(task => projectIds.Contains(task.ProjectId)).ToListAsync(cancellationToken);
        var memberCounts = projectIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await dbContext.ProjectMembers.AsNoTracking()
                .Where(member => projectIds.Contains(member.ProjectId))
                .GroupBy(member => member.ProjectId)
                .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);

        var ownerIds = projects.Select(project => project.OwnerUserId).Distinct().ToList();
        var owners = ownerIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await dbContext.Users.AsNoTracking()
                .Where(appUser => ownerIds.Contains(appUser.Id))
                .ToDictionaryAsync(appUser => appUser.Id, appUser => appUser.DisplayName, cancellationToken);

        var response = projects.Select(project =>
        {
            var projectTasks = tasks.Where(task => task.ProjectId == project.Id).ToArray();
            return new ProjectSummaryResponse(
                project.Id,
                project.Name,
                project.Code,
                project.Description,
                project.Status.ToString(),
                project.OwnerUserId,
                owners.GetValueOrDefault(project.OwnerUserId, "Unknown user"),
                project.StartDate,
                project.TargetDate,
                memberCounts.GetValueOrDefault(project.Id),
                projectTasks.Length,
                projectTasks.Count(task => task.Status is not ProjectTaskStatus.Done and not ProjectTaskStatus.Cancelled),
                projectTasks.Count(task => task.IsOverdue(today)));
        }).ToArray();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetProjectByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (project is null)
        {
            return Results.NotFound();
        }

        if (!await AccessControl.HasProjectAccessAsync(user, dbContext, id, cancellationToken))
        {
            return AccessControl.Forbidden("You do not have access to this project.");
        }

        return Results.Ok(await BuildProjectDetailResponseAsync(user, dbContext, project, cancellationToken));
    }

    private static async Task<IResult> CreateProjectAsync(
        CreateProjectRequest request,
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!AccessControl.CanCreateProjects(user))
        {
            return AccessControl.Forbidden("Only workspace admins and project managers can create projects.");
        }

        try
        {
            var currentUserId = AccessControl.GetRequiredUserId(user);
            var project = new Project(request.Name, request.Code, request.Description, currentUserId, request.StartDate, request.TargetDate);

            await dbContext.Projects.AddAsync(project, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            await dbContext.ProjectMembers.AddAsync(new ProjectMember(project.Id, currentUserId, ProjectMemberRole.ProjectManager), cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Created($"/api/projects/{project.Id}", await BuildProjectDetailResponseAsync(user, dbContext, project, cancellationToken));
        }
        catch (BusinessRuleException error)
        {
            return Results.BadRequest(new { message = error.Message });
        }
    }

    private static async Task<IResult> UpdateProjectAsync(
        Guid id,
        UpdateProjectRequest request,
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (project is null)
        {
            return Results.NotFound();
        }

        if (!AccessControl.CanManageProject(user, project))
        {
            return AccessControl.Forbidden("Only the project owner or a workspace admin can update the project.");
        }

        try
        {
            project.UpdateDetails(request.Name, request.Code, request.Description, request.StartDate, request.TargetDate);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(await BuildProjectDetailResponseAsync(user, dbContext, project, cancellationToken));
        }
        catch (BusinessRuleException error)
        {
            return Results.BadRequest(new { message = error.Message });
        }
    }

    private static async Task<IResult> ActivateProjectAsync(Guid id, ClaimsPrincipal user, ClientServerSpaApiDbContext dbContext, CancellationToken cancellationToken)
        => await TransitionProjectAsync(id, user, dbContext, project => project.Activate(), cancellationToken);

    private static async Task<IResult> MarkAtRiskProjectAsync(Guid id, ClaimsPrincipal user, ClientServerSpaApiDbContext dbContext, CancellationToken cancellationToken)
        => await TransitionProjectAsync(id, user, dbContext, project => project.MarkAtRisk(), cancellationToken);

    private static async Task<IResult> CompleteProjectAsync(Guid id, ClaimsPrincipal user, ClientServerSpaApiDbContext dbContext, CancellationToken cancellationToken)
        => await TransitionProjectAsync(id, user, dbContext, project => project.Complete(), cancellationToken);

    private static async Task<IResult> ArchiveProjectAsync(Guid id, ClaimsPrincipal user, ClientServerSpaApiDbContext dbContext, CancellationToken cancellationToken)
        => await TransitionProjectAsync(id, user, dbContext, project => project.Archive(), cancellationToken);

    private static async Task<IResult> GetProjectMembersAsync(
        Guid id,
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (project is null)
        {
            return Results.NotFound();
        }

        if (!await AccessControl.HasProjectAccessAsync(user, dbContext, id, cancellationToken))
        {
            return AccessControl.Forbidden("You do not have access to this project.");
        }

        return Results.Ok(await BuildProjectMembersAsync(dbContext, project.Id, cancellationToken));
    }

    private static async Task<IResult> AddProjectMemberAsync(
        Guid id,
        AddProjectMemberRequest request,
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (project is null)
        {
            return Results.NotFound();
        }

        if (!AccessControl.CanManageMembership(user, project))
        {
            return AccessControl.Forbidden("Only the project owner or a workspace admin can manage membership.");
        }

        if (!Enum.TryParse<ProjectMemberRole>(request.RoleInProject, ignoreCase: true, out var roleInProject))
        {
            return Results.BadRequest(new { message = "RoleInProject is invalid." });
        }

        var appUser = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == request.UserId, cancellationToken);
        if (appUser is null)
        {
            return Results.BadRequest(new { message = "Selected user does not exist." });
        }

        var existingMember = await dbContext.ProjectMembers.SingleOrDefaultAsync(
            member => member.ProjectId == id && member.UserId == request.UserId,
            cancellationToken);

        if (existingMember is not null)
        {
            existingMember.UpdateRole(roleInProject);
        }
        else
        {
            await dbContext.ProjectMembers.AddAsync(new ProjectMember(id, request.UserId, roleInProject), cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(await BuildProjectMembersAsync(dbContext, id, cancellationToken));
    }

    private static async Task<IResult> RemoveProjectMemberAsync(
        Guid id,
        Guid memberId,
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (project is null)
        {
            return Results.NotFound();
        }

        if (!AccessControl.CanManageMembership(user, project))
        {
            return AccessControl.Forbidden("Only the project owner or a workspace admin can manage membership.");
        }

        var member = await dbContext.ProjectMembers.SingleOrDefaultAsync(item => item.Id == memberId && item.ProjectId == id, cancellationToken);
        if (member is null)
        {
            return Results.NotFound();
        }

        if (member.UserId == project.OwnerUserId)
        {
            return Results.BadRequest(new { message = "The project owner cannot be removed from project membership." });
        }

        dbContext.ProjectMembers.Remove(member);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> TransitionProjectAsync(
        Guid id,
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        Action<Project> transition,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (project is null)
        {
            return Results.NotFound();
        }

        if (!AccessControl.CanManageProject(user, project))
        {
            return AccessControl.Forbidden("Only the project owner or a workspace admin can change project status.");
        }

        try
        {
            transition(project);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(await BuildProjectDetailResponseAsync(user, dbContext, project, cancellationToken));
        }
        catch (BusinessRuleException error)
        {
            return Results.BadRequest(new { message = error.Message });
        }
    }

    private static async Task<ProjectDetailResponse> BuildProjectDetailResponseAsync(
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        Project project,
        CancellationToken cancellationToken)
    {
        var members = await dbContext.ProjectMembers
            .AsNoTracking()
            .Where(member => member.ProjectId == project.Id)
            .ToListAsync(cancellationToken);
        members = members
            .OrderBy(member => member.RoleInProject)
            .ThenBy(member => member.JoinedAt)
            .ToList();

        var tasks = await dbContext.Tasks
            .AsNoTracking()
            .Where(task => task.ProjectId == project.Id)
            .OrderBy(task => task.DueDate)
            .ThenBy(task => task.Title)
            .ToListAsync(cancellationToken);

        var userIds = members.Select(member => member.UserId)
            .Concat(tasks.Select(task => task.AssigneeUserId))
            .Concat(tasks.Select(task => task.CreatedByUserId))
            .Append(project.OwnerUserId)
            .Distinct()
            .ToList();

        var users = await dbContext.Users
            .AsNoTracking()
            .Where(appUser => userIds.Contains(appUser.Id))
            .ToDictionaryAsync(appUser => appUser.Id, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var memberResponses = members.Select(member =>
        {
            var appUser = users[member.UserId];
            return new ProjectMemberResponse(member.Id, member.UserId, appUser.DisplayName, appUser.Email!, member.RoleInProject.ToString(), member.JoinedAt);
        }).ToArray();

        var taskResponses = tasks.Select(task => new ProjectTaskSummaryResponse(
            task.Id,
            task.ProjectId,
            project.Name,
            project.Code,
            task.Title,
            task.Status.ToString(),
            task.Priority.ToString(),
            task.AssigneeUserId,
            users.GetValueOrDefault(task.AssigneeUserId)?.DisplayName ?? "Unknown user",
            task.DueDate,
            task.IsOverdue(today),
            AccessControl.GetAvailableTaskActions(user, project, task),
            task.UpdatedAt)).ToArray();

        return new ProjectDetailResponse(
            project.Id,
            project.Name,
            project.Code,
            project.Description,
            project.Status.ToString(),
            project.OwnerUserId,
            users.GetValueOrDefault(project.OwnerUserId)?.DisplayName ?? "Unknown user",
            project.StartDate,
            project.TargetDate,
            project.CompletedAt,
            AccessControl.CanManageProject(user, project),
            AccessControl.CanManageMembership(user, project),
            AccessControl.CanManageTaskStructure(user, project),
            memberResponses,
            taskResponses);
    }

    internal static async Task<ProjectMemberResponse[]> BuildProjectMembersAsync(
        ClientServerSpaApiDbContext dbContext,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var members = await dbContext.ProjectMembers
            .AsNoTracking()
            .Where(member => member.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        var userIds = members.Select(member => member.UserId).Distinct().ToList();
        var users = await dbContext.Users
            .AsNoTracking()
            .Where(appUser => userIds.Contains(appUser.Id))
            .ToDictionaryAsync(appUser => appUser.Id, cancellationToken);

        return members
            .OrderBy(member => member.RoleInProject)
            .ThenBy(member => users.GetValueOrDefault(member.UserId)?.DisplayName)
            .Select(member =>
            {
                var appUser = users[member.UserId];
                return new ProjectMemberResponse(member.Id, member.UserId, appUser.DisplayName, appUser.Email!, member.RoleInProject.ToString(), member.JoinedAt);
            })
            .ToArray();
    }
}
