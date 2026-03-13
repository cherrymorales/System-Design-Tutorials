using MassTransit;
using SystemDesignTutorials.Microservices.BuildingBlocks;
using SystemDesignTutorials.Microservices.Contracts;
using SystemDesignTutorials.Microservices.OperationsQuery;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConfiguredDbContext<OperationsQueryDbContext>(builder.Configuration, "OperationsQuery");
builder.Services.AddScoped<OperationsQuerySeeder>();
builder.Services.AddScoped<OperationsQueryService>();
builder.Services.AddHostedService<DatabaseBootstrapHostedService<OperationsQueryDbContext, OperationsQuerySeeder>>();

builder.Services.AddMassTransit(registration =>
{
    registration.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("operations-query", false));
    registration.AddConsumer<OrderSubmittedProjectionConsumer>();
    registration.AddConsumer<InventoryReservedProjectionConsumer>();
    registration.AddConsumer<InventoryRejectedProjectionConsumer>();
    registration.AddConsumer<PaymentAuthorizedProjectionConsumer>();
    registration.AddConsumer<PaymentFailedProjectionConsumer>();
    registration.AddConsumer<OrderReadyProjectionConsumer>();
    registration.AddConsumer<OrderFailedProjectionConsumer>();
    registration.AddConsumer<ShipmentCreatedProjectionConsumer>();
    registration.AddConsumer<ShipmentStatusChangedProjectionConsumer>();
    registration.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMq"));
        cfg.ConfigureEndpoints(context);
        cfg.UseInMemoryOutbox(context);
    });
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "operations-query" }));

var internalApi = app.MapGroup("/internal").RequireInternalApi(app.Configuration);

internalApi.MapGet("/dashboard", async (OperationsQueryService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetDashboardAsync(cancellationToken)));

internalApi.MapGet("/orders", async (string? status, OperationsQueryService service, CancellationToken cancellationToken) =>
{
    OrderStatus? filter = Enum.TryParse<OrderStatus>(status, true, out var parsedStatus) ? parsedStatus : null;
    return Results.Ok(await service.GetOrdersAsync(filter, cancellationToken));
});

internalApi.MapGet("/orders/{orderId:guid}", async (Guid orderId, OperationsQueryService service, CancellationToken cancellationToken) =>
{
    var order = await service.GetOrderAsync(orderId, cancellationToken);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

app.Run();

public partial class Program;
