using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SystemDesignTutorials.Microservices.BuildingBlocks;
using SystemDesignTutorials.Microservices.Contracts;

namespace SystemDesignTutorials.Microservices.Gateway;

public sealed record GatewaySecurityOptions(string InternalApiKey);

internal static class GatewayHttp
{
    public static async Task<HttpResponseMessage> SendWithRetryAsync(
        HttpClient httpClient,
        Func<HttpRequestMessage> createRequest,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 6;

        for (var attempt = 1; attempt <= maxAttempts; attempt += 1)
        {
            using var request = createRequest();

            try
            {
                var response = await httpClient.SendAsync(request, cancellationToken);
                if ((int)response.StatusCode < 500 || attempt == maxAttempts)
                {
                    return response;
                }

                response.Dispose();
            }
            catch (HttpRequestException) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        throw new InvalidOperationException("Downstream request retry loop exhausted.");
    }
}

public interface IIdentityClient
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<SeedUserDto>> GetSeedUsersAsync(CancellationToken cancellationToken);
}

public interface ICatalogClient
{
    Task<IReadOnlyCollection<ProductDto>> GetProductsAsync(ForwardedUserContext user, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ProductDto>> GetProductsAsync(ForwardedUserContext user, IEnumerable<string> skus, CancellationToken cancellationToken);
    Task<ProductDto?> GetProductAsync(ForwardedUserContext user, string sku, CancellationToken cancellationToken);
}

public interface IInventoryClient
{
    Task<AvailabilityDto?> GetAvailabilityAsync(ForwardedUserContext user, string sku, CancellationToken cancellationToken);
}

public interface IOrdersClient
{
    Task<IReadOnlyCollection<OrderSummaryDto>> GetOrdersAsync(ForwardedUserContext user, CancellationToken cancellationToken);
    Task<OrderDetailDto?> GetOrderAsync(ForwardedUserContext user, Guid orderId, CancellationToken cancellationToken);
    Task<OrderDetailDto> CreateOrderAsync(ForwardedUserContext user, InternalCreateOrderRequest request, CancellationToken cancellationToken);
    Task<OrderDetailDto?> SubmitOrderAsync(ForwardedUserContext user, Guid orderId, CancellationToken cancellationToken);
}

public interface IFulfillmentClient
{
    Task<IReadOnlyCollection<ShipmentDto>> GetShipmentsAsync(ForwardedUserContext user, string? status, CancellationToken cancellationToken);
    Task<ShipmentDto?> ProgressShipmentAsync(ForwardedUserContext user, Guid shipmentId, string command, CancellationToken cancellationToken);
}

public interface IOperationsQueryClient
{
    Task<DashboardSummaryDto> GetDashboardAsync(ForwardedUserContext user, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<OrderSummaryDto>> GetOrdersAsync(ForwardedUserContext user, string? status, CancellationToken cancellationToken);
    Task<OrderDetailDto?> GetOrderAsync(ForwardedUserContext user, Guid orderId, CancellationToken cancellationToken);
}

public sealed class IdentityClient(HttpClient httpClient, GatewaySecurityOptions securityOptions) : IIdentityClient
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        using var response = await GatewayHttp.SendWithRetryAsync(httpClient, () =>
        {
            var message = new HttpRequestMessage(HttpMethod.Post, "/internal/auth/login")
            {
                Content = JsonContent.Create(request),
            };
            message.ApplyInternalOnly(securityOptions.InternalApiKey);
            return message;
        }, cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return (await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken))!;
        }

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken))!;
    }

    public async Task<IReadOnlyCollection<SeedUserDto>> GetSeedUsersAsync(CancellationToken cancellationToken)
    {
        using var response = await GatewayHttp.SendWithRetryAsync(httpClient, () =>
        {
            var message = new HttpRequestMessage(HttpMethod.Get, "/internal/auth/users");
            message.ApplyInternalOnly(securityOptions.InternalApiKey);
            return message;
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<SeedUserDto>>(cancellationToken)) ?? [];
    }
}

public sealed class CatalogClient(HttpClient httpClient, GatewaySecurityOptions securityOptions) : ICatalogClient
{
    public Task<IReadOnlyCollection<ProductDto>> GetProductsAsync(ForwardedUserContext user, CancellationToken cancellationToken)
        => GetProductsAsync(user, [], cancellationToken);

