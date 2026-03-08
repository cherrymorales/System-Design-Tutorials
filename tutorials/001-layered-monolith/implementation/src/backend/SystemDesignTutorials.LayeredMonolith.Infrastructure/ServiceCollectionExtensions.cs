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

        services.AddDbContext<LayeredMonolithDbContext>(options => options.UseNpgsql(connectionString));

        services
            .AddIdentityCore<AppIdentityUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<LayeredMonolithDbContext>();

        return services;
    }
}
