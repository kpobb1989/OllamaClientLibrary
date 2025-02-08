using Newtonsoft.Json;

using System.Text;

namespace OllamaClientLibrary.Extensions
{
    internal static class HttpClientExtensions
    {
        public static async Task<Stream> ExecuteAndGetStreamAsync(this HttpClient httpClient, string requestUri, HttpMethod method, JsonSerializer jsonSerializer, object? request = null, CancellationToken ct = default)
        {
            HttpRequestMessage httpRequestMessage;

            if (method == HttpMethod.Post)
            {
                httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
                {
                    Content = new StringContent(jsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
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