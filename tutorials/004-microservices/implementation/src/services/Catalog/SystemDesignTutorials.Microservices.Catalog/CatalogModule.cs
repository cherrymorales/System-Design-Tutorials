using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.Microservices.BuildingBlocks;
using SystemDesignTutorials.Microservices.Contracts;

namespace SystemDesignTutorials.Microservices.Catalog;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);
            entity.HasIndex(product => product.Sku).IsUnique();
            entity.Property(product => product.Sku).HasMaxLength(80);
            entity.Property(product => product.Name).HasMaxLength(200);
            entity.Property(product => product.Category).HasMaxLength(120);
            entity.Property(product => product.OperationalStatus).HasMaxLength(80);
        });
    }
}

public sealed class Product
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public bool IsSellable { get; set; }
    public string OperationalStatus { get; set; } = string.Empty;
}

public sealed class CatalogSeeder : IDatabaseSeeder<CatalogDbContext>
{
    public async Task SeedAsync(CatalogDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.Products.AddRange(
            new Product
            {
                Id = Guid.NewGuid(),
                Sku = "SKU-HEADSET-001",
                Name = "Noise Cancelling Headset",
                Category = "Peripherals",
                UnitPrice = 249.00m,
                IsSellable = true,
                OperationalStatus = "Active",
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Sku = "SKU-MOUSE-002",
                Name = "Wireless Precision Mouse",
                Category = "Peripherals",
                UnitPrice = 89.00m,
                IsSellable = true,
                OperationalStatus = "Active",
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Sku = "SKU-DOCK-003",
                Name = "USB-C Docking Station",
                Category = "Accessories",
                UnitPrice = 319.00m,
                IsSellable = true,
                OperationalStatus = "Active",
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Sku = "SKU-ARCHIVE-004",
                Name = "Archived Keyboard Bundle",
                Category = "Legacy",
                UnitPrice = 59.00m,
                IsSellable = false,
                OperationalStatus = "Archived",
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class CatalogService(CatalogDbContext dbContext)
{
    public async Task<IReadOnlyCollection<ProductDto>> GetProductsAsync(IEnumerable<string> skus, CancellationToken cancellationToken)
    {
        var query = dbContext.Products.AsNoTracking().AsQueryable();
        var skuList = skus
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (skuList.Count > 0)
        {
            query = query.Where(product => skuList.Contains(product.Sku));
        }

        return await query
            .OrderBy(product => product.Name)
            .Select(product => new ProductDto(
                product.Sku,
                product.Name,
                product.Category,
                product.UnitPrice,
                product.IsSellable,
                product.OperationalStatus))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDto?> GetProductAsync(string sku, CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(product => product.Sku == sku)
            .Select(product => new ProductDto(
                product.Sku,
                product.Name,
                product.Category,
                product.UnitPrice,
                product.IsSellable,
                product.OperationalStatus))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
