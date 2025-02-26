
using System;

namespace OllamaClientLibrary.Abstractions.Services
{
    public interface ICacheService
    {
        T? Get<T>(string key, TimeSpan? cacheTime = null) where T : class;

        void Set<T>(string key, T value);

        void Clear();
    }
}
