using SystemDesignTutorials.EventDriven.Contracts;

namespace SystemDesignTutorials.EventDriven.Core;

public static class SeedUsers
{
    public const string DefaultPassword = "Password123!";

    private static readonly IReadOnlyList<SeedUserDto> Users =
    [
        new(Guid.Parse("4d6ccfc0-7fb6-4588-8642-9e0e53f20801"), "coordinator@eventdriven.local", "Content Operations Coordinator", EventDrivenRoles.ContentOperationsCoordinator),
        new(Guid.Parse("4d6ccfc0-7fb6-4588-8642-9e0e53f20802"), "manager@eventdriven.local", "Operations Manager", EventDrivenRoles.OperationsManager),
    ];

    public static IReadOnlyList<SeedUserDto> All => Users;

    public static SeedUserDto? FindByEmail(string email)
        => Users.FirstOrDefault(user => string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase));
}
