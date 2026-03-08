using Microsoft.AspNetCore.Identity;

namespace SystemDesignTutorials.LayeredMonolith.Infrastructure.Identity;

public sealed class AppIdentityUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
}
