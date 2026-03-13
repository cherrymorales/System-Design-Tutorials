using FluentAssertions;
using MassTransit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.Microservices.Catalog;
using SystemDesignTutorials.Microservices.Contracts;
using SystemDesignTutorials.Microservices.Fulfillment;
using SystemDesignTutorials.Microservices.Identity;
using SystemDesignTutorials.Microservices.Inventory;
using SystemDesignTutorials.Microservices.Orders;
using SystemDesignTutorials.Microservices.Payments;

namespace SystemDesignTutorials.Microservices.Services.Tests;

public sealed class WorkflowServiceTests
{
    [Fact]
    public async Task Orders_create_draft_with_snapshots_and_total()
    {
        await using var database = new SqliteConnection("Data Source=:memory:");
        await database.OpenAsync();
        await using var dbContext = CreateOrdersContext(database);
        await dbContext.Database.EnsureCreatedAsync();

        var service = new OrdersService(dbContext);
        var order = await service.CreateDraftAsync(
            new InternalCreateOrderRequest(
                "CSR-20001",
                "AUD",
                [
                    new InternalCreateOrderLineRequest("SKU-HEADSET-001", "Noise Cancelling Headset", 2, 249m),
                    new InternalCreateOrderLineRequest("SKU-MOUSE-002", "Wireless Precision Mouse", 1, 89m),
                ],
                Guid.NewGuid(),
                "orders@microservices.local"),
            CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Draft);
        order.TotalAmount.Should().Be(587m);
        order.Lines.Should().HaveCount(2);
    }

