using SystemDesignTutorials.ClientServerSpaApi.Domain.Enums;

namespace SystemDesignTutorials.ClientServerSpaApi.Domain.Entities;

public sealed class Project
{
    private Project()
    {
    }

    public Project(string name, string code, string description, Guid ownerUserId, DateOnly startDate, DateOnly? targetDate)
    {
        Id = Guid.NewGuid();
        Name = RequireText(name, nameof(name), 128);
        Code = RequireText(code, nameof(code), 64).ToUpperInvariant();
        Description = RequireText(description, nameof(description), 2000);
        OwnerUserId = ownerUserId;
        StartDate = startDate;
        TargetDate = targetDate;
        Status = ProjectStatus.Planned;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ProjectStatus Status { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? TargetDate { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void UpdateDetails(string name, string code, string description, DateOnly startDate, DateOnly? targetDate)
    {
        EnsureNotArchived();
        Name = RequireText(name, nameof(name), 128);
        Code = RequireText(code, nameof(code), 64).ToUpperInvariant();
        Description = RequireText(description, nameof(description), 2000);
        StartDate = startDate;
        TargetDate = targetDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        if (Status is ProjectStatus.Completed or ProjectStatus.Archived)
        {
            throw new BusinessRuleException("Completed or archived projects cannot return to active work.");
        }

        Status = ProjectStatus.Active;
        CompletedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAtRisk()
    {
        EnsureNotArchived();
        if (Status == ProjectStatus.Completed)
        {
            throw new BusinessRuleException("Completed projects cannot be marked at risk.");
        }

        Status = ProjectStatus.AtRisk;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        EnsureNotArchived();
        Status = ProjectStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CompletedAt.Value;
    }

    public void Archive()
    {
        Status = ProjectStatus.Archived;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool CanCreateTasks()
    {
        return Status is ProjectStatus.Active or ProjectStatus.AtRisk;
    }

    private void EnsureNotArchived()
    {
        if (Status == ProjectStatus.Archived)
        {
            throw new BusinessRuleException("Archived projects are read-only.");
        }
    }

    private static string RequireText(string value, string parameterName, int maxLength)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BusinessRuleException($"{parameterName} is required.");
        }

        if (normalized.Length > maxLength)
        {
            throw new BusinessRuleException($"{parameterName} must be {maxLength} characters or fewer.");
        }

        return normalized;
    }
}
