using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SystemDesignTutorials.ModularMonolith.Application.Modules;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Identity;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Modules.Billing;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Modules.Catalog;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Modules.Customers;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Modules.Inventory;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Modules.Orders;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Modules.Reporting;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Persistence;

namespace SystemDesignTutorials.ModularMonolith.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=modular_monolith;Username=postgres;Password=postgres";

        services.AddDbContext<ModularMonolithDbContext>(options =>
        {
            if (IsSqliteConnection(connectionString))
            {
                options.UseSqlite(connectionString);
                return;
            }

            options.UseNpgsql(connectionString);
        });

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

        services
            .AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies(options =>
            {
                var applicationCookie = options.ApplicationCookie ?? throw new InvalidOperationException("Application cookie configuration is unavailable.");
                applicationCookie.Configure(cookie =>
                {
                    cookie.Cookie.Name = "modularmonolith.auth";
                    cookie.Events.OnRedirectToLogin = context => HandleApiRedirect(context, StatusCodes.Status401Unauthorized);
                    cookie.Events.OnRedirectToAccessDenied = context => HandleApiRedirect(context, StatusCodes.Status403Forbidden);
                });
            });

        services.AddAuthorization();

        services.AddScoped<ICustomersModule, CustomersModule>();
        services.AddScoped<ICatalogModule, CatalogModule>();
        services.AddScoped<IInventoryModule, InventoryModule>();
        services.AddScoped<IOrdersModule, OrdersModule>();
        services.AddScoped<IBillingModule, BillingModule>();
        services.AddScoped<IReportingModule, ReportingModule>();

        return services;
    }

    private static Task HandleApiRedirect(RedirectContext<CookieAuthenticationOptions> context, int statusCode)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }

    private static bool IsSqliteConnection(string connectionString)
    {
        return connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Filename=", StringComparison.OrdinalIgnoreCase)
            || connectionString.EndsWith(".db", StringComparison.OrdinalIgnoreCase)
            || connectionString.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase);
    }
}
