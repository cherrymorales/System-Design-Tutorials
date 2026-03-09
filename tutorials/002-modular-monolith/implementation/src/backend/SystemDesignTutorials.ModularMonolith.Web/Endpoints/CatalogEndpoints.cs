using System.Security.Claims;
using SystemDesignTutorials.ModularMonolith.Application.Modules;

namespace SystemDesignTutorials.ModularMonolith.Web.Endpoints;

internal static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var catalog = app.MapGroup("/catalog");
        catalog.MapGet("/products", GetProductsAsync);
        catalog.MapPost("/products", CreateProductAsync);
        catalog.MapPut("/products/{productId:guid}", UpdateProductAsync);
    }

    private static async Task<IResult> GetProductsAsync(ICatalogModule catalogModule, CancellationToken cancellationToken)
    {
        return Results.Ok(await catalogModule.GetProductsAsync(cancellationToken));
    }

    private static async Task<IResult> CreateProductAsync(ClaimsPrincipal user, CreateProductRequest request, ICatalogModule catalogModule, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageCatalog(user))
        {
            return AccessControl.Forbidden("Only operations managers can create catalog products in V1.");
        }

        var product = await catalogModule.CreateProductAsync(new CreateProductCommand(request.Sku, request.Name, request.Category, request.UnitPrice), cancellationToken);
        return Results.Ok(product);
    }

    private static async Task<IResult> UpdateProductAsync(ClaimsPrincipal user, Guid productId, UpdateProductRequest request, ICatalogModule catalogModule, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanManageCatalog(user))
        {
            return AccessControl.Forbidden("Only operations managers can update catalog products in V1.");
        }

        var product = await catalogModule.UpdateProductAsync(productId, new UpdateProductCommand(request.Name, request.Category, request.UnitPrice, request.IsActive), cancellationToken);
        return Results.Ok(product);
    }
}

