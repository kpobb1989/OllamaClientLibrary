using OllamaClientLibrary.Models.Tools;

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
        /// <summary>
        /// Invokes the specified tool's method asynchronously with the provided arguments.
        /// </summary>
        /// <param name="tool">The tool containing the method to invoke.</param>
        /// <param name="arguments">The arguments to pass to the method.</param>
        /// <returns>The result of the method invocation, or null if the method does not return a value.</returns>
        public static async Task<object?> InvokeAsync(OllamaTool tool, Dictionary<string, object?>? arguments)
        {
            if (tool.MethodInfo == null)
            {
                return null;
            }

            var parameters = new List<object>();
            var methodParams = tool.MethodInfo.GetParameters();
            foreach (var param in methodParams)
            {
                if (arguments != null && arguments.TryGetValue(param.Name, out var value) && value != null)
                {
                    parameters.Add(param.ParameterType.IsEnum
                        ? Enum.Parse(param.ParameterType, value.ToString(), true)
                        : Convert.ChangeType(value, param.ParameterType));
                }
            }

            var result = tool.MethodInfo.Invoke(tool.Instance, parameters.ToArray());

            if (result is Task task && task.GetType().IsGenericType)
            {
                return await GetTaskResultAsync(task).ConfigureAwait(false);
            }

            return result;
        }

        /// <summary>
        /// Creates an array of <see cref="OllamaTool"/> for all public methods of the specified instance.
        /// </summary>
        /// <param name="instance">The instance whose public methods will be used to create tools.</param>
        /// <returns>An array of <see cref="OllamaTool"/> representing the public methods of the instance.</returns>
        public static OllamaTool[] Create(object instance)
        {
            var tools = new List<OllamaTool>();
            var methodInfos = instance.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            foreach (var methodInfo in methodInfos)
            {
                var tool = CreateTool(methodInfo, instance);
                tools.Add(tool);
            }

            return tools.ToArray();
        }

        /// <summary>
        /// Creates an array of <see cref="OllamaTool"/> for all public methods of the specified class type.
        /// </summary>
        /// <typeparam name="TClass">The class type whose public methods will be used to create tools.</typeparam>
        /// <returns>An array of <see cref="OllamaTool"/> representing the public methods of the class type.</returns>
        public static OllamaTool[] Create<TClass>()
        {
            var tools = new List<OllamaTool>();
            var methodInfos = typeof(TClass).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            foreach (var methodInfo in methodInfos)
            {
                var tool = CreateTool(methodInfo);
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

        private static OllamaTool CreateTool(MethodInfo methodInfo, object? instance = null)
        {
            if (!methodInfo.IsStatic && instance == null)
            {
                var constructor = methodInfo.DeclaringType?.GetConstructor(Type.EmptyTypes);

                if (constructor == null)
                {
                    throw new InvalidOperationException("The class must have a parameterless constructor.");
                }

                if (methodInfo.DeclaringType != null) 
                    instance = Activator.CreateInstance(methodInfo.DeclaringType);
            }

            var tool = new OllamaTool
            {
                MethodInfo = methodInfo,
                Instance = instance,
                Function = new OllamaFunction
                {
                    Name = methodInfo.Name,
                    Description = GetMethodDescription(methodInfo),
                    Parameters = new OllamaParameter()
                }
            };

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
            => parameterInfo.GetCustomAttribute<DescriptionAttribute>()?.Description ?? $"Description for {parameterInfo.Name}";
    }
}
