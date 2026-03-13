using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.Json.Serialization;
using SystemDesignTutorials.Microservices.BuildingBlocks;
using SystemDesignTutorials.Microservices.Contracts;
using SystemDesignTutorials.Microservices.Gateway;

var builder = WebApplication.CreateBuilder(args);

var apiKey = builder.Configuration["InternalApi:ApiKey"]
    ?? throw new InvalidOperationException("InternalApi:ApiKey must be configured for the gateway.");

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "system-design-tutorials.microservices";
        options.LoginPath = "/login";
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddHttpClient<IIdentityClient, IdentityClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Services:Identity"]!));
builder.Services.AddHttpClient<ICatalogClient, CatalogClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Services:Catalog"]!));
builder.Services.AddHttpClient<IInventoryClient, InventoryClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Services:Inventory"]!));
builder.Services.AddHttpClient<IOrdersClient, OrdersClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Services:Orders"]!));
builder.Services.AddHttpClient<IFulfillmentClient, FulfillmentClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Services:Fulfillment"]!));
builder.Services.AddHttpClient<IOperationsQueryClient, OperationsQueryClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Services:OperationsQuery"]!));
builder.Services.AddSingleton(new GatewaySecurityOptions(apiKey));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "gateway-bff" }));

app.MapGet("/api/auth/users", async (IIdentityClient identityClient, CancellationToken cancellationToken) =>
    Results.Ok(await identityClient.GetSeedUsersAsync(cancellationToken)));

app.MapPost("/api/auth/login", async (LoginRequest request, HttpContext httpContext, IIdentityClient identityClient, CancellationToken cancellationToken) =>
{
    var loginResponse = await identityClient.LoginAsync(request, cancellationToken);
    if (!loginResponse.Succeeded || loginResponse.User is null)
    {
        return Results.BadRequest(loginResponse);
    }

    var user = loginResponse.User;
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.Name, user.DisplayName),
        new(ClaimTypes.Role, user.Role),
        new("display_name", user.DisplayName),
    };

    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

    return Results.Ok(user);
});

app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.NoContent();
});

