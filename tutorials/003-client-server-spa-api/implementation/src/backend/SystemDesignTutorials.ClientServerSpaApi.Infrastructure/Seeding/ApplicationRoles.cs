namespace SystemDesignTutorials.ClientServerSpaApi.Infrastructure.Seeding;

public static class ApplicationRoles
{
    public const string WorkspaceAdmin = nameof(WorkspaceAdmin);
    public const string ProjectManager = nameof(ProjectManager);
    public const string Contributor = nameof(Contributor);
    public const string Viewer = nameof(Viewer);

    public static readonly string[] All =
    [
        WorkspaceAdmin,
        ProjectManager,
        Contributor,
        Viewer,
    ];
}