    [Fact]
    public async Task Orders_submit_publishes_order_submitted_event()
    {
        await using var database = new SqliteConnection("Data Source=:memory:");
        await database.OpenAsync();
        await using var dbContext = CreateOrdersContext(database);
        await dbContext.Database.EnsureCreatedAsync();

        var service = new OrdersService(dbContext);
        var order = await service.CreateDraftAsync(
            new InternalCreateOrderRequest(
                "CSR-20002",
                "AUD",
                [new InternalCreateOrderLineRequest("SKU-HEADSET-001", "Noise Cancelling Headset", 1, 249m)],
                Guid.NewGuid(),
                "orders@microservices.local"),
            CancellationToken.None);

        var publisher = new TestPublishEndpoint();
        var submitted = await service.SubmitAsync(order.OrderId, publisher, CancellationToken.None);

        submitted.Should().NotBeNull();
        submitted!.Status.Should().Be(OrderStatus.AwaitingDependencies);
        publisher.Messages.OfType<OrderSubmittedIntegrationEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task Orders_transition_to_ready_when_inventory_and_payment_succeed()
    {
        await using var database = new SqliteConnection("Data Source=:memory:");
        await database.OpenAsync();
        await using var dbContext = CreateOrdersContext(database);
        await dbContext.Database.EnsureCreatedAsync();

        var service = new OrdersService(dbContext);
        var order = await service.CreateDraftAsync(
            new InternalCreateOrderRequest(
                "CSR-20003",
                "AUD",
                [new InternalCreateOrderLineRequest("SKU-HEADSET-001", "Noise Cancelling Headset", 1, 249m)],
                Guid.NewGuid(),
                "orders@microservices.local"),
            CancellationToken.None);

        await service.SubmitAsync(order.OrderId, new TestPublishEndpoint(), CancellationToken.None);
        var publisher = new TestPublishEndpoint();

        await service.HandleInventoryReservedAsync(
            new InventoryReservedIntegrationEvent(order.OrderId, Guid.NewGuid(), "MEL-DC", DateTimeOffset.UtcNow),
            publisher,
            CancellationToken.None);
        await service.HandlePaymentAuthorizedAsync(
            new PaymentAuthorizedIntegrationEvent(order.OrderId, Guid.NewGuid(), 249m, "AUTH-1", DateTimeOffset.UtcNow),
            publisher,
            CancellationToken.None);

        var storedOrder = await service.GetOrderAsync(order.OrderId, CancellationToken.None);
        storedOrder!.Status.Should().Be(OrderStatus.ReadyForFulfillment);
        publisher.Messages.OfType<OrderReadyForFulfillmentIntegrationEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task Inventory_rejects_forced_stock_failure()
    {
        await using var database = new SqliteConnection("Data Source=:memory:");
        await database.OpenAsync();
        await using var dbContext = CreateInventoryContext(database);
        await dbContext.Database.EnsureCreatedAsync();
        await new InventorySeeder().SeedAsync(dbContext, CancellationToken.None);

        var service = new InventoryService(dbContext);
        var publisher = new TestPublishEndpoint();

        await service.HandleOrderSubmittedAsync(
            new OrderSubmittedIntegrationEvent(
                Guid.NewGuid(),
                "ORD-FAIL-STOCK",
                "FAIL-STOCK-CASE",
                "AUD",
                249m,
                "orders@microservices.local",
                [new SubmittedOrderLine("SKU-HEADSET-001", "Noise Cancelling Headset", 1, 249m)],
                DateTimeOffset.UtcNow),
            publisher,
            CancellationToken.None);

        publisher.Messages.OfType<InventoryReservationRejectedIntegrationEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task Inventory_reserves_stock_and_publishes_success_event()
    {
        await using var database = new SqliteConnection("Data Source=:memory:");
        await database.OpenAsync();
        await using var dbContext = CreateInventoryContext(database);
        await dbContext.Database.EnsureCreatedAsync();
        await new InventorySeeder().SeedAsync(dbContext, CancellationToken.None);

        var service = new InventoryService(dbContext);
        var publisher = new TestPublishEndpoint();

        await service.HandleOrderSubmittedAsync(
            new OrderSubmittedIntegrationEvent(
                Guid.NewGuid(),
                "ORD-STOCK-OK",
                "CSR-STOCK-OK",
                "AUD",
                249m,
                "orders@microservices.local",
                [new SubmittedOrderLine("SKU-HEADSET-001", "Noise Cancelling Headset", 1, 249m)],
                DateTimeOffset.UtcNow),
            publisher,
            CancellationToken.None);

        publisher.Messages.OfType<InventoryReservedIntegrationEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task Payments_fail_when_reference_requests_failure()
    {
        await using var database = new SqliteConnection("Data Source=:memory:");
        await database.OpenAsync();
        await using var dbContext = CreatePaymentsContext(database);
        await dbContext.Database.EnsureCreatedAsync();

        var service = new PaymentsService(dbContext);
        var publisher = new TestPublishEndpoint();

        await service.HandleOrderSubmittedAsync(
            new OrderSubmittedIntegrationEvent(
                Guid.NewGuid(),
                "ORD-FAIL-PAY",
                "FAIL-PAYMENT-CASE",
                "AUD",
                249m,
                "orders@microservices.local",
                [new SubmittedOrderLine("SKU-HEADSET-001", "Noise Cancelling Headset", 1, 249m)],
                DateTimeOffset.UtcNow),
            publisher,
            CancellationToken.None);

        publisher.Messages.OfType<PaymentAuthorizationFailedIntegrationEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task Fulfillment_progression_enforces_valid_sequence()
    {
        await using var database = new SqliteConnection("Data Source=:memory:");
        await database.OpenAsync();
        await using var dbContext = CreateFulfillmentContext(database);
        await dbContext.Database.EnsureCreatedAsync();

        var service = new FulfillmentService(dbContext);
        var publisher = new TestPublishEndpoint();

        await service.HandleReadyForFulfillmentAsync(
            new OrderReadyForFulfillmentIntegrationEvent(Guid.NewGuid(), "ORD-20004", DateTimeOffset.UtcNow),
            publisher,
            CancellationToken.None);

        var shipment = (await service.GetShipmentsAsync(null, CancellationToken.None)).Single();
        var picked = await service.ProgressAsync(shipment.ShipmentId, ShipmentStatus.Picking, publisher, CancellationToken.None);

        picked!.Status.Should().Be(ShipmentStatus.Picking);

        var invalid = () => service.ProgressAsync(shipment.ShipmentId, ShipmentStatus.Delivered, publisher, CancellationToken.None);
        await invalid.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Identity_validates_seeded_user_credentials()
    {
        await using var database = new SqliteConnection("Data Source=:memory:");
        await database.OpenAsync();
        var options = new DbContextOptionsBuilder<IdentityDbContext>().UseSqlite(database).Options;
        await using var dbContext = new IdentityDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<UserAccount>();
        var seeder = new IdentitySeeder(passwordHasher);
        await seeder.SeedAsync(dbContext, CancellationToken.None);

        var service = new IdentityService(dbContext, passwordHasher);
        var response = await service.LoginAsync(new LoginRequest("orders@microservices.local", "Password123!"), CancellationToken.None);

        response.Succeeded.Should().BeTrue();
        response.User!.Role.Should().Be(ServiceRoles.OrderOpsAgent);
    }

    [Fact]
    public async Task Catalog_filters_products_by_requested_skus()
    {
        await using var database = new SqliteConnection("Data Source=:memory:");
        await database.OpenAsync();
        var options = new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(database).Options;
        await using var dbContext = new CatalogDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await new CatalogSeeder().SeedAsync(dbContext, CancellationToken.None);

        var service = new CatalogService(dbContext);
        var products = await service.GetProductsAsync(["SKU-HEADSET-001"], CancellationToken.None);

        products.Should().ContainSingle();
        products.Single().Sku.Should().Be("SKU-HEADSET-001");
    }

    private static OrdersDbContext CreateOrdersContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>().UseSqlite(connection).Options;
        return new OrdersDbContext(options);
    }

    private static InventoryDbContext CreateInventoryContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite(connection).Options;
        return new InventoryDbContext(options);
    }

    private static PaymentsDbContext CreatePaymentsContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>().UseSqlite(connection).Options;
        return new PaymentsDbContext(options);
    }

    private static FulfillmentDbContext CreateFulfillmentContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>().UseSqlite(connection).Options;
        return new FulfillmentDbContext(options);
    }
}

internal sealed class TestPublishEndpoint : IPublishEndpoint
{
    public List<object> Messages { get; } = [];

    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new NotSupportedException();

    public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish<T>(object values, CancellationToken cancellationToken = default) where T : class
        => throw new NotSupportedException();

    public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class
        => throw new NotSupportedException();

    public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class
        => throw new NotSupportedException();

    public Task Publish(object message, CancellationToken cancellationToken = default)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }
}
