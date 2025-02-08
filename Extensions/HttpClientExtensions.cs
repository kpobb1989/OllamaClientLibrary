using System.Net.Http.Json;
using System.Text.Json;

namespace Ollama.NET.Extensions
{
    internal static class HttpClientExtensions
    {
        public static async Task<Stream> ExecuteAndGetStreamAsync(this HttpClient httpClient, string requestUri, HttpMethod method, object? request = null, JsonSerializerOptions? options = null, CancellationToken ct = default)
        {
            HttpRequestMessage httpRequestMessage;

            if (method == HttpMethod.Post)
            {
                httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
                {
                    Content = JsonContent.Create(request, options: options)
                };
            }
            else
            {
                httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
            }

            var response = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync(ct);
        }
    }
}
