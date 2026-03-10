using Microsoft.AspNetCore.Identity;

namespace SystemDesignTutorials.ClientServerSpaApi.Infrastructure.Identity;

public sealed class AppIdentityUser : IdentityUser<Guid>
{
    public string DisplayName { get; init; } = string.Empty;
}
