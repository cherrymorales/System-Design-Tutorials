using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Identity;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Persistence;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Seeding;

namespace SystemDesignTutorials.ModularMonolith.Web.Endpoints;

internal static class AccessControl
{
    public static string GetRequiredEmail(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Email)
            ?? user.Identity?.Name
            ?? throw new InvalidOperationException("Authenticated email claim is missing.");

    public static bool IsSales(ClaimsPrincipal user) => user.IsInRole(ApplicationRoles.SalesCoordinator);
    public static bool IsWarehouse(ClaimsPrincipal user) => user.IsInRole(ApplicationRoles.WarehouseOperator);
    public static bool IsFinance(ClaimsPrincipal user) => user.IsInRole(ApplicationRoles.FinanceOfficer);
    public static bool IsManager(ClaimsPrincipal user) => user.IsInRole(ApplicationRoles.OperationsManager);

    public static bool CanManageCustomers(ClaimsPrincipal user) => IsSales(user) || IsManager(user);
    public static bool CanManageCatalog(ClaimsPrincipal user) => IsManager(user);
    public static bool CanManageOrders(ClaimsPrincipal user) => IsSales(user) || IsManager(user);
    public static bool CanReadyOrders(ClaimsPrincipal user) => IsWarehouse(user) || IsManager(user);
    public static bool CanManageBilling(ClaimsPrincipal user) => IsFinance(user) || IsManager(user);
    public static bool CanViewInventory(ClaimsPrincipal user) => IsWarehouse(user) || IsSales(user) || IsManager(user);
    public static bool CanViewReports(ClaimsPrincipal user) => IsManager(user);

    public static IResult Forbidden(string message)
        => Results.Json(new { message }, statusCode: StatusCodes.Status403Forbidden);
}

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

        var result = await signInManager.PasswordSignInAsync(user, request.Password, isPersistent: false, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return Results.Unauthorized();
        }

        var session = await BuildSessionResponseAsync(userManager, user);
        return Results.Ok(session);
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

        var session = await BuildSessionResponseAsync(userManager, appUser);
        return Results.Ok(session);
    }

    private static async Task<UserSessionResponse> BuildSessionResponseAsync(UserManager<AppIdentityUser> userManager, AppIdentityUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserSessionResponse(user.DisplayName, user.Email!, roles.ToArray());
    }
}

internal sealed record UserSessionResponse(string DisplayName, string Email, string[] Roles);
