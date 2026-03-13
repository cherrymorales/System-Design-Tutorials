using Microsoft.AspNetCore.Identity;
using SystemDesignTutorials.Microservices.BuildingBlocks;
using SystemDesignTutorials.Microservices.Contracts;
using SystemDesignTutorials.Microservices.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConfiguredDbContext<IdentityDbContext>(builder.Configuration, "Identity");
builder.Services.AddSingleton<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
builder.Services.AddScoped<IdentityService>();
builder.Services.AddScoped<IdentitySeeder>();
builder.Services.AddHostedService<DatabaseBootstrapHostedService<IdentityDbContext, IdentitySeeder>>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "identity" }));

var internalApi = app.MapGroup("/internal").RequireInternalApi(app.Configuration);

internalApi.MapPost("/auth/login", async (LoginRequest request, IdentityService service, CancellationToken cancellationToken) =>
{
    var response = await service.LoginAsync(request, cancellationToken);
    return response.Succeeded ? Results.Ok(response) : Results.BadRequest(response);
});

internalApi.MapGet("/auth/users", async (IdentityService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetSeedUsersAsync(cancellationToken)));

app.Run();

public partial class Program;
