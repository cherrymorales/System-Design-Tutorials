using MassTransit;
using SystemDesignTutorials.Microservices.BuildingBlocks;
using SystemDesignTutorials.Microservices.Inventory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConfiguredDbContext<InventoryDbContext>(builder.Configuration, "Inventory");
builder.Services.AddScoped<InventorySeeder>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddHostedService<DatabaseBootstrapHostedService<InventoryDbContext, InventorySeeder>>();

builder.Services.AddMassTransit(registration =>
{
    registration.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("inventory", false));
    registration.AddConsumer<OrderSubmittedConsumer>();
    registration.AddConsumer<ReservationReleaseRequestedConsumer>();
    registration.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMq"));
        cfg.ConfigureEndpoints(context);
        cfg.UseInMemoryOutbox(context);
    });
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "inventory" }));

var internalApi = app.MapGroup("/internal").RequireInternalApi(app.Configuration);

internalApi.MapGet("/availability", async (string sku, InventoryService service, CancellationToken cancellationToken) =>
{
    var availability = await service.GetAvailabilityAsync(sku, cancellationToken);
    return availability is null ? Results.NotFound() : Results.Ok(availability);
});

app.Run();

public partial class Program;
