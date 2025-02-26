using Microsoft.Extensions.DependencyInjection;

using OllamaClientLibrary.Abstractions.HttpClients;
using OllamaClientLibrary.Abstractions.Services;
using OllamaClientLibrary.Abstractions;
using OllamaClientLibrary.HttpClients;
using OllamaClientLibrary.Services;
using OllamaClientLibrary.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Collections.Generic;

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
            services.AddSingleton(options ?? new OllamaOptions());
            services.AddSingleton(JsonSerializer.Create(new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new List<JsonConverter>()
                {
                    new StringEnumConverter(new CamelCaseNamingStrategy())
                }
            }));

            return services;
        }
    }
}
