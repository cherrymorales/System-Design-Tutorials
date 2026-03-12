namespace SystemDesignTutorials.ClientServerSpaApi.Web.Endpoints;

internal sealed record UserSessionResponse(string DisplayName, string Email, string[] Roles);

internal sealed record DashboardSummaryResponse(
    int ActiveProjectCount,
    int AtRiskProjectCount,
    int OverdueTaskCount,
    int TasksInReviewCount,
    int MyOpenTaskCount);

internal sealed record ProjectSummaryResponse(
    Guid Id,
    string Name,
    string Code,
    string Description,
    string Status,
    Guid OwnerUserId,
    string OwnerDisplayName,
    DateOnly StartDate,
    DateOnly? TargetDate,
    int MemberCount,
    int TotalTaskCount,
    int OpenTaskCount,
    int OverdueTaskCount);

internal sealed record ProjectMemberResponse(
    Guid Id,
    Guid UserId,
    string DisplayName,
    string Email,
    string RoleInProject,
    DateTimeOffset JoinedAt);

internal sealed record ProjectTaskSummaryResponse(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    string ProjectCode,
    string Title,
    string Status,
    string Priority,
    Guid AssigneeUserId,
    string AssigneeDisplayName,
    DateOnly? DueDate,
    bool IsOverdue,
    string[] AvailableActions,
    DateTimeOffset UpdatedAt);

internal sealed record ProjectDetailResponse(
    Guid Id,
    string Name,
    string Code,
    string Description,
    string Status,
    Guid OwnerUserId,
    string OwnerDisplayName,
    DateOnly StartDate,
    DateOnly? TargetDate,
    DateTimeOffset? CompletedAt,
    bool CanManageProject,
    bool CanManageMembership,
    bool CanManageTasks,
    ProjectMemberResponse[] Members,
    ProjectTaskSummaryResponse[] Tasks);

internal sealed record TaskCommentResponse(
    Guid Id,
    Guid TaskId,
    Guid AuthorUserId,
    string AuthorDisplayName,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool CanEdit);

internal sealed record TaskActivityResponse(
    Guid Id,
    string Type,
    string ActorDisplayName,
    string Summary,
    DateTimeOffset CreatedAt);

internal sealed record ProjectTaskDetailResponse(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    string ProjectCode,
    string ProjectStatus,
    string Title,
    string Description,
    string Status,
    string Priority,
    Guid AssigneeUserId,
    string AssigneeDisplayName,
    Guid CreatedByUserId,
    string CreatedByDisplayName,
    string? BlockerNote,
    DateOnly? DueDate,
    DateTimeOffset? CompletedAt,
    DateTimeOffset UpdatedAt,
    bool CanEditTask,
    bool CanComment,
    bool CanUpdateWorkflow,
    string[] AvailableActions,
    TaskCommentResponse[] Comments,
    TaskActivityResponse[] Activity);

internal sealed record UserOptionResponse(Guid Id, string DisplayName, string Email, string RoleInProject);

internal sealed record WorkspaceUserResponse(Guid Id, string DisplayName, string Email, string[] Roles);
