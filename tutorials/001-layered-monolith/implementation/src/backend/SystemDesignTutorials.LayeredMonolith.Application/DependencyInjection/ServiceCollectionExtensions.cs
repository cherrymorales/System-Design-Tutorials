using Microsoft.Extensions.DependencyInjection;

namespace SystemDesignTutorials.LayeredMonolith.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
