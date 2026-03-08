using SystemDesignTutorials.LayeredMonolith.Application.DependencyInjection;
using SystemDesignTutorials.LayeredMonolith.Infrastructure;
using SystemDesignTutorials.LayeredMonolith.Infrastructure.Seeding;
using SystemDesignTutorials.LayeredMonolith.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await ApplicationDataSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "001-layered-monolith",
    timestamp = DateTimeOffset.UtcNow,
}));

app.MapAuthEndpoints();

var api = app.MapGroup("/api").RequireAuthorization();
api.MapCatalogEndpoints();
api.MapInventoryEndpoints();
api.MapWorkflowEndpoints();

app.Run();
