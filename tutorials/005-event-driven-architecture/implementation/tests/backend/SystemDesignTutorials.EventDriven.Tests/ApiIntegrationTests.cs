using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace SystemDesignTutorials.EventDriven.Tests;

public sealed class ApiIntegrationTests : IClassFixture<EventDrivenApiFactory>
{
    private readonly EventDrivenApiFactory _factory;

    public ApiIntegrationTests(EventDrivenApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AssetsEndpointRequiresAuthentication()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/assets");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CoordinatorCanRegisterAssetAndMarkUploadComplete()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        await LoginAsync(client, "coordinator@eventdriven.local");

        var createResponse = await client.PostAsJsonAsync("/api/assets", new
        {
            assetKey = $"ASSET-{Guid.NewGuid().ToString("N")[..6]}",
            title = "Integration test asset",
            simulateFailure = false,
        });
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<AssetDetailResponse>();
        Assert.NotNull(created);
        Assert.Equal("Registered", created!.LifecycleState);

        var uploadResponse = await client.PostAsync($"/api/assets/{created.AssetId}/upload-complete", null);
        uploadResponse.EnsureSuccessStatusCode();

        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<AssetDetailResponse>();
        Assert.NotNull(uploaded);
        Assert.Equal("Uploaded", uploaded!.LifecycleState);

        var listResponse = await client.GetAsync("/api/assets");
        listResponse.EnsureSuccessStatusCode();
        var listed = await listResponse.Content.ReadAsStringAsync();
        Assert.Contains(created.AssetId.ToString(), listed);
    }

    private static async Task LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!",
        });
        response.EnsureSuccessStatusCode();
    }

    private sealed record AssetDetailResponse(Guid AssetId, string LifecycleState);
}

public sealed class EventDrivenApiFactory : WebApplicationFactory<Program>
{
    private readonly string _sqlitePath = Path.Combine(Path.GetTempPath(), $"event-driven-api-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_sqlitePath}",
                ["Messaging:Transport"] = "InMemory",
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(_sqlitePath))
        {
            try
            {
                File.Delete(_sqlitePath);
            }
            catch (IOException)
            {
                // The test host can release the SQLite file slightly after factory disposal; leaving it behind is acceptable for test isolation.
            }
        }
    }
}
