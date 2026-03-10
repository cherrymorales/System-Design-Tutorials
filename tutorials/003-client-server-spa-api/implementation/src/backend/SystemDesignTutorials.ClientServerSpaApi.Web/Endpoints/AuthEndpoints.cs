using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.ClientServerSpaApi.Infrastructure.Identity;
using SystemDesignTutorials.ClientServerSpaApi.Infrastructure.Persistence;
using SystemDesignTutorials.ClientServerSpaApi.Web.Contracts;

namespace SystemDesignTutorials.ClientServerSpaApi.Web.Endpoints;

internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth");
        auth.MapPost("/login", LoginAsync);
        auth.MapPost("/logout", LogoutAsync).RequireAuthorization();
        auth.MapGet("/me", MeAsync).RequireAuthorization();
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        SignInManager<AppIdentityUser> signInManager,
        UserManager<AppIdentityUser> userManager,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var user = await userManager.Users.SingleOrDefaultAsync(item => item.Email == email, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return Results.Unauthorized();
        }

        await signInManager.SignInWithClaimsAsync(
            user,
            isPersistent: false,
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            ]);

        return Results.Ok(await BuildSessionResponseAsync(userManager, user));
    }

    private static async Task<IResult> LogoutAsync(SignInManager<AppIdentityUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> MeAsync(
        ClaimsPrincipal user,
        UserManager<AppIdentityUser> userManager,
        CancellationToken cancellationToken)
    {
        var appUser = await userManager.GetUserAsync(user);
        if (appUser is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(await BuildSessionResponseAsync(userManager, appUser));
    }

    private static async Task<UserSessionResponse> BuildSessionResponseAsync(UserManager<AppIdentityUser> userManager, AppIdentityUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserSessionResponse(user.DisplayName, user.Email!, roles.ToArray());
    }
}
