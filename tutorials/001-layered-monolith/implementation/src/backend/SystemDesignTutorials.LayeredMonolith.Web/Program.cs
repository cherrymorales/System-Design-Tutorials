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
app.MapHealthChecks("/health");

var api = app.MapGroup("/api");
api.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "001-layered-monolith",
    timestamp = DateTimeOffset.UtcNow,
}));

api.MapCatalogEndpoints();
api.MapInventoryEndpoints();
api.MapWorkflowEndpoints();

app.Run();
