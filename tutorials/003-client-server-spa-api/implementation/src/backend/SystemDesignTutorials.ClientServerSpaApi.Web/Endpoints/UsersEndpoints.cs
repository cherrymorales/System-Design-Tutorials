using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.ClientServerSpaApi.Domain.Enums;
using SystemDesignTutorials.ClientServerSpaApi.Infrastructure.Persistence;

namespace SystemDesignTutorials.ClientServerSpaApi.Web.Endpoints;

internal static class UsersEndpoints
{
    public static void MapUsersEndpoints(this RouteGroupBuilder api)
    {
        var users = api.MapGroup("/users");
        users.MapGet("/workspace", GetWorkspaceUsersAsync);
        users.MapGet("/assignable", GetAssignableUsersAsync);
    }

    private static async Task<IResult> GetWorkspaceUsersAsync(
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!AccessControl.CanCreateProjects(user))
        {
            return AccessControl.Forbidden("Only workspace admins and project managers can browse workspace users.");
        }

        var users = await dbContext.Users
            .AsNoTracking()
            .OrderBy(appUser => appUser.DisplayName)
            .ToListAsync(cancellationToken);

        var response = new List<WorkspaceUserResponse>(users.Count);
        foreach (var appUser in users)
        {
            var roles = await dbContext.UserRoles
                .Where(userRole => userRole.UserId == appUser.Id)
                .Join(
                    dbContext.Roles,
                    userRole => userRole.RoleId,
                    role => role.Id,
                    (_, role) => role.Name!)
                .ToArrayAsync(cancellationToken);

            response.Add(new WorkspaceUserResponse(appUser.Id, appUser.DisplayName, appUser.Email!, roles));
        }

        return Results.Ok(response.ToArray());
    }

    private static async Task<IResult> GetAssignableUsersAsync(
        ClaimsPrincipal user,
        Guid projectId,
        ClientServerSpaApiDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!await AccessControl.HasProjectAccessAsync(user, dbContext, projectId, cancellationToken))
        {
            return AccessControl.Forbidden("You do not have access to this project.");
        }

        var members = await dbContext.ProjectMembers
            .AsNoTracking()
            .Where(member => member.ProjectId == projectId && member.RoleInProject != ProjectMemberRole.Viewer)
            .ToListAsync(cancellationToken);

        var memberIds = members.Select(member => member.UserId).ToList();
        var usersLookup = await dbContext.Users
            .AsNoTracking()
            .Where(appUser => memberIds.Contains(appUser.Id))
            .ToDictionaryAsync(appUser => appUser.Id, cancellationToken);

        var response = members
            .OrderBy(member => usersLookup[member.UserId].DisplayName)
            .Select(member =>
            {
                var appUser = usersLookup[member.UserId];
                return new UserOptionResponse(appUser.Id, appUser.DisplayName, appUser.Email!, member.RoleInProject.ToString());
            })
            .ToArray();

        return Results.Ok(response);
    }
}
