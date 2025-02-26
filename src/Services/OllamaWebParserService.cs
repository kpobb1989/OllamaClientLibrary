using HtmlAgilityPack;

using OllamaClientLibrary.Abstractions.Services;
using OllamaClientLibrary.Dto.Models;

using System.Collections.Generic;

using System.Threading.Tasks;

using System.Threading;

using OllamaClientLibrary.Extensions;
using System.Linq;
using System;
using System.IO;

namespace OllamaClientLibrary.Services
{
    internal class OllamaWebParserService : IOllamaWebParserService
    {
        public async Task<IEnumerable<Model>> GetRemoteModelsAsync(Stream stream, CancellationToken ct)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.Load(stream);

            var modelNodes = htmlDoc.DocumentNode.SelectNodes("//div[@class='flex px-4 py-3']");

            var remoteModels = new List<Model>();

            var semaphore = new SemaphoreSlim(5);

            var tasks = modelNodes.Select(async modelNode =>
            {
                await semaphore.WaitAsync(ct).ConfigureAwait(false);

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
                                if (float.TryParse(sizeText.Replace("GB", string.Empty).Trim(), out float size))
                                {
                                    remoteModel.Size = (long)Math.Round(size * 1024 * 1024 * 1024); // Convert GB to bytes
                                }
                            }
                            else if (sizeText.EndsWith("MB"))
                            {
                                if (float.TryParse(sizeText.Replace("MB", string.Empty).Trim(), out float size))
                                {
                                    remoteModel.Size = (long)Math.Round(size * 1024 * 1024); // Convert MB to bytes
                                }
                            }
                            else if (sizeText.EndsWith("TB"))
                            {
                                if (float.TryParse(sizeText.Replace("TB", string.Empty).Trim(), out float size))
                                {
                                    remoteModel.Size = (long)Math.Round(size * 1024 * 1024 * 1024 * 1024); // Convert TB to bytes
                                }
                            }

                            // Extract modified date
                            var dateText = parts[2].Trim();
                            if (DateTime.TryParse(dateText, out DateTime modifiedAt))
                            {
                                remoteModel.ModifiedAt = modifiedAt.Date;
                            }
                            else
                            {
                                remoteModel.ModifiedAt = dateText.AsDateTime()?.Date;
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

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return remoteModels;
        }
    }
}
