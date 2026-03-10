using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.ClientServerSpaApi.Domain;
using SystemDesignTutorials.ClientServerSpaApi.Domain.Entities;
using SystemDesignTutorials.ClientServerSpaApi.Domain.Enums;
using SystemDesignTutorials.ClientServerSpaApi.Infrastructure.Persistence;
using SystemDesignTutorials.ClientServerSpaApi.Web.Contracts;

namespace SystemDesignTutorials.ClientServerSpaApi.Web.Endpoints;

internal static class TasksEndpoints
{
    private static readonly TimeSpan CommentEditGracePeriod = TimeSpan.FromMinutes(15);

    public static void MapTasksEndpoints(this RouteGroupBuilder api)
    {
        var tasks = api.MapGroup("/tasks");
        tasks.MapGet("/", GetTasksAsync);
        tasks.MapPost("/", CreateTaskAsync);
        tasks.MapGet("/{id:guid}", GetTaskByIdAsync);
        tasks.MapPut("/{id:guid}", UpdateTaskAsync);
        tasks.MapPost("/{id:guid}/start", StartTaskAsync);
        tasks.MapPost("/{id:guid}/block", BlockTaskAsync);
        tasks.MapPost("/{id:guid}/submit-review", SubmitReviewTaskAsync);
        tasks.MapPost("/{id:guid}/complete", CompleteTaskAsync);
        tasks.MapPost("/{id:guid}/cancel", CancelTaskAsync);
        tasks.MapGet("/{id:guid}/comments", GetTaskCommentsAsync);
        tasks.MapPost("/{id:guid}/comments", AddTaskCommentAsync);
        tasks.MapPut("/{id:guid}/comments/{commentId:guid}", UpdateTaskCommentAsync);
    }

