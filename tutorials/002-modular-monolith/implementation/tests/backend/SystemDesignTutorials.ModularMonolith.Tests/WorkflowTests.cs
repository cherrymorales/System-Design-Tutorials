using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SystemDesignTutorials.ModularMonolith.Application.DependencyInjection;
using SystemDesignTutorials.ModularMonolith.Application.Modules;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Identity;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Modules.Billing;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Modules.Catalog;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Modules.Customers;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Modules.Inventory;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Modules.Orders;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Modules.Reporting;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Persistence;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Seeding;

namespace SystemDesignTutorials.ModularMonolith.Tests;

public class WorkflowTests
{
    [Fact]
    public async Task Full_order_to_cash_flow_updates_order_invoice_and_inventory()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddDbContext<ModularMonolithDbContext>(options => options.UseSqlite(connection));
        services
            .AddIdentityCore<AppIdentityUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddSignInManager()
            .AddEntityFrameworkStores<ModularMonolithDbContext>();
        services.AddAuthorization();
        services.AddScoped<ICustomersModule, CustomersModule>();
        services.AddScoped<ICatalogModule, CatalogModule>();
        services.AddScoped<IInventoryModule, InventoryModule>();
        services.AddScoped<IOrdersModule, OrdersModule>();
        services.AddScoped<IBillingModule, BillingModule>();
        services.AddScoped<IReportingModule, ReportingModule>();

        await using var provider = services.BuildServiceProvider();
        await ApplicationDataSeeder.SeedAsync(provider);

        using var scope = provider.CreateScope();
        var customers = scope.ServiceProvider.GetRequiredService<ICustomersModule>();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogModule>();
        var orders = scope.ServiceProvider.GetRequiredService<IOrdersModule>();
        var billing = scope.ServiceProvider.GetRequiredService<IBillingModule>();
        var inventory = scope.ServiceProvider.GetRequiredService<IInventoryModule>();

        var customer = (await customers.GetCustomersAsync(CancellationToken.None)).First();
        var product = (await catalog.GetProductsAsync(CancellationToken.None)).First();
        var initialStock = (await inventory.GetStockAsync(CancellationToken.None)).Single(x => x.ProductId == product.Id).QuantityOnHand;

        var order = await orders.CreateOrderAsync(new CreateOrderCommand(customer.Id, [new CreateOrderLineCommand(product.Id, 2)]), "sales@modularmonolith.local", CancellationToken.None);
        order = await orders.SubmitOrderAsync(order.Id, CancellationToken.None);
        order = await orders.ReserveOrderAsync(order.Id, "sales@modularmonolith.local", CancellationToken.None);
        order = await orders.MarkReadyForInvoicingAsync(order.Id, "warehouse@modularmonolith.local", CancellationToken.None);

        var billingSnapshot = await orders.GetOrderForInvoiceAsync(order.Id, CancellationToken.None);
        var invoice = await billing.CreateDraftAsync(new CreateInvoiceDraftCommand(billingSnapshot.OrderId, billingSnapshot.CustomerId, billingSnapshot.CustomerName, billingSnapshot.TotalAmount), "finance@modularmonolith.local", CancellationToken.None);
        await orders.LinkInvoiceAsync(order.Id, invoice.Id, CancellationToken.None);
        invoice = await billing.IssueInvoiceAsync(invoice.Id, "finance@modularmonolith.local", CancellationToken.None);
        await orders.MarkOrderInvoicedAsync(order.Id, CancellationToken.None);
        await billing.MarkPaidAsync(invoice.Id, "finance@modularmonolith.local", CancellationToken.None);
        order = await orders.CompleteOrderAsync(order.Id, "finance@modularmonolith.local", CancellationToken.None);

        order.Status.Should().Be("Completed");
        var finalStock = (await inventory.GetStockAsync(CancellationToken.None)).Single(x => x.ProductId == product.Id).QuantityOnHand;
        finalStock.Should().Be(initialStock - 2);
    }
}
