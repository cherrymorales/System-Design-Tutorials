using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json.Serialization;
using MassTransit;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.EventDriven.Contracts;
using SystemDesignTutorials.EventDriven.Core;

var builder = WebApplication.CreateBuilder(args);
var messagingOptions = builder.Configuration.GetSection("Messaging").Get<MessagingOptions>() ?? new MessagingOptions();

builder.Services.AddHealthChecks();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanOperateAssets", policy =>
        policy.RequireRole(EventDrivenRoles.ContentOperationsCoordinator, EventDrivenRoles.OperationsManager));
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "eventdriven.auth";
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddDbContext<EventDrivenDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Port=5437;Database=eventdriven;Username=postgres;Password=postgres";

    if (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

builder.Services.AddScoped<AssetWorkflowService>();
builder.Services.AddScoped<ProjectionService>();
builder.Services.AddSingleton<IDatabaseSeeder, NoOpSeeder>();
builder.Services.AddHostedService<DatabaseBootstrapHostedService>();
builder.Services.AddHostedService<OutboxPublisherHostedService>();

builder.Services.AddMassTransit(x =>
{
    if (messagingOptions.UseInMemory)
    {
        x.UsingInMemory((_, cfg) => cfg.ConfigureEndpoints(_));
        return;
    }

    x.UsingRabbitMq((_, cfg) =>
    {
        cfg.Host(messagingOptions.Host, "/", host =>
        {
            host.Username(messagingOptions.Username);
            host.Password(messagingOptions.Password);
        });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "005-event-driven-architecture",
    timestamp = DateTimeOffset.UtcNow,
}));

app.MapGet("/api/auth/seed-users", () => Results.Ok(SeedUsers.All));

app.MapPost("/api/auth/login", async Task<IResult> (LoginRequest request, HttpContext httpContext) =>
{
    var seededUser = SeedUsers.FindByEmail(request.Email);
    if (seededUser is null || request.Password != SeedUsers.DefaultPassword)
    {
        return Results.Unauthorized();
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, seededUser.UserId.ToString()),
        new(ClaimTypes.Email, seededUser.Email),
        new(ClaimTypes.Name, seededUser.DisplayName),
        new(ClaimTypes.Role, seededUser.Role),
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity));

    return Results.Ok(ToCurrentUserFromSeed(seededUser));
});

app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.NoContent();
}).RequireAuthorization();

app.MapGet("/api/auth/me", (HttpContext httpContext) =>
{
    if (httpContext.User.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(ToCurrentUser(httpContext.User));
}).RequireAuthorization();

var api = app.MapGroup("/api").RequireAuthorization();

api.MapGet("/dashboard", async (AssetWorkflowService service, CancellationToken cancellationToken)
    => Results.Ok(await service.GetDashboardAsync(cancellationToken)));

api.MapGet("/assets", async (AssetWorkflowService service, CancellationToken cancellationToken)
    => Results.Ok(await service.ListAssetsAsync(cancellationToken)));

api.MapGet("/assets/{assetId:guid}", async (Guid assetId, AssetWorkflowService service, CancellationToken cancellationToken) =>
{
    var asset = await service.GetAssetAsync(assetId, cancellationToken);
    return asset is null ? Results.NotFound() : Results.Ok(asset);
});

api.MapPost("/assets", async Task<IResult> (
    RegisterAssetRequest request,
    AssetWorkflowService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.RegisterAssetAsync(request, ToCurrentUser(httpContext.User), cancellationToken);
        return Results.Created($"/api/assets/{created.AssetId}", created);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
}).RequireAuthorization("CanOperateAssets");

api.MapPost("/assets/{assetId:guid}/upload-complete", async Task<IResult> (
    Guid assetId,
    AssetWorkflowService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        var detail = await service.MarkUploadCompleteAsync(assetId, cancellationToken);
        return Results.Ok(detail);
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization("CanOperateAssets");

api.MapGet("/notifications", async (AssetWorkflowService service, CancellationToken cancellationToken)
    => Results.Ok(await service.GetNotificationsAsync(cancellationToken)));

app.Run();

static CurrentUserDto ToCurrentUser(ClaimsPrincipal user)
    => new(
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Missing user id claim.")),
        user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
        user.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
        user.FindFirstValue(ClaimTypes.Role) ?? string.Empty);

static CurrentUserDto ToCurrentUserFromSeed(SeedUserDto user)
    => new(user.UserId, user.Email, user.DisplayName, user.Role);

public partial class Program;
