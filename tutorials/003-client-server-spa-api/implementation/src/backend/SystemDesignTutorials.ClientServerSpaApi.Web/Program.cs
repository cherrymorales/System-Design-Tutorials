using SystemDesignTutorials.ClientServerSpaApi.Application.DependencyInjection;
using SystemDesignTutorials.ClientServerSpaApi.Infrastructure;
using SystemDesignTutorials.ClientServerSpaApi.Infrastructure.Seeding;
using SystemDesignTutorials.ClientServerSpaApi.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await ApplicationDataSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "003-client-server-spa-api",
    timestamp = DateTimeOffset.UtcNow,
}));

app.MapAuthEndpoints();

var api = app.MapGroup("/api").RequireAuthorization();
api.MapDashboardEndpoints();
api.MapProjectsEndpoints();
api.MapTasksEndpoints();
api.MapUsersEndpoints();

app.Run();

public partial class Program;
