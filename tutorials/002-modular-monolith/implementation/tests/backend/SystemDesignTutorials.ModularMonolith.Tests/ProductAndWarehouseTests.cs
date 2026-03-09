using Microsoft.AspNetCore.Hosting;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SystemDesignTutorials.ModularMonolith.Tests;

public class ProductAndWarehouseTests
{
    [Fact]
    public async Task Unauthenticated_requests_are_rejected()
    {
        await using var factory = new ModularMonolithApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

        var response = await client.GetAsync("/api/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Manager_can_sign_in_and_access_reports()
    {
        await using var factory = new ModularMonolithApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email = "manager@modularmonolith.local", password = "Password123!" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reportsResponse = await client.GetAsync("/api/reports/summary");
        reportsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

internal sealed class ModularMonolithApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"modular-monolith-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_databasePath}",
            });
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
