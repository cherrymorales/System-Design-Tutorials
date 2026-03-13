using SystemDesignTutorials.Microservices.BuildingBlocks;
using SystemDesignTutorials.Microservices.Catalog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConfiguredDbContext<CatalogDbContext>(builder.Configuration, "Catalog");
builder.Services.AddScoped<CatalogSeeder>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddHostedService<DatabaseBootstrapHostedService<CatalogDbContext, CatalogSeeder>>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "catalog" }));

var internalApi = app.MapGroup("/internal").RequireInternalApi(app.Configuration);

internalApi.MapGet("/products", async (string? skus, CatalogService service, CancellationToken cancellationToken) =>
{
    var skuFilter = string.IsNullOrWhiteSpace(skus)
        ? Array.Empty<string>()
        : skus.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    return Results.Ok(await service.GetProductsAsync(skuFilter, cancellationToken));
});

internalApi.MapGet("/products/{sku}", async (string sku, CatalogService service, CancellationToken cancellationToken) =>
{
    var product = await service.GetProductAsync(sku, cancellationToken);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

app.Run();

public partial class Program;
