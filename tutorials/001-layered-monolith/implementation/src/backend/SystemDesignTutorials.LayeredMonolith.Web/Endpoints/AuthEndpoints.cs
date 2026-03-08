using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.LayeredMonolith.Infrastructure.Identity;
using SystemDesignTutorials.LayeredMonolith.Infrastructure.Persistence;
using SystemDesignTutorials.LayeredMonolith.Infrastructure.Seeding;
using SystemDesignTutorials.LayeredMonolith.Web.Contracts;

namespace SystemDesignTutorials.LayeredMonolith.Web.Endpoints;

internal static class AccessControl
{
    public static string GetRequiredEmail(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Email)
            ?? user.Identity?.Name
            ?? throw new InvalidOperationException("Authenticated email claim is missing.");

    public static Guid GetRequiredUserId(ClaimsPrincipal user)
        => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new InvalidOperationException("Authenticated user id claim is missing.");

    public static bool IsOperator(ClaimsPrincipal user) => user.IsInRole(ApplicationRoles.WarehouseOperator);
    public static bool IsPlanner(ClaimsPrincipal user) => user.IsInRole(ApplicationRoles.InventoryPlanner);
    public static bool IsPurchasing(ClaimsPrincipal user) => user.IsInRole(ApplicationRoles.PurchasingOfficer);
    public static bool IsManager(ClaimsPrincipal user) => user.IsInRole(ApplicationRoles.OperationsManager);

    public static bool CanManageProducts(ClaimsPrincipal user) => IsPurchasing(user) || IsManager(user);
    public static bool CanManageWarehouses(ClaimsPrincipal user) => IsManager(user);
    public static bool CanRecordReceipts(ClaimsPrincipal user) => IsOperator(user) || IsPurchasing(user) || IsManager(user);
    public static bool CanCreateTransfers(ClaimsPrincipal user) => IsPlanner(user) || IsManager(user);
    public static bool CanApproveTransfers(ClaimsPrincipal user) => IsPlanner(user) || IsManager(user);
    public static bool CanDispatchTransfers(ClaimsPrincipal user) => IsOperator(user) || IsManager(user);
    public static bool CanReceiveTransfers(ClaimsPrincipal user) => IsOperator(user) || IsManager(user);
    public static bool CanCancelTransfers(ClaimsPrincipal user) => IsManager(user);
    public static bool CanCreateAdjustments(ClaimsPrincipal user) => IsOperator(user) || IsManager(user);
    public static bool CanReviewAdjustments(ClaimsPrincipal user) => IsManager(user);

    public static IResult Forbidden(string message)
        => Results.Json(new { message }, statusCode: StatusCodes.Status403Forbidden);

    public static async Task<HashSet<Guid>> GetAssignedWarehouseIdsAsync(
        ClaimsPrincipal user,
        LayeredMonolithDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!IsOperator(user))
        {
            return [];
        }

        var userId = GetRequiredUserId(user);
        return await dbContext.UserWarehouseAssignments
            .Where(assignment => assignment.UserId == userId)
            .Select(assignment => assignment.WarehouseId)
            .ToHashSetAsync(cancellationToken);
    }

    public static async Task<bool> CanAccessWarehouseAsync(
        ClaimsPrincipal user,
        LayeredMonolithDbContext dbContext,
        Guid warehouseId,
        CancellationToken cancellationToken)
    {
        if (!IsOperator(user))
        {
            return true;
        }

        var userId = GetRequiredUserId(user);
        return await dbContext.UserWarehouseAssignments.AnyAsync(
            assignment => assignment.UserId == userId && assignment.WarehouseId == warehouseId,
            cancellationToken);
    }
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
        LayeredMonolithDbContext dbContext,
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

        var session = await BuildSessionResponseAsync(userManager, dbContext, user, cancellationToken);
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
        LayeredMonolithDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var appUser = await userManager.GetUserAsync(user);
        if (appUser is null)
        {
            return Results.Unauthorized();
        }

        var session = await BuildSessionResponseAsync(userManager, dbContext, appUser, cancellationToken);
        return Results.Ok(session);
    }

    private static async Task<UserSessionResponse> BuildSessionResponseAsync(
        UserManager<AppIdentityUser> userManager,
        LayeredMonolithDbContext dbContext,
        AppIdentityUser user,
        CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var assignedWarehouseIds = await dbContext.UserWarehouseAssignments
            .Where(assignment => assignment.UserId == user.Id)
            .Select(assignment => assignment.WarehouseId)
            .ToArrayAsync(cancellationToken);

        return new UserSessionResponse(user.DisplayName, user.Email!, roles.ToArray(), assignedWarehouseIds);
    }
}

internal sealed record UserSessionResponse(string DisplayName, string Email, string[] Roles, Guid[] AssignedWarehouseIds);
