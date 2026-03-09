using System.Security.Claims;
using SystemDesignTutorials.ModularMonolith.Application.Modules;

namespace SystemDesignTutorials.ModularMonolith.Web.Endpoints;

internal static class CustomersEndpoints
{
    public static void MapCustomersEndpoints(this IEndpointRouteBuilder app)
    {
        var customers = app.MapGroup("/customers");
        customers.MapGet("/", GetCustomersAsync);
        customers.MapPost("/", CreateCustomerAsync);
        customers.MapPut("/{customerId:guid}", UpdateCustomerAsync);
    }

    private static async Task<IResult> GetCustomersAsync(ClaimsPrincipal user, ICustomersModule customersModule, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageCustomers(user) && !AccessControl.CanViewReports(user))
        {
            return AccessControl.Forbidden("You do not have access to customer records.");
        }

        return Results.Ok(await customersModule.GetCustomersAsync(cancellationToken));
    }

    private static async Task<IResult> CreateCustomerAsync(ClaimsPrincipal user, CreateCustomerRequest request, ICustomersModule customersModule, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageCustomers(user))
        {
            return AccessControl.Forbidden("Only sales or manager roles can create customers.");
        }

        var customer = await customersModule.CreateCustomerAsync(new CreateCustomerCommand(request.AccountCode, request.Name, request.BillingContactName, request.BillingContactEmail, request.ShippingContactName, request.ShippingContactEmail), cancellationToken);
        return Results.Ok(customer);
    }

    private static async Task<IResult> UpdateCustomerAsync(ClaimsPrincipal user, Guid customerId, UpdateCustomerRequest request, ICustomersModule customersModule, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageCustomers(user))
        {
            return AccessControl.Forbidden("Only sales or manager roles can update customers.");
        }

        var customer = await customersModule.UpdateCustomerAsync(customerId, new UpdateCustomerCommand(request.Name, request.BillingContactName, request.BillingContactEmail, request.ShippingContactName, request.ShippingContactEmail, request.IsActive), cancellationToken);
        return Results.Ok(customer);
    }
}

