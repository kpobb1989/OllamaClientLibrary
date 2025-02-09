using Newtonsoft.Json;

using System;
using System.IO;
using System.Threading.Tasks;
using OllamaClientLibrary.Extensions;

namespace OllamaClientLibrary.Cache
{
    internal static class CacheStorage
    {
        public static async Task<T?> GetAsync<T>(string key, TimeSpan cacheTime) where T: class
        {
            var filePath = GetFilePath(key);

            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);

                if ((DateTime.UtcNow - fileInfo.LastWriteTimeUtc).Ticks > cacheTime.Ticks)
                {
                    return default;
                }

                await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

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

        private static string GetFilePath(string key)
        {
            var localPath = AppContext.BaseDirectory;

            var filePath = Path.Combine(localPath, $"{key}.json");

            return filePath;
        }
    }
}
