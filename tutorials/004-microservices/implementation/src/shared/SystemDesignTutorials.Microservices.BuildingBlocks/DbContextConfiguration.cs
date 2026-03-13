using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SystemDesignTutorials.Microservices.BuildingBlocks;

public static class DbContextConfigurationExtensions
{
    public static IServiceCollection AddConfiguredDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
        where TContext : DbContext
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' was not configured.");

        services.AddDbContext<TContext>(options =>
        {
            if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        return services;
    }
}
