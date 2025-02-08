using HtmlAgilityPack;

using Newtonsoft.Json;

using OllamaClientLibrary.Dto.Models;
using OllamaClientLibrary.Extensions;

using System.Collections.Concurrent;

namespace OllamaClientLibrary.Parsers
{
    internal static class RemoteModelParser
    {
        public static async Task<IEnumerable<Model>> ParseAsync(HttpClient client, JsonSerializer jsonSerializer, CancellationToken ct)
        {
            using var stream = await client.ExecuteAndGetStreamAsync("https://ollama.com/library?sort=newest", HttpMethod.Get, jsonSerializer, ct: ct);

            var htmlDoc = new HtmlDocument();
            htmlDoc.Load(stream);

            var hrefs = htmlDoc.DocumentNode
                      .SelectNodes("//a[starts-with(@href, '/library/')]")
                      .Select(node => node.GetAttributeValue("href", string.Empty))
                      .ToList();

            var remoteModels = new ConcurrentBag<Model>();

            var semaphore = new SemaphoreSlim(20);

            var tasks = hrefs.Select(async href =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    var models = await GetRemoteModels(client, jsonSerializer, href, ct);

                    foreach (var model in models)
                    {
                        remoteModels.Add(model);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            await Task.WhenAll(tasks);

            return remoteModels;
        }

        private static async Task<IEnumerable<Model>> GetRemoteModels(HttpClient client, JsonSerializer jsonSerializer, string href, CancellationToken ct)
        {
            using var stream = await client.ExecuteAndGetStreamAsync($"https://ollama.com/{href}/tags", HttpMethod.Get, jsonSerializer, ct: ct);

            var htmlDoc = new HtmlDocument();
            htmlDoc.Load(stream);

            var modelNodes = htmlDoc.DocumentNode.SelectNodes("//div[@class='flex px-4 py-3']");
            var remoteModels = new List<Model>();

            var semaphore = new SemaphoreSlim(5);

            var tasks = modelNodes.Select(async modelNode =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    var remoteModel = new Model
                    {
                        Name = modelNode.SelectSingleNode(".//a[@class='group']").GetAttributeValue("href", null)?.Split("/").Last()
                    };

                    // Extract size and modified date
                    var infoNode = modelNode.SelectSingleNode(".//div[@class='flex items-baseline space-x-1 text-[13px] text-neutral-500']/span");
                    if (infoNode != null)
                    {
                        var infoText = infoNode.InnerText.Trim();
                        var parts = infoText.Split('•');

                        if (parts.Length >= 2)
                        {
                            // Extract size
                            var sizeText = parts[1].Trim();
                            if (sizeText.EndsWith("GB"))
                            {
                                if (float.TryParse(sizeText.Replace("GB", "").Trim(), out float size))
                                {
                                    remoteModel.Size = (long)Math.Round(size * 1024 * 1024 * 1024); // Convert GB to bytes
                                }
                            }
                            else if (sizeText.EndsWith("MB"))
                            {
                                if (float.TryParse(sizeText.Replace("MB", "").Trim(), out float size))
                                {
                                    remoteModel.Size = (long)Math.Round(size * 1024 * 1024); // Convert MB to bytes
                                }
                            }
                            else if (sizeText.EndsWith("TB"))
                            {
                                if (float.TryParse(sizeText.Replace("TB", "").Trim(), out float size))
                                {
                                    remoteModel.Size = (long)Math.Round(size * 1024 * 1024 * 1024 * 1024); // Convert TB to bytes
                                }
                            }

                            // Extract modified date
                            var dateText = parts[2].Trim();
                            if (DateTime.TryParse(dateText, out DateTime modifiedAt))
                            {
                                remoteModel.ModifiedAt = modifiedAt;
                            }
                            else
                            {
                                // Handle relative time (e.g., "2 weeks ago")
                                remoteModel.ModifiedAt = ParseRelativeTime(dateText);
                            }
                        }
                    }

                    remoteModels.Add(remoteModel);
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            await Task.WhenAll(tasks);

            return remoteModels;
        }

        private static DateTime? ParseRelativeTime(string relativeTime)
        {
            var now = DateTime.UtcNow;
            var parts = relativeTime.Split(' ');

            if (relativeTime.ToLower() == "yesterday")
            {
                return now.AddDays(-1);
            }

            if (parts.Length >= 2)
            {
                if (int.TryParse(parts[0], out int value))
                {
                    var unit = parts[1].ToLower();
                    return unit switch
                    {
                        "second" or "seconds" => now.AddSeconds(-value),
                        "minute" or "minutes" => now.AddMinutes(-value),
                        "hour" or "hours" => now.AddHours(-value),
                        "day" or "days" => now.AddDays(-value),
                        "week" or "weeks" => now.AddDays(-value * 7),
                        "month" or "months" => now.AddMonths(-value),
                        "year" or "years" => now.AddYears(-value),
                        _ => null,
                    };
                }
            }

            return null;
        }
    }
}
