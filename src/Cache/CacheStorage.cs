using Newtonsoft.Json;

using OllamaClientLibrary.Extensions;

using System;
using System.IO;

namespace OllamaClientLibrary.Cache
{
    public static class CacheStorage
    {
        public static T? Get<T>(string key, TimeSpan? cacheTime = null) where T : class
        {
            cacheTime ??= TimeSpan.FromHours(1);

            var filePath = GetFilePath(key);

            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);

                if ((DateTime.UtcNow - fileInfo.LastWriteTimeUtc).Ticks > cacheTime?.Ticks)
                {
                    return default;
                }

                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                var jsonSerializer = JsonSerializer.Create();

                var value = jsonSerializer.Deserialize<T>(stream);
                return value;
            }

            return default;
        }

        public static void Save<T>(string key, T value)
        {
            var filePath = GetFilePath(key);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(stream);

            var jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented
            });

            jsonSerializer.Serialize(writer, value);
        }

        public static void Clear()
        {
            var cachePath = Path.Combine(AppContext.BaseDirectory, "cache");

            if (!Directory.Exists(cachePath))
            {
                return;
            }

            var files = Directory.GetFiles(cachePath, "*.json");

            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        private static string GetFilePath(string key)
        {
            var cachePath = Path.Combine(AppContext.BaseDirectory, "cache");

            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }

            var filePath = Path.Combine(cachePath, $"{key}.json");

            return filePath;
        }
    }
}
