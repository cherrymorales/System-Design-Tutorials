using FluentAssertions;
using SystemDesignTutorials.ClientServerSpaApi.Domain;
using SystemDesignTutorials.ClientServerSpaApi.Domain.Entities;

namespace SystemDesignTutorials.ClientServerSpaApi.Tests;

public sealed class TaskCommentTests
{
    [Fact]
    public void New_comment_trims_the_body()
    {
        var comment = new TaskComment(Guid.NewGuid(), Guid.NewGuid(), "  Investigating the blocked route guard.  ");

        comment.Body.Should().Be("Investigating the blocked route guard.");
    }

    [Fact]
    public void Comment_edit_window_expires_after_the_grace_period()
    {
        var comment = new TaskComment(Guid.NewGuid(), Guid.NewGuid(), "Original note.");

        var act = () => comment.UpdateBody(
            "Edited note.",
            comment.CreatedAt.AddMinutes(16),
            TimeSpan.FromMinutes(15));

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("Comment edit window has expired.");
    }
}
