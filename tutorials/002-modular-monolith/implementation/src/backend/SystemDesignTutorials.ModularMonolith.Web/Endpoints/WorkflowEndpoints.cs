using System.Security.Claims;
using SystemDesignTutorials.ModularMonolith.Application.Modules;

namespace SystemDesignTutorials.ModularMonolith.Web.Endpoints;

internal static class OrdersEndpoints
{
    public static void MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/orders");
        orders.MapGet("/", GetOrdersAsync);
        orders.MapPost("/", CreateOrderAsync);
        orders.MapPost("/{orderId:guid}/submit", SubmitOrderAsync);
        orders.MapPost("/{orderId:guid}/reserve", ReserveOrderAsync);
        orders.MapPost("/{orderId:guid}/ready-for-invoicing", ReadyForInvoicingAsync);
        orders.MapPost("/{orderId:guid}/complete", CompleteOrderAsync);
        orders.MapPost("/{orderId:guid}/cancel", CancelOrderAsync);
    }

    private static async Task<IResult> GetOrdersAsync(ClaimsPrincipal user, IOrdersModule ordersModule, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageOrders(user) && !AccessControl.CanReadyOrders(user) && !AccessControl.CanManageBilling(user) && !AccessControl.CanViewReports(user))
        {
            return AccessControl.Forbidden("You do not have access to orders.");
        }

        return Results.Ok(await ordersModule.GetOrdersAsync(cancellationToken));
    }

    private static async Task<IResult> CreateOrderAsync(ClaimsPrincipal user, CreateOrderRequest request, IOrdersModule ordersModule, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageOrders(user))
        {
            return AccessControl.Forbidden("Only sales or manager roles can create orders.");
        }

        var order = await ordersModule.CreateOrderAsync(new CreateOrderCommand(request.CustomerId, request.Lines.Select(line => new CreateOrderLineCommand(line.ProductId, line.Quantity)).ToArray()), AccessControl.GetRequiredEmail(user), cancellationToken);
        return Results.Ok(order);
    }

    private static async Task<IResult> SubmitOrderAsync(ClaimsPrincipal user, Guid orderId, IOrdersModule ordersModule, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageOrders(user))
        {
            return AccessControl.Forbidden("Only sales or manager roles can submit orders.");
        }

        return Results.Ok(await ordersModule.SubmitOrderAsync(orderId, cancellationToken));
    }

    private static async Task<IResult> ReserveOrderAsync(ClaimsPrincipal user, Guid orderId, IOrdersModule ordersModule, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageOrders(user))
        {
            return AccessControl.Forbidden("Only sales or manager roles can reserve orders.");
        }

        return Results.Ok(await ordersModule.ReserveOrderAsync(orderId, AccessControl.GetRequiredEmail(user), cancellationToken));
    }

    private static async Task<IResult> ReadyForInvoicingAsync(ClaimsPrincipal user, Guid orderId, IOrdersModule ordersModule, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanReadyOrders(user))
        {
            return AccessControl.Forbidden("Only warehouse or manager roles can mark an order ready for invoicing.");
        }

        return Results.Ok(await ordersModule.MarkReadyForInvoicingAsync(orderId, AccessControl.GetRequiredEmail(user), cancellationToken));
    }

    private static async Task<IResult> CompleteOrderAsync(ClaimsPrincipal user, Guid orderId, IOrdersModule ordersModule, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageBilling(user) && !AccessControl.CanViewReports(user))
        {
            return AccessControl.Forbidden("Only finance or manager roles can complete an order.");
        }

        return Results.Ok(await ordersModule.CompleteOrderAsync(orderId, AccessControl.GetRequiredEmail(user), cancellationToken));
    }

    private static async Task<IResult> CancelOrderAsync(ClaimsPrincipal user, Guid orderId, IOrdersModule ordersModule, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageOrders(user) && !AccessControl.CanViewReports(user))
        {
            return AccessControl.Forbidden("Only sales or manager roles can cancel an order.");
        }

        return Results.Ok(await ordersModule.CancelOrderAsync(orderId, AccessControl.GetRequiredEmail(user), cancellationToken));
    }
}
