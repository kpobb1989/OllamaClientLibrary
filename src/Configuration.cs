using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using OllamaClientLibrary.Abstractions.HttpClients;
using OllamaClientLibrary.Abstractions.Services;
using OllamaClientLibrary.Abstractions;
using OllamaClientLibrary.HttpClients;
using OllamaClientLibrary.Models;
using OllamaClientLibrary.Services;
using System.Collections.Generic;

namespace OllamaClientLibrary
{
    internal static class Configuration
    {
        public static void ConfigureServices(IServiceCollection services, OllamaOptions? options = null)
        {
            services.AddTransient<IOllamaHttpClient, OllamaHttpClient>();
            services.AddTransient<ICacheService, CacheService>();
            services.AddTransient<IOllamaWebParserService, OllamaWebParserService>();
            services.AddTransient<IOllamaClient, OllamaClient>();
            services.AddTransient<IDocumentService, DocumentService>();
            services.AddTransient<IOcrService, OcrService>();
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
        }
    }
}
