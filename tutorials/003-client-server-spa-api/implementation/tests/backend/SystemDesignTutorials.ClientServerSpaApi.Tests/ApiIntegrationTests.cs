using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SystemDesignTutorials.ClientServerSpaApi.Tests;

public sealed class ApiIntegrationTests
{
    [Fact]
    public async Task Unauthenticated_projects_request_returns_401()
    {
        await using var factory = new ClientServerSpaApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

        var response = await client.GetAsync("/api/projects");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Project_manager_can_create_project_and_add_member()
    {
        await using var factory = new ClientServerSpaApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        await LoginAsAsync(client, "manager@clientserverspa.local");

        var createResponse = await client.PostAsJsonAsync("/api/projects", new
        {
            name = "Mercury Workspace",
            code = "MERCURY-WS",
            description = "Cross-team client delivery hub.",
            startDate = new DateOnly(2026, 3, 10),
            targetDate = new DateOnly(2026, 6, 1),
        });
        var createBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, createBody);

        var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectDetailResponseDto>();
        createdProject.Should().NotBeNull();

        var workspaceUsers = await client.GetFromJsonAsync<List<WorkspaceUserResponseDto>>("/api/users/workspace");
        var contributor = workspaceUsers!.Single(item => item.Email == "alex@clientserverspa.local");

        var memberResponse = await client.PostAsJsonAsync($"/api/projects/{createdProject!.Id}/members", new
        {
            userId = contributor.Id,
            roleInProject = "Contributor",
        });
        memberResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var members = await client.GetFromJsonAsync<List<ProjectMemberResponseDto>>($"/api/projects/{createdProject.Id}/members");
        members.Should().NotBeNull();
        members!.Select(item => item.Email).Should().Contain(["manager@clientserverspa.local", "alex@clientserverspa.local"]);
    }

    [Fact]
    public async Task Viewer_cannot_create_projects_or_comment_on_tasks()
    {
        await using var factory = new ClientServerSpaApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        await LoginAsAsync(client, "viewer@clientserverspa.local");

        var createProjectResponse = await client.PostAsJsonAsync("/api/projects", new
        {
            name = "Blocked",
            code = "BLOCKED",
            description = "Should fail.",
            startDate = new DateOnly(2026, 3, 10),
            targetDate = new DateOnly(2026, 4, 10),
        });

        createProjectResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var tasks = await client.GetFromJsonAsync<List<ProjectTaskSummaryResponseDto>>("/api/tasks");
        var blockedTask = tasks!.Single(item => item.Title == "Align auth session contract");

        var commentResponse = await client.PostAsJsonAsync($"/api/tasks/{blockedTask.Id}/comments", new
        {
            body = "Viewer should not be able to comment.",
        });

        commentResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Contributor_can_add_comment_to_accessible_task()
    {
        await using var factory = new ClientServerSpaApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        await LoginAsAsync(client, "alex@clientserverspa.local");

        var tasks = await client.GetFromJsonAsync<List<ProjectTaskSummaryResponseDto>>("/api/tasks");
        var task = tasks!.Single(item => item.Title == "Implement dashboard filters");

        var response = await client.PostAsJsonAsync($"/api/tasks/{task.Id}/comments", new
        {
            body = "Filter refinements are ready for review.",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var comments = await response.Content.ReadFromJsonAsync<List<TaskCommentResponseDto>>();
        comments.Should().NotBeNull();
        comments!.Select(item => item.Body).Should().Contain("Filter refinements are ready for review.");
    }

    [Fact]
    public async Task Invalid_task_transition_returns_400()
    {
        await using var factory = new ClientServerSpaApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        await LoginAsAsync(client, "manager@clientserverspa.local");

        var tasks = await client.GetFromJsonAsync<List<ProjectTaskSummaryResponseDto>>("/api/tasks");
        var blockedTask = tasks!.Single(item => item.Title == "Align auth session contract");

        var response = await client.PostAsync($"/api/tasks/{blockedTask.Id}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var payload = await response.Content.ReadFromJsonAsync<MessageResponseDto>();
        payload!.Message.Should().Be("Only review tasks can be completed.");
    }

    [Fact]
    public async Task Overdue_filter_returns_only_overdue_tasks()
    {
        await using var factory = new ClientServerSpaApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        await LoginAsAsync(client, "manager@clientserverspa.local");

        var tasks = await client.GetFromJsonAsync<List<ProjectTaskSummaryResponseDto>>("/api/tasks?overdueOnly=true");

        tasks.Should().NotBeNull();
        tasks.Should().NotBeEmpty();
        var taskList = tasks!;
        taskList.Should().OnlyContain(item => item.IsOverdue);
        taskList.Select(item => item.Title).Should().Contain("Align auth session contract");
    }

    private static async Task LoginAsAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

internal sealed class ClientServerSpaApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"client-server-spa-api-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var sqliteConnectionString = $"Data Source={_databasePath}";

        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", sqliteConnectionString);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = sqliteConnectionString,
            });
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (File.Exists(_databasePath))
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch (IOException)
            {
                // Ignore transient Windows file locks during cleanup.
            }
        }
    }
}

internal sealed record ProjectDetailResponseDto(Guid Id, string Name, string Code, string Description, string Status);
internal sealed record ProjectMemberResponseDto(Guid Id, Guid UserId, string DisplayName, string Email, string RoleInProject, DateTimeOffset JoinedAt);
internal sealed record ProjectTaskSummaryResponseDto(Guid Id, Guid ProjectId, string ProjectName, string ProjectCode, string Title, string Status, string Priority, Guid AssigneeUserId, string AssigneeDisplayName, DateOnly? DueDate, bool IsOverdue, string[] AvailableActions, DateTimeOffset UpdatedAt);
internal sealed record TaskCommentResponseDto(Guid Id, Guid TaskId, Guid AuthorUserId, string AuthorDisplayName, string Body, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, bool CanEdit);
internal sealed record WorkspaceUserResponseDto(Guid Id, string DisplayName, string Email, string[] Roles);
internal sealed record MessageResponseDto(string Message);
