using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SystemDesignTutorials.ClientServerSpaApi.Domain.Entities;
using SystemDesignTutorials.ClientServerSpaApi.Domain.Enums;
using SystemDesignTutorials.ClientServerSpaApi.Infrastructure.Identity;
using SystemDesignTutorials.ClientServerSpaApi.Infrastructure.Persistence;

namespace SystemDesignTutorials.ClientServerSpaApi.Infrastructure.Seeding;

public static class ApplicationDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var dbContext = serviceProvider.GetRequiredService<ClientServerSpaApiDbContext>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppIdentityUser>>();

        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        foreach (var role in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var admin = await SeedUserAsync(userManager, ApplicationRoles.WorkspaceAdmin, "admin@clientserverspa.local", "Workspace Admin");
        var manager = await SeedUserAsync(userManager, ApplicationRoles.ProjectManager, "manager@clientserverspa.local", "Project Manager");
        var alex = await SeedUserAsync(userManager, ApplicationRoles.Contributor, "alex@clientserverspa.local", "Alex Contributor");
        var sam = await SeedUserAsync(userManager, ApplicationRoles.Contributor, "sam@clientserverspa.local", "Sam Contributor");
        var viewer = await SeedUserAsync(userManager, ApplicationRoles.Viewer, "viewer@clientserverspa.local", "Project Viewer");

        var apollo = new Project(
            "Apollo Client Portal",
            "APOLLO-PORTAL",
            "Client-facing portal workstream coordinating dashboard, task, and comment flows.",
            manager.Id,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 6, 30));
        apollo.Activate();

        var atlas = new Project(
            "Atlas Delivery Workspace",
            "ATLAS-DELIVERY",
            "Internal delivery workspace refresh with progress tracking and overdue reporting.",
            manager.Id,
            new DateOnly(2026, 2, 10),
            new DateOnly(2026, 5, 15));
        atlas.Activate();
        atlas.MarkAtRisk();

        var legacy = new Project(
            "Legacy Billing Cleanup",
            "LEGACY-BILLING",
            "Completed cleanup project kept in the system as a read-only historical example.",
            manager.Id,
            new DateOnly(2025, 10, 1),
            new DateOnly(2026, 1, 20));
        legacy.Activate();
        legacy.Complete();

        await dbContext.Projects.AddRangeAsync([apollo, atlas, legacy], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.ProjectMembers.AddRangeAsync(
            [
                new ProjectMember(apollo.Id, manager.Id, ProjectMemberRole.ProjectManager),
                new ProjectMember(apollo.Id, alex.Id, ProjectMemberRole.Contributor),
                new ProjectMember(apollo.Id, sam.Id, ProjectMemberRole.Contributor),
                new ProjectMember(apollo.Id, viewer.Id, ProjectMemberRole.Viewer),
                new ProjectMember(atlas.Id, manager.Id, ProjectMemberRole.ProjectManager),
                new ProjectMember(atlas.Id, sam.Id, ProjectMemberRole.Contributor),
                new ProjectMember(atlas.Id, viewer.Id, ProjectMemberRole.Viewer),
                new ProjectMember(legacy.Id, manager.Id, ProjectMemberRole.ProjectManager),
                new ProjectMember(legacy.Id, viewer.Id, ProjectMemberRole.Viewer),
            ],
            cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var dashboardFilters = new ProjectTask(apollo.Id, "Implement dashboard filters", "Add status and assignee filters to dashboard views.", alex.Id, manager.Id, ProjectTaskPriority.High, today.AddDays(7));
        dashboardFilters.Start(manager.Id);
        dashboardFilters.SubmitReview(alex.Id);

        var authContract = new ProjectTask(apollo.Id, "Align auth session contract", "Ensure /api/auth/me returns role-aware session data for the SPA.", sam.Id, manager.Id, ProjectTaskPriority.Medium, today.AddDays(-1));
        authContract.Start(sam.Id);
        authContract.Block(sam.Id, "Waiting on route authorization review for dashboard and task detail endpoints.");

        var routeShell = new ProjectTask(apollo.Id, "Create route shell", "Wire login, dashboard, project detail, and task detail routes.", alex.Id, manager.Id, ProjectTaskPriority.Medium, today.AddDays(3));

        var reporting = new ProjectTask(atlas.Id, "Review overdue reporting query", "Investigate slow dashboard summary query for overdue work.", sam.Id, manager.Id, ProjectTaskPriority.High, today.AddDays(2));
        reporting.Start(sam.Id);

        var releaseChecklist = new ProjectTask(atlas.Id, "Complete release checklist audit", "Confirm all release readiness items are recorded in one place.", manager.Id, manager.Id, ProjectTaskPriority.Low, today.AddDays(-3));
        releaseChecklist.Start(manager.Id);
        releaseChecklist.SubmitReview(manager.Id);
        releaseChecklist.Complete(manager.Id);

        await dbContext.Tasks.AddRangeAsync([dashboardFilters, authContract, routeShell, reporting, releaseChecklist], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.TaskComments.AddRangeAsync(
            [
                new TaskComment(authContract.Id, sam.Id, "Auth cookie flow is wired. Waiting on final access-control checks before unblocking."),
                new TaskComment(dashboardFilters.Id, alex.Id, "Dashboard filter UI is ready for review after API projections are finalized."),
                new TaskComment(reporting.Id, sam.Id, "The current summary query needs a narrower filter projection."),
            ],
            cancellationToken);

        await dbContext.TaskActivities.AddRangeAsync(
            [
                new TaskActivity(dashboardFilters.Id, TaskActivityType.StatusChanged, manager.Id, "Task moved to InReview."),
                new TaskActivity(authContract.Id, TaskActivityType.StatusChanged, sam.Id, "Task moved to Blocked."),
                new TaskActivity(authContract.Id, TaskActivityType.CommentAdded, sam.Id, "Added blocker context comment."),
                new TaskActivity(releaseChecklist.Id, TaskActivityType.StatusChanged, manager.Id, "Task completed."),
            ],
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<AppIdentityUser> SeedUserAsync(
        UserManager<AppIdentityUser> userManager,
        string role,
        string email,
        string displayName)
    {
        var user = new AppIdentityUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, "Password123!");
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to seed user '{email}': {errors}");
        }

        await userManager.AddToRoleAsync(user, role);
        return user;
    }
}
