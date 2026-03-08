using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.LayeredMonolith.Domain.Entities;
using SystemDesignTutorials.LayeredMonolith.Infrastructure.Identity;
using SystemDesignTutorials.LayeredMonolith.Infrastructure.Persistence;

namespace SystemDesignTutorials.LayeredMonolith.Infrastructure.Seeding;

public static class ApplicationDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var dbContext = serviceProvider.GetRequiredService<LayeredMonolithDbContext>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppIdentityUser>>();

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        foreach (var role in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        await SeedUserAsync(userManager, ApplicationRoles.OperationsManager, "manager@layeredmonolith.local", "Operations Manager");
        await SeedUserAsync(userManager, ApplicationRoles.InventoryPlanner, "planner@layeredmonolith.local", "Inventory Planner");
        await SeedUserAsync(userManager, ApplicationRoles.PurchasingOfficer, "purchasing@layeredmonolith.local", "Purchasing Officer");
        await SeedUserAsync(userManager, ApplicationRoles.WarehouseOperator, "operator.brisbane@layeredmonolith.local", "Brisbane Operator");

        if (!await dbContext.Warehouses.AnyAsync(cancellationToken))
        {
            var brisbane = new Warehouse("BNE", "Brisbane Warehouse", "Brisbane");
            var sydney = new Warehouse("SYD", "Sydney Warehouse", "Sydney");
            var melbourne = new Warehouse("MEL", "Melbourne Warehouse", "Melbourne");

            await dbContext.Warehouses.AddRangeAsync([brisbane, sydney, melbourne], cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.Products.AnyAsync(cancellationToken))
        {
            var products = new[]
            {
                new Product("LAP-14-BLK", "14 Inch Laptop", "Computers", "SUP-TECH", 1299.00m),
                new Product("MON-27-4K", "27 Inch 4K Monitor", "Displays", "SUP-DISPLAY", 499.00m),
                new Product("CHR-ERG-BLK", "Ergonomic Office Chair", "Furniture", "SUP-OFFICE", 349.00m),
            };

            await dbContext.Products.AddRangeAsync(products, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.InventoryItems.AnyAsync(cancellationToken))
        {
            var warehouses = await dbContext.Warehouses.OrderBy(x => x.Code).ToListAsync(cancellationToken);
            var products = await dbContext.Products.OrderBy(x => x.Sku).ToListAsync(cancellationToken);

            var inventoryItems = new List<InventoryItem>();

            foreach (var warehouse in warehouses)
            {
                foreach (var product in products)
                {
                    var item = new InventoryItem(product.Id, warehouse.Id, reorderThreshold: 10);
                    item.Receive(warehouse.Code switch
                    {
                        "BNE" => 30,
                        "SYD" => 18,
                        _ => 12,
                    });
                    inventoryItems.Add(item);
                }
            }

            await dbContext.InventoryItems.AddRangeAsync(inventoryItems, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SeedUserAsync(
        UserManager<AppIdentityUser> userManager,
        string role,
        string email,
        string displayName)
    {
        var existing = await userManager.Users.SingleOrDefaultAsync(user => user.Email == email);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, role))
            {
                await userManager.AddToRoleAsync(existing, role);
            }

            return;
        }

        var user = new AppIdentityUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, "Password123!");
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to seed user '{email}': {errors}");
        }

        await userManager.AddToRoleAsync(user, role);
    }
}

