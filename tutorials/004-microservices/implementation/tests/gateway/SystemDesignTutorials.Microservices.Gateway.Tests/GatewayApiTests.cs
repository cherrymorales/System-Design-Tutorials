using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SystemDesignTutorials.Microservices.BuildingBlocks;
using SystemDesignTutorials.Microservices.Contracts;
using SystemDesignTutorials.Microservices.Gateway;

namespace SystemDesignTutorials.Microservices.Gateway.Tests;

public sealed class GatewayApiTests
{
    [Fact]
    public async Task Login_then_me_restores_gateway_session()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("orders@microservices.local", "Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var meResponse = await client.GetAsync("/api/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var currentUser = await meResponse.Content.ReadFromJsonAsync<CurrentUserDto>();
        currentUser!.Role.Should().Be(ServiceRoles.OrderOpsAgent);
    }

    [Fact]
    public async Task Protected_order_route_rejects_unauthenticated_requests()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Orders_route_serializes_status_enums_as_strings()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("orders@microservices.local", "Password123!"));
        var response = await client.GetAsync("/api/orders");
        var payload = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().Contain("\"status\":\"Draft\"");
        payload.Should().Contain("\"reservationStatus\":\"Pending\"");
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IIdentityClient>();
                    services.RemoveAll<ICatalogClient>();
                    services.RemoveAll<IInventoryClient>();
                    services.RemoveAll<IOrdersClient>();
                    services.RemoveAll<IFulfillmentClient>();
                    services.RemoveAll<IOperationsQueryClient>();

                    services.AddSingleton<IIdentityClient>(new FakeIdentityClient());
                    services.AddSingleton<ICatalogClient>(new FakeCatalogClient());
                    services.AddSingleton<IInventoryClient>(new FakeInventoryClient());
                    services.AddSingleton<IOrdersClient>(new FakeOrdersClient());
                    services.AddSingleton<IFulfillmentClient>(new FakeFulfillmentClient());
                    services.AddSingleton<IOperationsQueryClient>(new FakeOperationsQueryClient());
                });
            });
    }
}

internal sealed class FakeIdentityClient : IIdentityClient
{
    public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new LoginResponse(true, new CurrentUserDto(Guid.NewGuid(), request.Email, "Order Operations Agent", ServiceRoles.OrderOpsAgent), null));

    public Task<IReadOnlyCollection<SeedUserDto>> GetSeedUsersAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<SeedUserDto>>(
            [new SeedUserDto("orders@microservices.local", "Order Operations Agent", ServiceRoles.OrderOpsAgent)]);
}

internal sealed class FakeCatalogClient : ICatalogClient
{
    public Task<ProductDto?> GetProductAsync(ForwardedUserContext user, string sku, CancellationToken cancellationToken)
        => Task.FromResult<ProductDto?>(new ProductDto(sku, "Noise Cancelling Headset", "Peripherals", 249m, true, "Active"));

    public Task<IReadOnlyCollection<ProductDto>> GetProductsAsync(ForwardedUserContext user, CancellationToken cancellationToken)
        => GetProductsAsync(user, [], cancellationToken);

    public Task<IReadOnlyCollection<ProductDto>> GetProductsAsync(ForwardedUserContext user, IEnumerable<string> skus, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<ProductDto>>(
            [new ProductDto("SKU-HEADSET-001", "Noise Cancelling Headset", "Peripherals", 249m, true, "Active")]);
}

internal sealed class FakeInventoryClient : IInventoryClient
{
    public Task<AvailabilityDto?> GetAvailabilityAsync(ForwardedUserContext user, string sku, CancellationToken cancellationToken)
        => Task.FromResult<AvailabilityDto?>(new AvailabilityDto(sku, 10, 0, [new AvailabilityByWarehouseDto("MEL-DC", 10, 0)]));
}

internal sealed class FakeOrdersClient : IOrdersClient
{
    public Task<OrderDetailDto> CreateOrderAsync(ForwardedUserContext user, InternalCreateOrderRequest request, CancellationToken cancellationToken)
        => Task.FromResult(CreateOrderDetail());

    public Task<OrderDetailDto?> GetOrderAsync(ForwardedUserContext user, Guid orderId, CancellationToken cancellationToken)
        => Task.FromResult<OrderDetailDto?>(CreateOrderDetail());

    public Task<IReadOnlyCollection<OrderSummaryDto>> GetOrdersAsync(ForwardedUserContext user, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<OrderSummaryDto>>(
            [new OrderSummaryDto(Guid.NewGuid(), "ORD-10001", "CSR-10001", OrderStatus.Draft, ReservationStatus.Pending, PaymentStatus.Pending, null, 249m, DateTimeOffset.UtcNow)]);

    public Task<OrderDetailDto?> SubmitOrderAsync(ForwardedUserContext user, Guid orderId, CancellationToken cancellationToken)
        => Task.FromResult<OrderDetailDto?>(CreateOrderDetail());

    private static OrderDetailDto CreateOrderDetail()
        => new(
            Guid.NewGuid(),
            "ORD-10001",
            "CSR-10001",
            "AUD",
            OrderStatus.Draft,
            ReservationStatus.Pending,
            PaymentStatus.Pending,
            null,
            249m,
            "orders@microservices.local",
            null,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            [new OrderLineDto("SKU-HEADSET-001", "Noise Cancelling Headset", 1, 249m, 249m)]);
}

internal sealed class FakeFulfillmentClient : IFulfillmentClient
{
    public Task<IReadOnlyCollection<ShipmentDto>> GetShipmentsAsync(ForwardedUserContext user, string? status, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<ShipmentDto>>([]);

    public Task<ShipmentDto?> ProgressShipmentAsync(ForwardedUserContext user, Guid shipmentId, string command, CancellationToken cancellationToken)
        => Task.FromResult<ShipmentDto?>(null);
}

internal sealed class FakeOperationsQueryClient : IOperationsQueryClient
{
    public Task<DashboardSummaryDto> GetDashboardAsync(ForwardedUserContext user, CancellationToken cancellationToken)
        => Task.FromResult(new DashboardSummaryDto(1, 1, 0, 0, 0, 0, []));

    public Task<OrderDetailDto?> GetOrderAsync(ForwardedUserContext user, Guid orderId, CancellationToken cancellationToken)
        => Task.FromResult<OrderDetailDto?>(null);

    public Task<IReadOnlyCollection<OrderSummaryDto>> GetOrdersAsync(ForwardedUserContext user, string? status, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<OrderSummaryDto>>([]);
}
