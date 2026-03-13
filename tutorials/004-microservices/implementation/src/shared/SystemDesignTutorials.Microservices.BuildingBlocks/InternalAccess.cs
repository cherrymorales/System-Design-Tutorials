using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SystemDesignTutorials.Microservices.BuildingBlocks;

public static class InternalRequestHeaders
{
    public const string ApiKey = "X-Internal-Api-Key";
    public const string UserId = "X-User-Id";
    public const string UserEmail = "X-User-Email";
    public const string UserName = "X-User-Name";
    public const string UserRole = "X-User-Role";
}

public sealed record ForwardedUserContext(Guid UserId, string Email, string DisplayName, string Role)
{
    public static ForwardedUserContext FromPrincipal(ClaimsPrincipal principal)
    {
        return new ForwardedUserContext(
            Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!),
            principal.FindFirstValue(ClaimTypes.Email)!,
            principal.Identity?.Name ?? principal.FindFirstValue("display_name") ?? principal.FindFirstValue(ClaimTypes.Email)!,
            principal.FindFirstValue(ClaimTypes.Role)!);
    }
}

public static class InternalAccessExtensions
{
    public static bool HasInternalAccess(this HttpContext context, IConfiguration configuration)
    {
        var expectedKey = configuration["InternalApi:ApiKey"];
        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            return false;
        }

        return string.Equals(context.Request.Headers[InternalRequestHeaders.ApiKey], expectedKey, StringComparison.Ordinal);
    }

    public static IResult ForbidInternalAccess(this HttpContext context)
    {
        return Results.Unauthorized();
    }

    public static bool IsInRole(this ForwardedUserContext userContext, params string[] roles)
    {
        return roles.Contains(userContext.Role, StringComparer.Ordinal);
    }

    public static ForwardedUserContext? GetForwardedUser(this HttpContext context)
    {
        var userId = context.Request.Headers[InternalRequestHeaders.UserId].ToString();
        var email = context.Request.Headers[InternalRequestHeaders.UserEmail].ToString();
        var displayName = context.Request.Headers[InternalRequestHeaders.UserName].ToString();
        var role = context.Request.Headers[InternalRequestHeaders.UserRole].ToString();

        return Guid.TryParse(userId, out var parsedId)
            && !string.IsNullOrWhiteSpace(email)
            && !string.IsNullOrWhiteSpace(displayName)
            && !string.IsNullOrWhiteSpace(role)
            ? new ForwardedUserContext(parsedId, email, displayName, role)
            : null;
    }

    public static void ApplyForwardedUser(this HttpClient client, ForwardedUserContext userContext, string apiKey)
    {
        client.DefaultRequestHeaders.Remove(InternalRequestHeaders.ApiKey);
        client.DefaultRequestHeaders.Remove(InternalRequestHeaders.UserId);
        client.DefaultRequestHeaders.Remove(InternalRequestHeaders.UserEmail);
        client.DefaultRequestHeaders.Remove(InternalRequestHeaders.UserName);
        client.DefaultRequestHeaders.Remove(InternalRequestHeaders.UserRole);

        client.DefaultRequestHeaders.Add(InternalRequestHeaders.ApiKey, apiKey);
        client.DefaultRequestHeaders.Add(InternalRequestHeaders.UserId, userContext.UserId.ToString());
        client.DefaultRequestHeaders.Add(InternalRequestHeaders.UserEmail, userContext.Email);
        client.DefaultRequestHeaders.Add(InternalRequestHeaders.UserName, userContext.DisplayName);
        client.DefaultRequestHeaders.Add(InternalRequestHeaders.UserRole, userContext.Role);
    }

    public static void ApplyInternalOnly(this HttpClient client, string apiKey)
    {
        client.DefaultRequestHeaders.Remove(InternalRequestHeaders.ApiKey);
        client.DefaultRequestHeaders.Add(InternalRequestHeaders.ApiKey, apiKey);
    }

    public static void ApplyForwardedUser(this HttpRequestMessage request, ForwardedUserContext userContext, string apiKey)
    {
        request.Headers.Remove(InternalRequestHeaders.ApiKey);
        request.Headers.Remove(InternalRequestHeaders.UserId);
        request.Headers.Remove(InternalRequestHeaders.UserEmail);
        request.Headers.Remove(InternalRequestHeaders.UserName);
        request.Headers.Remove(InternalRequestHeaders.UserRole);

        request.Headers.Add(InternalRequestHeaders.ApiKey, apiKey);
        request.Headers.Add(InternalRequestHeaders.UserId, userContext.UserId.ToString());
        request.Headers.Add(InternalRequestHeaders.UserEmail, userContext.Email);
        request.Headers.Add(InternalRequestHeaders.UserName, userContext.DisplayName);
        request.Headers.Add(InternalRequestHeaders.UserRole, userContext.Role);
    }

    public static void ApplyInternalOnly(this HttpRequestMessage request, string apiKey)
    {
        request.Headers.Remove(InternalRequestHeaders.ApiKey);
        request.Headers.Add(InternalRequestHeaders.ApiKey, apiKey);
    }

    public static RouteGroupBuilder RequireInternalApi(this RouteGroupBuilder group, IConfiguration configuration)
    {
        group.AddEndpointFilter(async (context, next) =>
        {
            if (!context.HttpContext.HasInternalAccess(configuration))
            {
                return Results.Unauthorized();
            }

            return await next(context);
        });

        return group;
    }
}

public interface IDatabaseSeeder<TContext> where TContext : DbContext
{
    Task SeedAsync(TContext dbContext, CancellationToken cancellationToken);
}

public sealed class DatabaseBootstrapHostedService<TContext, TSeeder>(
    IServiceProvider serviceProvider,
    ILogger<DatabaseBootstrapHostedService<TContext, TSeeder>> logger)
    : IHostedService
    where TContext : DbContext
    where TSeeder : class, IDatabaseSeeder<TContext>
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 12;

        for (var attempt = 1; attempt <= maxAttempts; attempt += 1)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                var seeder = scope.ServiceProvider.GetRequiredService<TSeeder>();
                await seeder.SeedAsync(dbContext, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    ex,
                    "Database bootstrap attempt {Attempt}/{MaxAttempts} failed for {Context}. Retrying in 5 seconds.",
                    attempt,
                    maxAttempts,
                    typeof(TContext).Name);

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        throw new InvalidOperationException($"Database bootstrap failed for {typeof(TContext).Name}.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
