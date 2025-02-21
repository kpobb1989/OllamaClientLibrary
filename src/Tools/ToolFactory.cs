using OllamaClientLibrary.Abstractions.Tools;
using OllamaClientLibrary.Dto.ChatCompletion.Tools.Request;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace OllamaClientLibrary.Tools
{
    public static class ToolFactory
    {
        public static async Task<object?> InvokeAsync(OllamaTool tool, Dictionary<string, object?>? arguments)
        {
            if (tool.MethodInfo == null)
            {
                return default;
            }

            var parameters = new List<object>();
            var methodParams = tool.MethodInfo.GetParameters();
            foreach (var param in methodParams)
            {
                if (arguments != null && arguments.TryGetValue(param.Name, out var value) && value != null)
                {
                    if (param.ParameterType.IsEnum)
                    {
                        parameters.Add(Enum.Parse(param.ParameterType, value.ToString(), true));
                    }
                    else
                    {
                        parameters.Add(Convert.ChangeType(value, param.ParameterType));
                    }
                }
            }

            var result = tool.MethodInfo.Invoke(tool.Instance, parameters.ToArray());

            if (result is Task task && task.GetType().IsGenericType)
            {
                return await GetTaskResultAsync(task).ConfigureAwait(false);
            }

            return result;
        }

        public static OllamaTool[] Create(object instance, params string[] methodNames)
        {
            var tools = new List<OllamaTool>();

            foreach (var methodName in methodNames)
            {
                var methodInfo = instance.GetType().GetMethod(methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                var tool = CreateInternal(methodInfo, instance);

                tools.Add(tool);
            }

            return tools.ToArray();
        }

        public static OllamaTool[] Create<TClass>(params string[] methodNames)
        {
            var tools = new List<OllamaTool>();

            foreach (var methodName in methodNames)
            {
                var methodInfo = typeof(TClass).GetMethod(methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                var tool = CreateInternal(methodInfo);

                tools.Add(tool);
            }

            return tools.ToArray();
        }

        private static async Task<object?> GetTaskResultAsync(Task task)
        {
            var resultProperty = task.GetType().GetProperty("Result");
            if (resultProperty != null)
            {
                return await Task.FromResult(resultProperty.GetValue(task)).ConfigureAwait(false);
            }
            return null;
        }

        private static OllamaTool CreateInternal(MethodInfo methodInfo, object? instance = null)
        {
            if (!methodInfo.IsStatic && instance == null)
            {
                var constructor = methodInfo.DeclaringType.GetConstructor(Type.EmptyTypes);

                if (constructor == null)
                {
                    throw new InvalidOperationException("The class must have a parameterless constructor.");
                }

                instance = Activator.CreateInstance(methodInfo.DeclaringType);
            }

            var tool = new OllamaTool
            {
                MethodInfo = methodInfo,
                Instance = instance,
                Function = new OllamaFunction
                {
                    Name = methodInfo?.Name,
                    Description = GetMethodDescription(methodInfo),
                    Parameters = new OllamaParameter()
                }
            };

            if (methodInfo != null)
            {
                foreach (var parameter in methodInfo.GetParameters())
                {
                    var parameterType = parameter.ParameterType;
                    var parameterName = parameter.Name ?? string.Empty;
                    var parameterDescription = GetParameterDescription(parameter);

                    var property = new OllamaProperty
                    {
                        Type = GetTypeString(parameterType),
                        Description = parameterDescription
                    };

                    if (parameterType.IsEnum)
                    {
                        property.Enum = Enum.GetNames(parameterType).ToList();
                    }

                    tool.Function.Parameters.Properties[parameterName] = property;
                    tool.Function.Parameters.Required.Add(parameterName);
                }
            }

            return tool;

        }

        private static string GetTypeString(Type type)
        {
            if (type == typeof(string))
                return "string";
            if (type == typeof(int) || type == typeof(long) || type == typeof(short))
                return "integer";
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
                return "number";
            if (type == typeof(bool))
                return "boolean";
            if (type.IsEnum)
                return "string"; // Enums are represented as strings

            return "object"; // Default to object for complex types
        }

        private static string GetMethodDescription(MethodInfo? methodInfo)
            => methodInfo?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? $"Description for {methodInfo?.Name}";

        private static string GetParameterDescription(ParameterInfo parameterInfo)
            => parameterInfo?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? $"Description for {parameterInfo?.Name}";
    }
}
