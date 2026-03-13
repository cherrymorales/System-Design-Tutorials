using MassTransit;
using SystemDesignTutorials.Microservices.BuildingBlocks;
using SystemDesignTutorials.Microservices.Contracts;
using SystemDesignTutorials.Microservices.Fulfillment;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConfiguredDbContext<FulfillmentDbContext>(builder.Configuration, "Fulfillment");
builder.Services.AddScoped<FulfillmentSeeder>();
builder.Services.AddScoped<FulfillmentService>();
builder.Services.AddHostedService<DatabaseBootstrapHostedService<FulfillmentDbContext, FulfillmentSeeder>>();

builder.Services.AddMassTransit(registration =>
{
    registration.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("fulfillment", false));
    registration.AddConsumer<OrderReadyForFulfillmentConsumer>();
    registration.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMq"));
        cfg.ConfigureEndpoints(context);
        cfg.UseInMemoryOutbox(context);
    });
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "fulfillment" }));

var internalApi = app.MapGroup("/internal").RequireInternalApi(app.Configuration);

internalApi.MapGet("/shipments", async (string? status, FulfillmentService service, CancellationToken cancellationToken) =>
{
    ShipmentStatus? filter = Enum.TryParse<ShipmentStatus>(status, true, out var parsedStatus) ? parsedStatus : null;
    return Results.Ok(await service.GetShipmentsAsync(filter, cancellationToken));
});

internalApi.MapGet("/shipments/{shipmentId:guid}", async (Guid shipmentId, FulfillmentService service, CancellationToken cancellationToken) =>
{
    var shipment = await service.GetShipmentAsync(shipmentId, cancellationToken);
    return shipment is null ? Results.NotFound() : Results.Ok(shipment);
});

internalApi.MapPost("/shipments/{shipmentId:guid}/pick", async (HttpContext httpContext, Guid shipmentId, FulfillmentService service, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken) =>
{
    var user = httpContext.GetForwardedUser();
    if (user is null || !user.IsInRole(ServiceRoles.FulfillmentOperator, ServiceRoles.OperationsManager))
    {
        return Results.Forbid();
    }

    var shipment = await service.ProgressAsync(shipmentId, ShipmentStatus.Picking, publishEndpoint, cancellationToken);
    return shipment is null ? Results.NotFound() : Results.Ok(shipment);
});

internalApi.MapPost("/shipments/{shipmentId:guid}/pack", async (HttpContext httpContext, Guid shipmentId, FulfillmentService service, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken) =>
{
    var user = httpContext.GetForwardedUser();
    if (user is null || !user.IsInRole(ServiceRoles.FulfillmentOperator, ServiceRoles.OperationsManager))
    {
        return Results.Forbid();
    }

    var shipment = await service.ProgressAsync(shipmentId, ShipmentStatus.Packed, publishEndpoint, cancellationToken);
    return shipment is null ? Results.NotFound() : Results.Ok(shipment);
});

internalApi.MapPost("/shipments/{shipmentId:guid}/ship", async (HttpContext httpContext, Guid shipmentId, FulfillmentService service, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken) =>
{
    var user = httpContext.GetForwardedUser();
    if (user is null || !user.IsInRole(ServiceRoles.FulfillmentOperator, ServiceRoles.OperationsManager))
    {
        return Results.Forbid();
    }

    var shipment = await service.ProgressAsync(shipmentId, ShipmentStatus.Shipped, publishEndpoint, cancellationToken);
    return shipment is null ? Results.NotFound() : Results.Ok(shipment);
});

internalApi.MapPost("/shipments/{shipmentId:guid}/deliver", async (HttpContext httpContext, Guid shipmentId, FulfillmentService service, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken) =>
{
    var user = httpContext.GetForwardedUser();
    if (user is null || !user.IsInRole(ServiceRoles.FulfillmentOperator, ServiceRoles.OperationsManager))
    {
        return Results.Forbid();
    }

    var shipment = await service.ProgressAsync(shipmentId, ShipmentStatus.Delivered, publishEndpoint, cancellationToken);
    return shipment is null ? Results.NotFound() : Results.Ok(shipment);
});

app.Run();

public partial class Program;