    private static async Task<IResult> GetTasksAsync(
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        Guid? projectId = null,
        Guid? assigneeUserId = null,
        string? status = null,
        bool overdueOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (projectId.HasValue && !await AccessControl.HasProjectAccessAsync(user, dbContext, projectId.Value, cancellationToken))
        {
            return AccessControl.Forbidden("You do not have access to this project.");
        }

        var accessibleProjectIds = await AccessControl.GetAccessibleProjectsQuery(user, dbContext)
            .Select(project => project.Id)
            .ToListAsync(cancellationToken);

        var query = dbContext.Tasks
            .AsNoTracking()
            .Where(task => accessibleProjectIds.Contains(task.ProjectId));

        if (projectId.HasValue)
        {
            query = query.Where(task => task.ProjectId == projectId.Value);
        }

        if (assigneeUserId.HasValue)
        {
            query = query.Where(task => task.AssigneeUserId == assigneeUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ProjectTaskStatus>(status, ignoreCase: true, out var parsedStatus))
            {
                return Results.BadRequest(new { message = "Task status filter is invalid." });
            }

            query = query.Where(task => task.Status == parsedStatus);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (overdueOnly)
        {
            query = query.Where(task =>
                task.DueDate.HasValue
                && task.DueDate.Value < today
                && task.Status != ProjectTaskStatus.Done
                && task.Status != ProjectTaskStatus.Cancelled);
        }

        var tasks = await query
            .OrderBy(task => task.DueDate)
            .ThenBy(task => task.Title)
            .ToListAsync(cancellationToken);

        var projectIds = tasks.Select(task => task.ProjectId).Distinct().ToList();
        var projects = projectIds.Count == 0
            ? new Dictionary<Guid, Project>()
            : await dbContext.Projects
                .AsNoTracking()
                .Where(project => projectIds.Contains(project.Id))
                .ToDictionaryAsync(project => project.Id, cancellationToken);
        var userIds = tasks.Select(task => task.AssigneeUserId).Distinct().ToList();
        var users = userIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await dbContext.Users
                .AsNoTracking()
                .Where(appUser => userIds.Contains(appUser.Id))
                .ToDictionaryAsync(appUser => appUser.Id, appUser => appUser.DisplayName, cancellationToken);

        var response = tasks.Select(task =>
        {
            var project = projects[task.ProjectId];
            return new ProjectTaskSummaryResponse(
                task.Id,
                task.ProjectId,
                project.Name,
                project.Code,
                task.Title,
                task.Status.ToString(),
                task.Priority.ToString(),
                task.AssigneeUserId,
                users.GetValueOrDefault(task.AssigneeUserId, "Unknown user"),
                task.DueDate,
                task.IsOverdue(today),
                AccessControl.GetAvailableTaskActions(user, project, task),
                task.UpdatedAt);
        }).ToArray();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetTaskByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (task is null)
        {
            return Results.NotFound();
        }

        var project = await dbContext.Projects.SingleAsync(item => item.Id == task.ProjectId, cancellationToken);
        if (!await AccessControl.HasProjectAccessAsync(user, dbContext, project.Id, cancellationToken))
        {
            return AccessControl.Forbidden("You do not have access to this task.");
        }

        return Results.Ok(await BuildTaskDetailResponseAsync(user, dbContext, project, task, cancellationToken));
    }

    private static async Task<IResult> CreateTaskAsync(
        CreateTaskRequest request,
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.SingleOrDefaultAsync(item => item.Id == request.ProjectId, cancellationToken);
        if (project is null)
        {
            return Results.BadRequest(new { message = "Project does not exist." });
        }

        if (!AccessControl.CanManageTaskStructure(user, project))
        {
            return AccessControl.Forbidden("Only the project owner or a workspace admin can create tasks.");
        }

        if (!project.CanCreateTasks())
        {
            return Results.BadRequest(new { message = "Tasks can only be created for active or at-risk projects." });
        }

        if (!Enum.TryParse<ProjectTaskPriority>(request.Priority, ignoreCase: true, out var priority))
        {
            return Results.BadRequest(new { message = "Priority is invalid." });
        }

        var assigneeMembership = await dbContext.ProjectMembers.SingleOrDefaultAsync(
            member => member.ProjectId == project.Id && member.UserId == request.AssigneeUserId,
            cancellationToken);

        if (assigneeMembership is null || !AccessControl.IsAssignableRole(assigneeMembership.RoleInProject))
        {
            return Results.BadRequest(new { message = "Assignee must be an assignable member of the project." });
        }

        try
        {
            var actorUserId = AccessControl.GetRequiredUserId(user);
            var task = new ProjectTask(project.Id, request.Title, request.Description, request.AssigneeUserId, actorUserId, priority, request.DueDate);
            await dbContext.Tasks.AddAsync(task, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            await dbContext.TaskActivities.AddAsync(
                new TaskActivity(task.Id, TaskActivityType.Created, actorUserId, "Task created."),
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Created($"/api/tasks/{task.Id}", await BuildTaskDetailResponseAsync(user, dbContext, project, task, cancellationToken));
        }
        catch (BusinessRuleException error)
        {
            return Results.BadRequest(new { message = error.Message });
        }
    }

    private static async Task<IResult> UpdateTaskAsync(
        Guid id,
        UpdateTaskRequest request,
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (task is null)
        {
            return Results.NotFound();
        }

        var project = await dbContext.Projects.SingleAsync(item => item.Id == task.ProjectId, cancellationToken);
        if (!AccessControl.CanManageTaskStructure(user, project))
        {
            return AccessControl.Forbidden("Only the project owner or a workspace admin can edit task details.");
        }

        if (!Enum.TryParse<ProjectTaskPriority>(request.Priority, ignoreCase: true, out var priority))
        {
            return Results.BadRequest(new { message = "Priority is invalid." });
        }

        var assigneeMembership = await dbContext.ProjectMembers.SingleOrDefaultAsync(
            member => member.ProjectId == project.Id && member.UserId == request.AssigneeUserId,
            cancellationToken);
        if (assigneeMembership is null || !AccessControl.IsAssignableRole(assigneeMembership.RoleInProject))
        {
            return Results.BadRequest(new { message = "Assignee must be an assignable member of the project." });
        }

        try
        {
            var actorUserId = AccessControl.GetRequiredUserId(user);
            var assigneeChanged = task.AssigneeUserId != request.AssigneeUserId;

            task.UpdateDetails(request.Title, request.Description, request.AssigneeUserId, priority, request.DueDate);
            await dbContext.TaskActivities.AddAsync(
                new TaskActivity(task.Id, TaskActivityType.Updated, actorUserId, "Task details updated."),
                cancellationToken);
            if (assigneeChanged)
            {
                await dbContext.TaskActivities.AddAsync(
                    new TaskActivity(task.Id, TaskActivityType.AssignmentChanged, actorUserId, "Task assignee changed."),
                    cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(await BuildTaskDetailResponseAsync(user, dbContext, project, task, cancellationToken));
        }
        catch (BusinessRuleException error)
        {
            return Results.BadRequest(new { message = error.Message });
        }
    }

    private static async Task<IResult> StartTaskAsync(Guid id, ClaimsPrincipal user, ClientServerSpaApiDbContext dbContext, CancellationToken cancellationToken)
        => await RunTaskWorkflowAsync(id, user, dbContext, (task, actorUserId) =>
        {
            task.Start(actorUserId);
            return new TaskActivity(task.Id, TaskActivityType.StatusChanged, actorUserId, "Task moved to InProgress.");
        }, cancellationToken);

    private static async Task<IResult> BlockTaskAsync(Guid id, BlockTaskRequest request, ClaimsPrincipal user, ClientServerSpaApiDbContext dbContext, CancellationToken cancellationToken)
        => await RunTaskWorkflowAsync(id, user, dbContext, (task, actorUserId) =>
        {
            task.Block(actorUserId, request.BlockerNote);
            return new TaskActivity(task.Id, TaskActivityType.StatusChanged, actorUserId, "Task moved to Blocked.");
        }, cancellationToken);

    private static async Task<IResult> SubmitReviewTaskAsync(Guid id, ClaimsPrincipal user, ClientServerSpaApiDbContext dbContext, CancellationToken cancellationToken)
        => await RunTaskWorkflowAsync(id, user, dbContext, (task, actorUserId) =>
        {
            task.SubmitReview(actorUserId);
            return new TaskActivity(task.Id, TaskActivityType.StatusChanged, actorUserId, "Task moved to InReview.");
        }, cancellationToken);

    private static async Task<IResult> CompleteTaskAsync(Guid id, ClaimsPrincipal user, ClientServerSpaApiDbContext dbContext, CancellationToken cancellationToken)
        => await RunTaskWorkflowAsync(id, user, dbContext, (task, actorUserId) =>
        {
            task.Complete(actorUserId);
            return new TaskActivity(task.Id, TaskActivityType.StatusChanged, actorUserId, "Task moved to Done.");
        }, cancellationToken);

    private static async Task<IResult> CancelTaskAsync(Guid id, ClaimsPrincipal user, ClientServerSpaApiDbContext dbContext, CancellationToken cancellationToken)
        => await RunTaskWorkflowAsync(id, user, dbContext, (task, actorUserId) =>
        {
            task.Cancel(actorUserId);
            return new TaskActivity(task.Id, TaskActivityType.StatusChanged, actorUserId, "Task moved to Cancelled.");
        }, cancellationToken);

    private static async Task<IResult> GetTaskCommentsAsync(
        Guid id,
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (task is null)
        {
            return Results.NotFound();
        }

        var project = await dbContext.Projects.SingleAsync(item => item.Id == task.ProjectId, cancellationToken);
        if (!await AccessControl.HasProjectAccessAsync(user, dbContext, project.Id, cancellationToken))
        {
            return AccessControl.Forbidden("You do not have access to this task.");
        }

        return Results.Ok(await BuildCommentResponsesAsync(user, dbContext, task.Id, cancellationToken));
    }

    private static async Task<IResult> AddTaskCommentAsync(
        Guid id,
        AddTaskCommentRequest request,
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (task is null)
        {
            return Results.NotFound();
        }

        var project = await dbContext.Projects.SingleAsync(item => item.Id == task.ProjectId, cancellationToken);
        if (!await AccessControl.HasProjectAccessAsync(user, dbContext, project.Id, cancellationToken))
        {
            return AccessControl.Forbidden("You do not have access to this task.");
        }

        try
        {
            var actorUserId = AccessControl.GetRequiredUserId(user);
            if (!await AccessControl.CanCommentAsync(user, dbContext, project.Id, cancellationToken))
            {
                return AccessControl.Forbidden("Your project role is read-only and cannot add comments.");
            }

            await dbContext.TaskComments.AddAsync(new TaskComment(task.Id, actorUserId, request.Body), cancellationToken);
            await dbContext.TaskActivities.AddAsync(new TaskActivity(task.Id, TaskActivityType.CommentAdded, actorUserId, "Task comment added."), cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(await BuildCommentResponsesAsync(user, dbContext, task.Id, cancellationToken));
        }
        catch (BusinessRuleException error)
        {
            return Results.BadRequest(new { message = error.Message });
        }
    }

    private static async Task<IResult> UpdateTaskCommentAsync(
        Guid id,
        Guid commentId,
        UpdateTaskCommentRequest request,
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (task is null)
        {
            return Results.NotFound();
        }

        var project = await dbContext.Projects.SingleAsync(item => item.Id == task.ProjectId, cancellationToken);
        if (!await AccessControl.HasProjectAccessAsync(user, dbContext, project.Id, cancellationToken))
        {
            return AccessControl.Forbidden("You do not have access to this task.");
        }

        var comment = await dbContext.TaskComments.SingleOrDefaultAsync(item => item.Id == commentId && item.TaskId == id, cancellationToken);
        if (comment is null)
        {
            return Results.NotFound();
        }

        var actorUserId = AccessControl.GetRequiredUserId(user);
        if (comment.AuthorUserId != actorUserId)
        {
            return AccessControl.Forbidden("Only the comment author can edit the comment.");
        }

        try
        {
            comment.UpdateBody(request.Body, DateTimeOffset.UtcNow, CommentEditGracePeriod);
            await dbContext.TaskActivities.AddAsync(new TaskActivity(task.Id, TaskActivityType.CommentEdited, actorUserId, "Task comment edited."), cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(await BuildCommentResponsesAsync(user, dbContext, task.Id, cancellationToken));
        }
        catch (BusinessRuleException error)
        {
            return Results.BadRequest(new { message = error.Message });
        }
    }

    private static async Task<IResult> RunTaskWorkflowAsync(
        Guid taskId,
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        Func<ProjectTask, Guid, TaskActivity> transition,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.SingleOrDefaultAsync(item => item.Id == taskId, cancellationToken);
        if (task is null)
        {
            return Results.NotFound();
        }

        var project = await dbContext.Projects.SingleAsync(item => item.Id == task.ProjectId, cancellationToken);
        if (!AccessControl.CanUpdateTaskWorkflow(user, project, task))
        {
            return AccessControl.Forbidden("Only the project owner, task assignee, or a workspace admin can move this task.");
        }

        try
        {
            var actorUserId = AccessControl.GetRequiredUserId(user);
            var activity = transition(task, actorUserId);
            await dbContext.TaskActivities.AddAsync(activity, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(await BuildTaskDetailResponseAsync(user, dbContext, project, task, cancellationToken));
        }
        catch (BusinessRuleException error)
        {
            return Results.BadRequest(new { message = error.Message });
        }
    }

    internal static async Task<ProjectTaskDetailResponse> BuildTaskDetailResponseAsync(
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        Project project,
        ProjectTask task,
        CancellationToken cancellationToken)
    {
        var comments = await BuildCommentResponsesAsync(user, dbContext, task.Id, cancellationToken);
        var activity = await BuildActivityResponsesAsync(dbContext, task.Id, cancellationToken);
        var canComment = await AccessControl.CanCommentAsync(user, dbContext, project.Id, cancellationToken);

        var userIds = comments.Select(comment => comment.AuthorUserId)
            .Append(task.AssigneeUserId)
            .Append(task.CreatedByUserId)
            .Distinct()
            .ToList();
        var userNames = userIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await dbContext.Users
                .AsNoTracking()
                .Where(appUser => userIds.Contains(appUser.Id))
                .ToDictionaryAsync(appUser => appUser.Id, appUser => appUser.DisplayName, cancellationToken);

        var canUpdateWorkflow = AccessControl.CanUpdateTaskWorkflow(user, project, task);
        var canManageTask = AccessControl.CanManageTaskStructure(user, project);
        return new ProjectTaskDetailResponse(
            task.Id,
            task.ProjectId,
            project.Name,
            project.Code,
            project.Status.ToString(),
            task.Title,
            task.Description,
            task.Status.ToString(),
            task.Priority.ToString(),
            task.AssigneeUserId,
            userNames.GetValueOrDefault(task.AssigneeUserId, "Unknown user"),
            task.CreatedByUserId,
            userNames.GetValueOrDefault(task.CreatedByUserId, "Unknown user"),
            task.BlockerNote,
            task.DueDate,
            task.CompletedAt,
            task.UpdatedAt,
            canManageTask,
            canComment,
            canUpdateWorkflow,
            AccessControl.GetAvailableTaskActions(user, project, task),
            comments,
            activity);
    }

    internal static async Task<TaskCommentResponse[]> BuildCommentResponsesAsync(
        ClaimsPrincipal user,
        ClientServerSpaApiDbContext dbContext,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var comments = await dbContext.TaskComments
            .AsNoTracking()
            .Where(comment => comment.TaskId == taskId)
            .ToListAsync(cancellationToken);
        comments = comments
            .OrderBy(comment => comment.CreatedAt)
            .ToList();
        var userIds = comments.Select(comment => comment.AuthorUserId).Distinct().ToList();
        var users = userIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await dbContext.Users
                .AsNoTracking()
                .Where(appUser => userIds.Contains(appUser.Id))
                .ToDictionaryAsync(appUser => appUser.Id, appUser => appUser.DisplayName, cancellationToken);
        var currentUserId = AccessControl.GetRequiredUserId(user);
        var now = DateTimeOffset.UtcNow;

        return comments.Select(comment =>
            new TaskCommentResponse(
                comment.Id,
                comment.TaskId,
                comment.AuthorUserId,
                users.GetValueOrDefault(comment.AuthorUserId, "Unknown user"),
                comment.Body,
                comment.CreatedAt,
                comment.UpdatedAt,
                comment.AuthorUserId == currentUserId && now - comment.CreatedAt <= CommentEditGracePeriod))
            .ToArray();
    }

    internal static async Task<TaskActivityResponse[]> BuildActivityResponsesAsync(
        ClientServerSpaApiDbContext dbContext,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var activity = await dbContext.TaskActivities
            .AsNoTracking()
            .Where(item => item.TaskId == taskId)
            .ToListAsync(cancellationToken);
        activity = activity
            .OrderByDescending(item => item.CreatedAt)
            .ToList();
        var actorIds = activity.Select(item => item.ActorUserId).Distinct().ToList();
        var users = actorIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await dbContext.Users
                .AsNoTracking()
                .Where(appUser => actorIds.Contains(appUser.Id))
                .ToDictionaryAsync(appUser => appUser.Id, appUser => appUser.DisplayName, cancellationToken);

        return activity.Select(item =>
            new TaskActivityResponse(
                item.Id,
                item.Type.ToString(),
                users.GetValueOrDefault(item.ActorUserId, "Unknown user"),
                item.Summary,
                item.CreatedAt))
            .ToArray();
    }
}
