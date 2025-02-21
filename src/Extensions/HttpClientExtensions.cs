﻿using Newtonsoft.Json;

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OllamaClientLibrary.Extensions
{
    internal static class HttpClientExtensions
    {
        public static async Task<T?> ExecuteAndGetJsonAsync<T>(this HttpClient httpClient, string requestUri, HttpMethod method, JsonSerializer jsonSerializer, object? request = null, CancellationToken ct = default) where T : class
        {
            using var stream = await ExecuteAndGetStreamAsync(httpClient, requestUri, method, jsonSerializer, request, ct).ConfigureAwait(false);

            return jsonSerializer.Deserialize<T>(stream);
        }

        public static async Task<Stream> ExecuteAndGetStreamAsync(this HttpClient httpClient, string requestUri, HttpMethod method, JsonSerializer? jsonSerializer = null, object? request = null, CancellationToken ct = default)
            => (await ExecuteAsync(httpClient, requestUri, method, jsonSerializer, request, returnStream: true, ct).ConfigureAwait(false))!;

        public static async Task<Stream?> ExecuteAsync(this HttpClient httpClient, string requestUri, HttpMethod method, JsonSerializer? jsonSerializer = null, object? request = null, bool returnStream = false, CancellationToken ct = default)
        {
            HttpRequestMessage httpRequestMessage;

            if (method == HttpMethod.Post || method == HttpMethod.Delete)
            {
                httpRequestMessage = new HttpRequestMessage(method, requestUri);

                if (request != null && jsonSerializer != null)
                {
                    httpRequestMessage.Content = new StringContent(jsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                }
            }
            else
            {
                httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
            }

            var response = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            if (returnStream)
            {
                return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }

            return null;
        }
    }
}