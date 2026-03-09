using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.ModularMonolith.Application.Modules;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Catalog;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Persistence;

namespace SystemDesignTutorials.ModularMonolith.Infrastructure.Modules.Catalog;

public sealed class CatalogModule(ModularMonolithDbContext dbContext) : ICatalogModule
{
    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .OrderBy(x => x.Name)
            .Select(x => new ProductDto(x.Id, x.Sku, x.Name, x.Category, x.UnitPrice, x.Status.ToString(), x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDto?> GetProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .Where(x => x.Id == productId)
            .Select(x => new ProductDto(x.Id, x.Sku, x.Name, x.Category, x.UnitPrice, x.Status.ToString(), x.CreatedAt, x.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        if (await dbContext.Products.AnyAsync(x => x.Sku == command.Sku, cancellationToken))
        {
            throw new InvalidOperationException("Product SKU already exists.");
        }

        var product = new Product(command.Sku, command.Name, command.Category, command.UnitPrice);
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetProductAsync(product.Id, cancellationToken) ?? throw new InvalidOperationException("Product was not persisted.");
    }

    public async Task<ProductDto> UpdateProductAsync(Guid productId, UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(x => x.Id == productId, cancellationToken)
            ?? throw new KeyNotFoundException("Product not found.");

        product.Update(command.Name, command.Category, command.UnitPrice, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetProductAsync(productId, cancellationToken) ?? throw new InvalidOperationException("Product update failed.");
    }

    public async Task<IReadOnlyDictionary<Guid, ProductValidationDto>> GetProductValidationMapAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken)
    {
        var distinctIds = productIds.Distinct().ToList();

        return await dbContext.Products
            .Where(x => distinctIds.Contains(x.Id))
            .ToDictionaryAsync(
                x => x.Id,
                x => new ProductValidationDto(x.Id, x.IsActive, x.Sku, x.Name, x.UnitPrice),
                cancellationToken);
    }
}