app.MapGet("/api/auth/me", (HttpContext httpContext) =>
{
    if (httpContext.User.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(ForwardedUserContext.FromPrincipal(httpContext.User));
});

var api = app.MapGroup("/api");
api.RequireAuthorization();

api.MapGet("/catalog/products", async (HttpContext httpContext, ICatalogClient catalogClient, CancellationToken cancellationToken) =>
    Results.Ok(await catalogClient.GetProductsAsync(httpContext.User.ToForwardedUserContext(), cancellationToken)));

api.MapGet("/catalog/products/{sku}", async (string sku, HttpContext httpContext, ICatalogClient catalogClient, CancellationToken cancellationToken) =>
{
    var product = await catalogClient.GetProductAsync(httpContext.User.ToForwardedUserContext(), sku, cancellationToken);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

api.MapGet("/catalog/availability", async (string sku, HttpContext httpContext, IInventoryClient inventoryClient, CancellationToken cancellationToken) =>
{
    var availability = await inventoryClient.GetAvailabilityAsync(httpContext.User.ToForwardedUserContext(), sku, cancellationToken);
    return availability is null ? Results.NotFound() : Results.Ok(availability);
});

api.MapGet("/orders", async (HttpContext httpContext, IOrdersClient ordersClient, CancellationToken cancellationToken) =>
    Results.Ok(await ordersClient.GetOrdersAsync(httpContext.User.ToForwardedUserContext(), cancellationToken)));

api.MapGet("/orders/{orderId:guid}", async (Guid orderId, HttpContext httpContext, IOperationsQueryClient operationsClient, IOrdersClient ordersClient, CancellationToken cancellationToken) =>
{
    var user = httpContext.User.ToForwardedUserContext();
    var projectedOrder = await operationsClient.GetOrderAsync(user, orderId, cancellationToken);
    if (projectedOrder is not null)
    {
        return Results.Ok(projectedOrder);
    }

    var liveOrder = await ordersClient.GetOrderAsync(user, orderId, cancellationToken);
    return liveOrder is null ? Results.NotFound() : Results.Ok(liveOrder);
});

api.MapPost("/orders", async (CreateOrderRequest request, HttpContext httpContext, ICatalogClient catalogClient, IOrdersClient ordersClient, CancellationToken cancellationToken) =>
{
    var user = httpContext.User.ToForwardedUserContext();
    if (!user.IsInRole(ServiceRoles.OrderOpsAgent, ServiceRoles.OperationsManager))
    {
        return Results.Forbid();
    }

    var catalog = await catalogClient.GetProductsAsync(user, request.Lines.Select(line => line.Sku), cancellationToken);
    var catalogLookup = catalog.Where(product => product.IsSellable).ToDictionary(product => product.Sku, StringComparer.OrdinalIgnoreCase);
    if (request.Lines.Any(line => !catalogLookup.ContainsKey(line.Sku)))
    {
        return Results.BadRequest(new { error = "One or more selected products are unavailable for order entry." });
    }

    var internalRequest = new InternalCreateOrderRequest(
        request.CustomerReference,
        request.Currency,
        request.Lines.Select(line =>
        {
            var product = catalogLookup[line.Sku];
            return new InternalCreateOrderLineRequest(line.Sku, product.Name, line.Quantity, product.UnitPrice);
        }).ToArray(),
        user.UserId,
        user.Email);

    var order = await ordersClient.CreateOrderAsync(user, internalRequest, cancellationToken);
    return Results.Created($"/api/orders/{order.OrderId}", order);
});

api.MapPost("/orders/{orderId:guid}/submit", async (Guid orderId, HttpContext httpContext, IOrdersClient ordersClient, CancellationToken cancellationToken) =>
{
    var user = httpContext.User.ToForwardedUserContext();
    if (!user.IsInRole(ServiceRoles.OrderOpsAgent, ServiceRoles.OperationsManager))
    {
        return Results.Forbid();
    }

    var order = await ordersClient.SubmitOrderAsync(user, orderId, cancellationToken);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

api.MapGet("/operations/dashboard", async (HttpContext httpContext, IOperationsQueryClient operationsClient, CancellationToken cancellationToken) =>
{
    var user = httpContext.User.ToForwardedUserContext();
    if (!user.IsInRole(ServiceRoles.InventoryCoordinator, ServiceRoles.FinanceReviewer, ServiceRoles.FulfillmentOperator, ServiceRoles.OperationsManager, ServiceRoles.OrderOpsAgent))
    {
        return Results.Forbid();
    }

    return Results.Ok(await operationsClient.GetDashboardAsync(user, cancellationToken));
});

api.MapGet("/operations/orders", async (string? status, HttpContext httpContext, IOperationsQueryClient operationsClient, CancellationToken cancellationToken) =>
{
    var user = httpContext.User.ToForwardedUserContext();
    return Results.Ok(await operationsClient.GetOrdersAsync(user, status, cancellationToken));
});

api.MapGet("/fulfillment/shipments", async (string? status, HttpContext httpContext, IFulfillmentClient fulfillmentClient, CancellationToken cancellationToken) =>
{
    var user = httpContext.User.ToForwardedUserContext();
    return Results.Ok(await fulfillmentClient.GetShipmentsAsync(user, status, cancellationToken));
});

api.MapPost("/fulfillment/shipments/{shipmentId:guid}/{command}", async (Guid shipmentId, string command, HttpContext httpContext, IFulfillmentClient fulfillmentClient, CancellationToken cancellationToken) =>
{
    var user = httpContext.User.ToForwardedUserContext();
    if (!user.IsInRole(ServiceRoles.FulfillmentOperator, ServiceRoles.OperationsManager))
    {
        return Results.Forbid();
    }

    var shipment = await fulfillmentClient.ProgressShipmentAsync(user, shipmentId, command, cancellationToken);
    return shipment is null ? Results.NotFound() : Results.Ok(shipment);
});

app.Run();

public partial class Program;

file static class ClaimsPrincipalExtensions
{
    public static ForwardedUserContext ToForwardedUserContext(this ClaimsPrincipal principal)
        => ForwardedUserContext.FromPrincipal(principal);
}
