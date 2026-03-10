using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SystemDesignTutorials.LayeredMonolith.Tests;

public sealed class ApiIntegrationTests
{
    [Fact]
    public async Task Unauthenticated_products_request_returns_401()
    {
        await using var factory = new LayeredMonolithApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

        var response = await client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Manager_can_sign_in_and_view_all_warehouses()
    {
        await using var factory = new LayeredMonolithApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "manager@layeredmonolith.local",
            password = "Password123!",
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var warehouses = await client.GetFromJsonAsync<List<WarehouseResponseDto>>("/api/warehouses");

        warehouses.Should().NotBeNull();
        var warehouseList = warehouses!;
        warehouseList.Should().HaveCount(3);
        warehouseList.Select(item => item.Code).Should().Contain(["BNE", "MEL", "SYD"]);
    }

    [Fact]
    public async Task Brisbane_operator_only_sees_assigned_warehouse_and_inventory()
    {
        await using var factory = new LayeredMonolithApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "operator.brisbane@layeredmonolith.local",
            password = "Password123!",
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var warehouses = await client.GetFromJsonAsync<List<WarehouseResponseDto>>("/api/warehouses");
        var inventory = await client.GetFromJsonAsync<List<InventorySummaryResponseDto>>("/api/inventory/summary");

        warehouses.Should().NotBeNull();
        var warehouseList = warehouses!;
        warehouseList.Should().ContainSingle();
        warehouseList[0].Code.Should().Be("BNE");

        inventory.Should().NotBeNull();
        var inventoryItems = inventory!;
        inventoryItems.Should().NotBeEmpty();
        inventoryItems.Select(item => item.WarehouseCode).Distinct().Should().Equal(["BNE"]);
    }

    [Fact]
    public async Task Brisbane_operator_cannot_create_transfers()
    {
        await using var factory = new LayeredMonolithApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "operator.brisbane@layeredmonolith.local",
            password = "Password123!",
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.PostAsJsonAsync("/api/transfers", new
        {
            sourceWarehouseId = Guid.NewGuid(),
            destinationWarehouseId = Guid.NewGuid(),
            productId = Guid.NewGuid(),
            quantity = 1,
            reason = "Not allowed",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

internal sealed class LayeredMonolithApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"layered-monolith-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var sqliteConnectionString = $"Data Source={_databasePath}";

        builder.UseSetting("ConnectionStrings:DefaultConnection", sqliteConnectionString);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = sqliteConnectionString,
            });
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (File.Exists(_databasePath))
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch (IOException)
            {
                // Ignore transient Windows file-lock timing during test cleanup.
            }
        }
    }
}

internal sealed record WarehouseResponseDto(Guid Id, string Code, string Name, string City, string Status, int TotalSkuCount, int LowStockSkuCount);
internal sealed record InventorySummaryResponseDto(Guid Id, Guid ProductId, string ProductSku, string ProductName, Guid WarehouseId, string WarehouseCode, string WarehouseName, int QuantityOnHand, int QuantityReserved, int AvailableQuantity, int ReorderThreshold, bool IsLowStock, DateTimeOffset UpdatedAt);
