using System.Security.Claims;
using SystemDesignTutorials.ModularMonolith.Application.Modules;

namespace SystemDesignTutorials.ModularMonolith.Web.Endpoints;

internal static class BillingEndpoints
{
    public static void MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var billing = app.MapGroup("/billing");
        billing.MapGet("/invoices", GetInvoicesAsync);
        billing.MapPost("/invoices", CreateDraftInvoiceAsync);
        billing.MapPost("/invoices/{invoiceId:guid}/issue", IssueInvoiceAsync);
        billing.MapPost("/invoices/{invoiceId:guid}/mark-paid", MarkPaidAsync);
    }

    private static async Task<IResult> GetInvoicesAsync(ClaimsPrincipal user, IBillingModule billingModule, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageBilling(user) && !AccessControl.CanViewReports(user))
        {
            return AccessControl.Forbidden("You do not have access to billing records.");
        }

        return Results.Ok(await billingModule.GetInvoicesAsync(cancellationToken));
    }

    private static async Task<IResult> CreateDraftInvoiceAsync(
        ClaimsPrincipal user,
        CreateInvoiceDraftRequest request,
        IOrdersModule ordersModule,
        IBillingModule billingModule,
        CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageBilling(user))
        {
            return AccessControl.Forbidden("Only finance or manager roles can create invoices.");
        }

        var order = await ordersModule.GetOrderForInvoiceAsync(request.OrderId, cancellationToken);
        var invoice = await billingModule.CreateDraftAsync(new CreateInvoiceDraftCommand(order.OrderId, order.CustomerId, order.CustomerName, order.TotalAmount), AccessControl.GetRequiredEmail(user), cancellationToken);
        await ordersModule.LinkInvoiceAsync(order.OrderId, invoice.Id, cancellationToken);
        return Results.Ok(invoice);
    }

    private static async Task<IResult> IssueInvoiceAsync(
        ClaimsPrincipal user,
        Guid invoiceId,
        IBillingModule billingModule,
        IOrdersModule ordersModule,
        CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageBilling(user))
        {
            return AccessControl.Forbidden("Only finance or manager roles can issue invoices.");
        }

        var invoice = await billingModule.IssueInvoiceAsync(invoiceId, AccessControl.GetRequiredEmail(user), cancellationToken);
        await ordersModule.MarkOrderInvoicedAsync(invoice.OrderId, cancellationToken);
        return Results.Ok(invoice);
    }

    private static async Task<IResult> MarkPaidAsync(ClaimsPrincipal user, Guid invoiceId, IBillingModule billingModule, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageBilling(user))
        {
            return AccessControl.Forbidden("Only finance or manager roles can mark invoices paid.");
        }

        return Results.Ok(await billingModule.MarkPaidAsync(invoiceId, AccessControl.GetRequiredEmail(user), cancellationToken));
    }
}

