using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SystemDesignTutorials.LayeredMonolith.Infrastructure.Identity;
using SystemDesignTutorials.LayeredMonolith.Infrastructure.Persistence;

namespace SystemDesignTutorials.LayeredMonolith.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=layered_monolith;Username=postgres;Password=postgres";

        services.AddDbContext<LayeredMonolithDbContext>(options =>
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
            .AddEntityFrameworkStores<LayeredMonolithDbContext>();

        services
            .AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies(options =>
            {
                var applicationCookie = options.ApplicationCookie ?? throw new InvalidOperationException("Application cookie configuration is unavailable.");
                applicationCookie.Configure(cookie =>
                {
                    cookie.Cookie.Name = "layeredmonolith.auth";
                    cookie.Events.OnRedirectToLogin = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/api"))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return Task.CompletedTask;
                        }

                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    };
                    cookie.Events.OnRedirectToAccessDenied = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/api"))
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            return Task.CompletedTask;
                        }

                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    };
                });
            });

        services.AddAuthorization();

        return services;
    }

    private static bool IsSqliteConnection(string connectionString)
    {
        return connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Filename=", StringComparison.OrdinalIgnoreCase)
            || connectionString.EndsWith(".db", StringComparison.OrdinalIgnoreCase)
            || connectionString.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase);
    }
}
