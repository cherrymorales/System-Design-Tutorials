using Microsoft.Extensions.DependencyInjection;

namespace SystemDesignTutorials.ModularMonolith.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
