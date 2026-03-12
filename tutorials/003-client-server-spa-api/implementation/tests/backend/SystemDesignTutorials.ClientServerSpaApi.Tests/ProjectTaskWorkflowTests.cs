using FluentAssertions;
using SystemDesignTutorials.ClientServerSpaApi.Domain;
using SystemDesignTutorials.ClientServerSpaApi.Domain.Entities;
using SystemDesignTutorials.ClientServerSpaApi.Domain.Enums;

namespace SystemDesignTutorials.ClientServerSpaApi.Tests;

public sealed class ProjectTaskWorkflowTests
{
    [Fact]
    public void Task_can_move_from_backlog_to_done_through_the_valid_flow()
    {
        var task = new ProjectTask(
            Guid.NewGuid(),
            "Implement API shell",
            "Wire the route structure for the SPA API tutorial.",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ProjectTaskPriority.High,
            new DateOnly(2026, 3, 20));

        task.Start(Guid.NewGuid());
        task.SubmitReview(Guid.NewGuid());
        task.Complete(Guid.NewGuid());

        task.Status.Should().Be(ProjectTaskStatus.Done);
        task.CompletedAt.Should().NotBeNull();
        task.BlockerNote.Should().BeNull();
    }

    [Fact]
    public void Blocking_requires_a_note_and_a_valid_source_state()
    {
        var task = new ProjectTask(
            Guid.NewGuid(),
            "Refine filters",
            "Add task filtering to the dashboard.",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ProjectTaskPriority.Medium,
            new DateOnly(2026, 3, 22));

        task.Start(Guid.NewGuid());

        var act = () => task.Block(Guid.NewGuid(), "  ");

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("blockerNote is required.");
    }

    [Fact]
    public void Completed_task_cannot_be_cancelled()
    {
        var task = new ProjectTask(
            Guid.NewGuid(),
            "Publish release note",
            "Close out the milestone handover.",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ProjectTaskPriority.Low,
            new DateOnly(2026, 3, 25));

        task.Start(Guid.NewGuid());
        task.SubmitReview(Guid.NewGuid());
        task.Complete(Guid.NewGuid());

        var act = () => task.Cancel(Guid.NewGuid());

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("Completed tasks cannot be cancelled.");
    }
}
