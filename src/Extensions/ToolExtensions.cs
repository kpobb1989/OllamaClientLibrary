using OllamaClientLibrary.Dto.ChatCompletion.Tools.Request;
using OllamaClientLibrary.Models.Tools;

using System.Linq;

namespace OllamaClientLibrary.Extensions
{
    internal static class ToolExtensions
    {
        public static Tool AsTool(this OllamaTool ollamaTool)
        {
            var tool = new Tool()
            {
                Type = ollamaTool.Type,
                Function = new Function()
                {
                    Name = ollamaTool.Function.Name,
                    Description = ollamaTool.Function.Description,
                    Parameters = new Parameter()
                    {
                        Type = ollamaTool.Function.Parameters.Type,
                        Properties = ollamaTool.Function.Parameters.Properties.ToDictionary(
                            p => p.Key,
                            p => new Property()
                            {
                                Type = p.Value.Type,
                                Description = p.Value.Description,
                                Enum = p.Value.Enum
                            }),
                        Required = ollamaTool.Function.Parameters.Required.ToList()
                    }
                }
            };

            return tool;
        }

        public static OllamaTool AsOllamaTool(this Tool tool)
        {
            var ollamaTool = new OllamaTool()
            {
                Function = new OllamaFunction()
                {
                    Name = tool.Function.Name,
                    Description = tool.Function.Description,
                    Parameters = new OllamaParameter()
                    {
                        Properties = tool.Function.Parameters.Properties.ToDictionary(
                            p => p.Key,
                            p => new OllamaProperty()
                            {
                                Type = p.Value.Type,
                                Description = p.Value.Description,
                                Enum = p.Value.Enum
                            }),
                        Required = tool.Function.Parameters.Required.ToList()
                    }
                }
            };

            return ollamaTool;
        }
    }
}
