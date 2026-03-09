using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Catalog;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Customers;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Inventory;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Identity;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Persistence;

namespace SystemDesignTutorials.ModularMonolith.Infrastructure.Seeding;

public static class ApplicationDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ModularMonolithDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppIdentityUser>>();

        await dbContext.Database.EnsureCreatedAsync();

        foreach (var role in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        await EnsureUserAsync(userManager, "sales@modularmonolith.local", "Sales Coordinator", ApplicationRoles.SalesCoordinator);
        await EnsureUserAsync(userManager, "warehouse@modularmonolith.local", "Warehouse Operator", ApplicationRoles.WarehouseOperator);
        await EnsureUserAsync(userManager, "finance@modularmonolith.local", "Finance Officer", ApplicationRoles.FinanceOfficer);
        await EnsureUserAsync(userManager, "manager@modularmonolith.local", "Operations Manager", ApplicationRoles.OperationsManager);

        if (!await dbContext.Customers.AnyAsync())
        {
            dbContext.Customers.AddRange(
                new Customer("CUST-1001", "Acme Office Group", "Mia Lawson", "mia.lawson@acme.example", "Tom Briggs", "tom.briggs@acme.example"),
                new Customer("CUST-1002", "Northern Clinics", "Rita Chen", "rita.chen@northern.example", "Liam Doyle", "liam.doyle@northern.example"),
                new Customer("CUST-1003", "Citywide Education", "Sarah Ibrahim", "sarah.ibrahim@citywide.example", "Chris Tan", "chris.tan@citywide.example"));
        }

        if (!await dbContext.Products.AnyAsync())
        {
            dbContext.Products.AddRange(
                new Product("MON-27-4K", "27in 4K Monitor", "Monitors", 449.00m),
                new Product("DOC-USB-C", "USB-C Docking Station", "Accessories", 229.00m),
                new Product("LAP-14-PRO", "14in Business Laptop", "Laptops", 1799.00m),
                new Product("KEY-WLS", "Wireless Keyboard", "Accessories", 89.00m));
        }

        if (!await dbContext.Warehouses.AnyAsync())
        {
            dbContext.Warehouses.Add(new Warehouse("BNE-CENTRAL", "Brisbane Central", "Brisbane"));
        }

        await dbContext.SaveChangesAsync();

        if (!await dbContext.StockItems.AnyAsync())
        {
            var warehouse = await dbContext.Warehouses.SingleAsync();
            var products = await dbContext.Products.OrderBy(x => x.Sku).ToListAsync();
            dbContext.StockItems.AddRange(
                new StockItem(products.Single(x => x.Sku == "MON-27-4K").Id, warehouse.Id, 35, 8),
                new StockItem(products.Single(x => x.Sku == "DOC-USB-C").Id, warehouse.Id, 40, 10),
                new StockItem(products.Single(x => x.Sku == "LAP-14-PRO").Id, warehouse.Id, 18, 5),
                new StockItem(products.Single(x => x.Sku == "KEY-WLS").Id, warehouse.Id, 60, 12));
            await dbContext.SaveChangesAsync();
        }
    }

    private static async Task EnsureUserAsync(UserManager<AppIdentityUser> userManager, string email, string displayName, string role)
    {
        var user = await userManager.Users.SingleOrDefaultAsync(x => x.Email == email);
        if (user is null)
        {
            user = new AppIdentityUser
            {
                UserName = email,
                Email = email,
                DisplayName = displayName,
                EmailConfirmed = true,
            };

            var createResult = await userManager.CreateAsync(user, "Password123!");
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create seeded user {email}: {string.Join(", ", createResult.Errors.Select(error => error.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }
}
