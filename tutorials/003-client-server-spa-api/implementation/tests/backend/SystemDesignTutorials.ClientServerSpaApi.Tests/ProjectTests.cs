using FluentAssertions;
using SystemDesignTutorials.ClientServerSpaApi.Domain;
using SystemDesignTutorials.ClientServerSpaApi.Domain.Entities;

namespace SystemDesignTutorials.ClientServerSpaApi.Tests;

public sealed class ProjectTests
{
    [Fact]
    public void New_project_starts_planned_and_cannot_create_tasks_until_activated()
    {
        var project = new Project(
            "Apollo",
            "apollo",
            "Client delivery workspace.",
            Guid.NewGuid(),
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 6, 1));

        project.Status.ToString().Should().Be("Planned");
        project.Code.Should().Be("APOLLO");
        project.CanCreateTasks().Should().BeFalse();

        project.Activate();
        project.CanCreateTasks().Should().BeTrue();
    }

    [Fact]
    public void Archived_project_is_read_only()
    {
        var project = new Project(
            "Atlas",
            "atlas",
            "Operations workspace.",
            Guid.NewGuid(),
            new DateOnly(2026, 1, 10),
            new DateOnly(2026, 4, 10));

        project.Activate();
        project.Archive();

        var act = () => project.UpdateDetails(
            "Atlas v2",
            "atlas-v2",
            "Updated description.",
            new DateOnly(2026, 1, 10),
            new DateOnly(2026, 4, 15));

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("Archived projects are read-only.");
        project.CanCreateTasks().Should().BeFalse();
    }
}
