using SystemDesignTutorials.ModularMonolith.Application.DependencyInjection;
using SystemDesignTutorials.ModularMonolith.Infrastructure;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Seeding;
using SystemDesignTutorials.ModularMonolith.Web.Endpoints;

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
    service = "002-modular-monolith",
    timestamp = DateTimeOffset.UtcNow,
}));

app.MapAuthEndpoints();

var api = app.MapGroup("/api").RequireAuthorization();
api.MapCustomersEndpoints();
api.MapCatalogEndpoints();
api.MapOrdersEndpoints();
api.MapInventoryEndpoints();
api.MapBillingEndpoints();
api.MapReportingEndpoints();

app.Run();

public partial class Program;
