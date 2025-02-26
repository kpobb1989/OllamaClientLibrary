using Microsoft.Extensions.DependencyInjection;

using OllamaClientLibrary.Abstractions.HttpClients;
using OllamaClientLibrary.Abstractions.Services;
using OllamaClientLibrary.Abstractions;
using OllamaClientLibrary.HttpClients;
using OllamaClientLibrary.Services;
using OllamaClientLibrary.Models;

namespace OllamaClientLibrary.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOllamaClient(this IServiceCollection services, OllamaOptions? options = null)
        {
            services.AddTransient<IOllamaHttpClient, OllamaHttpClient>();
            services.AddTransient<ICacheService, CacheService>();
            services.AddTransient<IOllamaWebParserService, OllamaWebParserService>();
            services.AddTransient<IOllamaClient, OllamaClient>();
            services.AddSingleton(options  ?? new OllamaOptions());

            return services;
        }
    }
}
