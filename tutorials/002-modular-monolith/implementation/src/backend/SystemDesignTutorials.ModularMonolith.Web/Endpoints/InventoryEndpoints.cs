using System.Security.Claims;
using SystemDesignTutorials.ModularMonolith.Application.Modules;

namespace SystemDesignTutorials.ModularMonolith.Web.Endpoints;

internal static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var inventory = app.MapGroup("/inventory");
        inventory.MapGet("/warehouses", GetWarehousesAsync);
        inventory.MapGet("/stock", GetStockAsync);
    }

    private static async Task<IResult> GetWarehousesAsync(ClaimsPrincipal user, IInventoryModule inventoryModule, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanViewInventory(user) && !AccessControl.CanManageBilling(user))
        {
            return AccessControl.Forbidden("You do not have access to inventory views.");
        }

        return Results.Ok(await inventoryModule.GetWarehousesAsync(cancellationToken));
    }

    private static async Task<IResult> GetStockAsync(ClaimsPrincipal user, IInventoryModule inventoryModule, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanViewInventory(user) && !AccessControl.CanManageBilling(user) && !AccessControl.CanViewReports(user))
        {
            return AccessControl.Forbidden("You do not have access to stock views.");
        }

        return Results.Ok(await inventoryModule.GetStockAsync(cancellationToken));
    }
}

