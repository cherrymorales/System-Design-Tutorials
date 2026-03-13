using MassTransit;
using SystemDesignTutorials.Microservices.BuildingBlocks;
using SystemDesignTutorials.Microservices.Contracts;
using SystemDesignTutorials.Microservices.Orders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConfiguredDbContext<OrdersDbContext>(builder.Configuration, "Orders");
builder.Services.AddScoped<OrdersSeeder>();
builder.Services.AddScoped<OrdersService>();
builder.Services.AddHostedService<DatabaseBootstrapHostedService<OrdersDbContext, OrdersSeeder>>();

builder.Services.AddMassTransit(registration =>
{
    registration.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("orders", false));
    registration.AddConsumer<InventoryReservedConsumer>();
    registration.AddConsumer<InventoryRejectedConsumer>();
    registration.AddConsumer<PaymentAuthorizedConsumer>();
    registration.AddConsumer<PaymentFailedConsumer>();
    registration.AddConsumer<ShipmentCreatedConsumer>();
    registration.AddConsumer<ShipmentDeliveredConsumer>();
    registration.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMq"));
        cfg.ConfigureEndpoints(context);
        cfg.UseInMemoryOutbox(context);
    });
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "orders" }));

var internalApi = app.MapGroup("/internal").RequireInternalApi(app.Configuration);

internalApi.MapGet("/orders", async (OrdersService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetOrdersAsync(cancellationToken)));

internalApi.MapGet("/orders/{orderId:guid}", async (Guid orderId, OrdersService service, CancellationToken cancellationToken) =>
{
    var order = await service.GetOrderAsync(orderId, cancellationToken);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

internalApi.MapPost("/orders", async (HttpContext httpContext, InternalCreateOrderRequest request, OrdersService service, CancellationToken cancellationToken) =>
{
    var user = httpContext.GetForwardedUser();
    if (user is null)
    {
        return Results.BadRequest(new { error = "Forwarded user context is required." });
    }

    var order = await service.CreateDraftAsync(request with
    {
        CreatedByUserId = user.UserId,
        CreatedByEmail = user.Email,
    }, cancellationToken);

    return Results.Created($"/internal/orders/{order.OrderId}", order);
});

internalApi.MapPost("/orders/{orderId:guid}/submit", async (HttpContext httpContext, Guid orderId, OrdersService service, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken) =>
{
    var user = httpContext.GetForwardedUser();
    if (user is null || !user.IsInRole(ServiceRoles.OrderOpsAgent, ServiceRoles.OperationsManager))
    {
        return Results.Forbid();
    }

    var order = await service.SubmitAsync(orderId, publishEndpoint, cancellationToken);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

internalApi.MapPost("/orders/{orderId:guid}/cancel", async (HttpContext httpContext, Guid orderId, OrdersService service, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken) =>
{
    var user = httpContext.GetForwardedUser();
    if (user is null || !user.IsInRole(ServiceRoles.OrderOpsAgent, ServiceRoles.OperationsManager))
    {
        return Results.Forbid();
    }

    var order = await service.CancelAsync(orderId, publishEndpoint, cancellationToken);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

app.Run();

public partial class Program;