    public async Task<IReadOnlyCollection<ProductDto>> GetProductsAsync(ForwardedUserContext user, IEnumerable<string> skus, CancellationToken cancellationToken)
    {
        var queryString = string.Join(',', skus);
        var path = string.IsNullOrWhiteSpace(queryString) ? "/internal/products" : $"/internal/products?skus={Uri.EscapeDataString(queryString)}";
        using var response = await GatewayHttp.SendWithRetryAsync(httpClient, () =>
        {
            var message = new HttpRequestMessage(HttpMethod.Get, path);
            message.ApplyForwardedUser(user, securityOptions.InternalApiKey);
            return message;
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<ProductDto>>(cancellationToken)) ?? [];
    }

    public async Task<ProductDto?> GetProductAsync(ForwardedUserContext user, string sku, CancellationToken cancellationToken)
    {
        using var response = await GatewayHttp.SendWithRetryAsync(httpClient, () =>
        {
            var message = new HttpRequestMessage(HttpMethod.Get, $"/internal/products/{Uri.EscapeDataString(sku)}");
            message.ApplyForwardedUser(user, securityOptions.InternalApiKey);
            return message;
        }, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken);
    }
}

public sealed class InventoryClient(HttpClient httpClient, GatewaySecurityOptions securityOptions) : IInventoryClient
{
    public async Task<AvailabilityDto?> GetAvailabilityAsync(ForwardedUserContext user, string sku, CancellationToken cancellationToken)
    {
        using var response = await GatewayHttp.SendWithRetryAsync(httpClient, () =>
        {
            var message = new HttpRequestMessage(HttpMethod.Get, $"/internal/availability?sku={Uri.EscapeDataString(sku)}");
            message.ApplyForwardedUser(user, securityOptions.InternalApiKey);
            return message;
        }, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AvailabilityDto>(cancellationToken);
    }
}

public sealed class OrdersClient(HttpClient httpClient, GatewaySecurityOptions securityOptions) : IOrdersClient
{
    public async Task<IReadOnlyCollection<OrderSummaryDto>> GetOrdersAsync(ForwardedUserContext user, CancellationToken cancellationToken)
    {
        using var response = await GatewayHttp.SendWithRetryAsync(httpClient, () =>
        {
            var message = new HttpRequestMessage(HttpMethod.Get, "/internal/orders");
            message.ApplyForwardedUser(user, securityOptions.InternalApiKey);
            return message;
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<OrderSummaryDto>>(cancellationToken)) ?? [];
    }

    public async Task<OrderDetailDto?> GetOrderAsync(ForwardedUserContext user, Guid orderId, CancellationToken cancellationToken)
    {
        using var response = await GatewayHttp.SendWithRetryAsync(httpClient, () =>
        {
            var message = new HttpRequestMessage(HttpMethod.Get, $"/internal/orders/{orderId}");
            message.ApplyForwardedUser(user, securityOptions.InternalApiKey);
            return message;
        }, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDetailDto>(cancellationToken);
    }

    public async Task<OrderDetailDto> CreateOrderAsync(ForwardedUserContext user, InternalCreateOrderRequest request, CancellationToken cancellationToken)
    {
        using var response = await GatewayHttp.SendWithRetryAsync(httpClient, () =>
        {
            var message = new HttpRequestMessage(HttpMethod.Post, "/internal/orders")
            {
                Content = JsonContent.Create(request),
            };
            message.ApplyForwardedUser(user, securityOptions.InternalApiKey);
            return message;
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderDetailDto>(cancellationToken))!;
    }

    public async Task<OrderDetailDto?> SubmitOrderAsync(ForwardedUserContext user, Guid orderId, CancellationToken cancellationToken)
    {
        using var response = await GatewayHttp.SendWithRetryAsync(httpClient, () =>
        {
            var message = new HttpRequestMessage(HttpMethod.Post, $"/internal/orders/{orderId}/submit");
            message.ApplyForwardedUser(user, securityOptions.InternalApiKey);
            return message;
        }, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDetailDto>(cancellationToken);
    }
}

public sealed class FulfillmentClient(HttpClient httpClient, GatewaySecurityOptions securityOptions) : IFulfillmentClient
{
    public async Task<IReadOnlyCollection<ShipmentDto>> GetShipmentsAsync(ForwardedUserContext user, string? status, CancellationToken cancellationToken)
    {
        var path = string.IsNullOrWhiteSpace(status) ? "/internal/shipments" : $"/internal/shipments?status={Uri.EscapeDataString(status)}";
        using var response = await GatewayHttp.SendWithRetryAsync(httpClient, () =>
        {
            var message = new HttpRequestMessage(HttpMethod.Get, path);
            message.ApplyForwardedUser(user, securityOptions.InternalApiKey);
            return message;
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<ShipmentDto>>(cancellationToken)) ?? [];
    }

    public async Task<ShipmentDto?> ProgressShipmentAsync(ForwardedUserContext user, Guid shipmentId, string command, CancellationToken cancellationToken)
    {
        using var response = await GatewayHttp.SendWithRetryAsync(httpClient, () =>
        {
            var message = new HttpRequestMessage(HttpMethod.Post, $"/internal/shipments/{shipmentId}/{command}");
            message.ApplyForwardedUser(user, securityOptions.InternalApiKey);
            return message;
        }, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ShipmentDto>(cancellationToken);
    }
}

public sealed class OperationsQueryClient(HttpClient httpClient, GatewaySecurityOptions securityOptions) : IOperationsQueryClient
{
    public async Task<DashboardSummaryDto> GetDashboardAsync(ForwardedUserContext user, CancellationToken cancellationToken)
    {
        using var response = await GatewayHttp.SendWithRetryAsync(httpClient, () =>
        {
            var message = new HttpRequestMessage(HttpMethod.Get, "/internal/dashboard");
            message.ApplyForwardedUser(user, securityOptions.InternalApiKey);
            return message;
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(cancellationToken))!;
    }

    public async Task<IReadOnlyCollection<OrderSummaryDto>> GetOrdersAsync(ForwardedUserContext user, string? status, CancellationToken cancellationToken)
    {
        var path = string.IsNullOrWhiteSpace(status) ? "/internal/orders" : $"/internal/orders?status={Uri.EscapeDataString(status)}";
        using var response = await GatewayHttp.SendWithRetryAsync(httpClient, () =>
        {
            var message = new HttpRequestMessage(HttpMethod.Get, path);
            message.ApplyForwardedUser(user, securityOptions.InternalApiKey);
            return message;
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<OrderSummaryDto>>(cancellationToken)) ?? [];
    }

    public async Task<OrderDetailDto?> GetOrderAsync(ForwardedUserContext user, Guid orderId, CancellationToken cancellationToken)
    {
        using var response = await GatewayHttp.SendWithRetryAsync(httpClient, () =>
        {
            var message = new HttpRequestMessage(HttpMethod.Get, $"/internal/orders/{orderId}");
            message.ApplyForwardedUser(user, securityOptions.InternalApiKey);
            return message;
        }, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDetailDto>(cancellationToken);
    }
}
