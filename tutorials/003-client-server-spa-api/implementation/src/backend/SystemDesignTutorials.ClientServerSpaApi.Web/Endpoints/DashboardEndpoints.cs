using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.ClientServerSpaApi.Domain.Enums;
using SystemDesignTutorials.ClientServerSpaApi.Infrastructure.Persistence;

namespace SystemDesignTutorials.ClientServerSpaApi.Web.Endpoints;

internal static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this RouteGroupBuilder api)
    {
        var dashboard = api.MapGroup("/dashboard");
        dashboard.MapGet("/summary", GetSummaryAsync);
        dashboard.MapGet("/my-work", GetMyWorkAsync);
    }

    private static async Task<IResult> GetSummaryAsync(
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var accessibleProjectIds = await AccessControl.GetAccessibleProjectsQuery(user, dbContext)
            .Select(project => project.Id)
            .ToListAsync(cancellationToken);

        if (accessibleProjectIds.Count == 0)
        {
            return Results.Ok(new DashboardSummaryResponse(0, 0, 0, 0, 0));
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var projects = await dbContext.Projects
            .AsNoTracking()
            .Where(project => accessibleProjectIds.Contains(project.Id))
            .ToListAsync(cancellationToken);
        var tasks = await dbContext.Tasks
            .AsNoTracking()
            .Where(task => accessibleProjectIds.Contains(task.ProjectId))
            .ToListAsync(cancellationToken);

        var currentUserId = AccessControl.GetRequiredUserId(user);
        var response = new DashboardSummaryResponse(
            projects.Count(project => project.Status == ProjectStatus.Active),
            projects.Count(project => project.Status == ProjectStatus.AtRisk),
            tasks.Count(task => task.IsOverdue(today)),
            tasks.Count(task => task.Status == ProjectTaskStatus.InReview),
            tasks.Count(task => task.AssigneeUserId == currentUserId && task.Status is not ProjectTaskStatus.Done and not ProjectTaskStatus.Cancelled));

        return Results.Ok(response);
    }

    private static async Task<IResult> GetMyWorkAsync(
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var accessibleProjects = await AccessControl.GetAccessibleProjectsQuery(user, dbContext)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var projectLookup = accessibleProjects.ToDictionary(project => project.Id);
        var accessibleProjectIds = projectLookup.Keys.ToList();
        var currentUserId = AccessControl.GetRequiredUserId(user);
        var tasks = await dbContext.Tasks
            .AsNoTracking()
            .Where(task => accessibleProjectIds.Contains(task.ProjectId) && task.AssigneeUserId == currentUserId)
            .OrderBy(task => task.DueDate)
            .ThenBy(task => task.Title)
            .ToListAsync(cancellationToken);

        var assigneeIds = tasks.Select(task => task.AssigneeUserId).Distinct().ToList();
        var users = await dbContext.Users
            .AsNoTracking()
            .Where(appUser => assigneeIds.Contains(appUser.Id))
            .ToDictionaryAsync(appUser => appUser.Id, appUser => appUser.DisplayName, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var response = tasks.Select(task =>
        {
            var project = projectLookup[task.ProjectId];
            var actions = AccessControl.GetAvailableTaskActions(user, project, task);
            return new ProjectTaskSummaryResponse(
                task.Id,
                project.Id,
                project.Name,
                project.Code,
                task.Title,
                task.Status.ToString(),
                task.Priority.ToString(),
                task.AssigneeUserId,
                users.GetValueOrDefault(task.AssigneeUserId, "Unknown user"),
                task.DueDate,
                task.IsOverdue(today),
                actions,
                task.UpdatedAt);
        }).ToArray();

        return Results.Ok(response);
    }
}
