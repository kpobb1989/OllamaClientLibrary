using Microsoft.Extensions.DependencyInjection;

using OllamaClientLibrary.Models;

namespace OllamaClientLibrary.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOllamaClient(this IServiceCollection services, OllamaOptions? options = null)
        {
            Configuration.ConfigureServices(services, options);

            return services;
        }
    }
}
