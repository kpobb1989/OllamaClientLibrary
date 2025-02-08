using System.Text.Json;

namespace OllamaClientLibrary.Cache
{
    internal static class CacheStorage
    {
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public static async Task<T?> GetAsync<T>(string key, TimeSpan cacheTime, CancellationToken ct = default)
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

                var value = await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: ct);
                return value;
            }

            return default;
        }

        public static async Task SaveAsync<T>(string key, T value, CancellationToken ct = default)
        {
            var filePath = GetFilePath(key);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);

            await JsonSerializer.SerializeAsync(stream, value, Options, ct);
        }

        private static string GetFilePath(string key)
        {
            var localPath = AppContext.BaseDirectory;

            var filePath = Path.Combine(localPath, $"{key}.json");

            return filePath;
        }
    }
}
